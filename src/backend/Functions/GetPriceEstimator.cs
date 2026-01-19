using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SmartHotel.Backend.Data;
using SmartHotel.Backend.Models;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json; // Potrzebne do parsowania JSON, zainstaluj pakiet Newtonsoft.Json
using Microsoft.EntityFrameworkCore; // Potrzebne dla Include i CountAsync

namespace SmartHotel.Backend.Functions
{
    /// <summary>
    /// Azure Function do szacowania ceny rezerwacji pokoju, uwzględniająca sezony i obłożenie.
    /// </summary>
    public class GetPriceEstimator
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<GetPriceEstimator> _logger;

        /// <summary>
        /// Konstruktor z wstrzyknięciem zależności dla DbContext i Loggera.
        /// </summary>
        public GetPriceEstimator(HotelDbContext context, ILogger<GetPriceEstimator> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint HTTP do wywoływania szacowania ceny.
        /// Obsługuje żądania GET i POST.
        /// </summary>
        [Function("GetPriceEstimator")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "priceEstimate")] HttpRequestData req)
        {
            _logger.LogInformation("GetPriceEstimator: C# HTTP trigger function processed a request.");

            int roomId = 0;
            DateOnly checkInDate = default;
            DateOnly checkOutDate = default;

            try
            {
                // 1. Pobieranie parametrów z żądania
                roomId = int.TryParse(req.Query["roomId"], out var rId) ? rId : 0;
                checkInDate = DateOnly.TryParse(req.Query["checkInDate"], out var ciDate) ? ciDate : default;
                checkOutDate = DateOnly.TryParse(req.Query["checkOutDate"], out var coDate) ? coDate : default;

                if (roomId == 0 || checkInDate == default || checkOutDate == default)
                {
                    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    if (!string.IsNullOrEmpty(requestBody))
                    {
                        var priceEstimateRequest = JsonConvert.DeserializeObject<PriceEstimateRequest>(requestBody);
                        roomId = priceEstimateRequest?.RoomId ?? roomId;
                        checkInDate = priceEstimateRequest?.CheckInDate ?? checkInDate;
                        checkOutDate = priceEstimateRequest?.CheckOutDate ?? checkOutDate;
                    }
                }

                // 2. Walidacja parametrów wejściowych
                if (roomId == 0 || checkInDate == default || checkOutDate == default || checkOutDate <= checkInDate)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid input. Please provide valid 'roomId', 'checkInDate', and 'checkOutDate' (checkOutDate must be after checkInDate). Dates should be in YYYY-MM-DD format.");
                    _logger.LogWarning("GetPriceEstimator: Invalid input received. roomId: {RoomId}, checkInDate: {CheckInDate}, checkOutDate: {CheckOutDate}", roomId, checkInDate, checkOutDate);
                    return badRequestResponse;
                }

                // 3. Pobieranie danych pokoju z bazy danych (z uwzględnieniem sezonowych cen i typu pokoju)
                var room = await _context.Rooms
                    .Include(r => r.SeasonalPrices) // Kluczowe: wczytaj powiązane ceny sezonowe
                    .AsNoTracking() // Użyj AsNoTracking() dla lepszej wydajności, ponieważ nie modyfikujemy encji
                    .FirstOrDefaultAsync(r => r.Id == roomId);

                if (room == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Room with ID '{roomId}' not found.");
                    _logger.LogWarning("GetPriceEstimator: Room with ID {RoomId} not found.", roomId);
                    return notFoundResponse;
                }

                // --- Definicja reguł cenowych opartych na obłożeniu ---
                // W rzeczywistym systemie te reguły powinny być konfigurowalne, np. z appsettings.json, Key Vault lub dedykowanej tabeli.
                var occupancyPriceRules = new[]
                {
                    new { MinOccupancyPercentage = 0m, MaxOccupancyPercentage = 50m, Multiplier = 1.0m },    // Brak podwyżki przy niskim obłożeniu
                    new { MinOccupancyPercentage = 50.1m, MaxOccupancyPercentage = 75m, Multiplier = 1.15m },  // +15% przy średnim obłożeniu
                    new { MinOccupancyPercentage = 75.1m, MaxOccupancyPercentage = 90m, Multiplier = 1.30m },  // +30% przy wysokim obłożeniu
                    new { MinOccupancyPercentage = 90.1m, MaxOccupancyPercentage = 100m, Multiplier = 1.50m }   // +50% przy bardzo wysokim obłożeniu
                };

                // 4. Obliczanie całkowitej ceny za okres rezerwacji
                decimal totalPrice = 0;
                DateOnly currentDate = checkInDate;

                // Pobierz całkowitą liczbę pokoi tego samego typu raz, poza pętlą, dla optymalizacji
                var totalRoomsOfType = await _context.Rooms.CountAsync(r => r.RoomType == room.RoomType);

                while (currentDate < checkOutDate)
                {
                    decimal dailyPriceBeforeOccupancy = room.BasePrice; // Zacznij od ceny bazowej

                    // --- Krok 1: Zastosuj cenę sezonową ---
                    var applicableSeason = room.SeasonalPrices.FirstOrDefault(sp =>
                        currentDate >= sp.StartDate && currentDate <= sp.EndDate);

                    if (applicableSeason != null)
                    {
                        if (applicableSeason.FixedPrice.HasValue && applicableSeason.FixedPrice.Value > 0)
                        {
                            dailyPriceBeforeOccupancy = applicableSeason.FixedPrice.Value;
                        }
                        else if (applicableSeason.PriceMultiplier > 0)
                        {
                            dailyPriceBeforeOccupancy *= applicableSeason.PriceMultiplier;
                        }
                        _logger.LogInformation("GetPriceEstimator: Applying season '{SeasonName}' for date {CurrentDate} (Room ID {RoomId}). Base: {BasePrice}, Seasonal price: {DailyPriceBeforeOccupancy}.",
                                applicableSeason.SeasonName, currentDate, roomId, room.BasePrice, dailyPriceBeforeOccupancy);
                    }
                    else
                    {
                        _logger.LogInformation("GetPriceEstimator: No specific season found for date {CurrentDate} (Room ID {RoomId}). Using base price {BasePrice}.", currentDate, roomId, room.BasePrice);
                    }

                    // --- Krok 2: Oblicz obłożenie dla danego dnia i typu pokoju ---
                    decimal currentOccupancyPercentage = 0;
                    if (totalRoomsOfType > 0)
                    {
                        // Zlicz pokoje tego typu, które są zarezerwowane na currentDate
                        // To zapytanie może być kosztowne dla dużej liczby rezerwacji/pokoi.
                        // W bardziej zaawansowanych scenariuszach można rozważyć buforowanie lub pre-agregację obłożenia.
                        var bookedRoomsOfType = await _context.Reservations.CountAsync(res =>
                            res.Room.RoomType == room.RoomType && // Filtrujemy po typie pokoju
                            res.CheckInDate <= currentDate &&
                            res.CheckOutDate > currentDate); // Rezerwacja jest aktywna w tym dniu

                        currentOccupancyPercentage = ((decimal)bookedRoomsOfType / totalRoomsOfType) * 100;
                        _logger.LogInformation("GetPriceEstimator: Occupancy for room type '{RoomType}' on {CurrentDate} (Room ID {RoomId}): {BookedRooms}/{TotalRooms} ({OccupancyPercentage:F2}%).",
                                room.RoomType, currentDate, roomId, bookedRoomsOfType, totalRoomsOfType, currentOccupancyPercentage);
                    }
                    else
                    {
                        _logger.LogWarning("GetPriceEstimator: No rooms found for type '{RoomType}' (Room ID {RoomId}). Cannot calculate occupancy.", room.RoomType, roomId);
                    }

                    // --- Krok 3: Zastosuj mnożnik cenowy oparty na obłożeniu ---
                    decimal occupancyMultiplier = 1.0m;
                    // Sortujemy malejąco, aby najpierw zastosować najwyższe obłożenie, jeśli pasuje do reguły
                    foreach (var rule in occupancyPriceRules.OrderByDescending(r => r.MaxOccupancyPercentage))
                    {
                        if (currentOccupancyPercentage >= rule.MinOccupancyPercentage && currentOccupancyPercentage <= rule.MaxOccupancyPercentage)
                        {
                            occupancyMultiplier = rule.Multiplier;
                            _logger.LogInformation("GetPriceEstimator: Applying occupancy multiplier {Multiplier} for {OccupancyPercentage:F2}% occupancy (Room ID {RoomId}).",
                                    occupancyMultiplier, currentOccupancyPercentage, roomId);
                            break; // Znaleziono pasującą regułę
                        }
                    }

                    // --- Ostateczna cena dzienna ---
                    decimal finalDailyPrice = dailyPriceBeforeOccupancy * occupancyMultiplier;
                    totalPrice += finalDailyPrice;

                    _logger.LogInformation("GetPriceEstimator: Day {CurrentDate} (Room ID {RoomId}): SeasonalPrice={DailyPriceBeforeOccupancy}, Occupancy={OccupancyPercentage:F2}%, OccupancyMultiplier={OccupancyMultiplier}, FinalDailyPrice={FinalDailyPrice}",
                            currentDate, roomId, dailyPriceBeforeOccupancy, currentOccupancyPercentage, occupancyMultiplier, finalDailyPrice);

                    currentDate = currentDate.AddDays(1); // Przejdź do następnego dnia
                }

                // 5. Przygotowanie odpowiedzi
                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                var result = new PriceEstimateResponse
                {
                    RoomId = roomId,
                    CheckInDate = checkInDate,
                    CheckOutDate = checkOutDate,
                    EstimatedPrice = totalPrice,
                    Currency = "USD", // Domyślna waluta, można skonfigurować
                    Notes = "This is an estimated price. It considers seasonal rates and current occupancy. Final price may vary based on real-time availability and additional services."
                };
                await okResponse.WriteAsJsonAsync(result);
                _logger.LogInformation("GetPriceEstimator: Successfully calculated price for Room ID {RoomId} from {CheckInDate} to {CheckOutDate}. Total: {TotalPrice} {Currency}", roomId, checkInDate, checkOutDate, totalPrice, result.Currency);
                return okResponse;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "GetPriceEstimator: JSON deserialization error.");
                var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await errorResponse.WriteStringAsync($"Invalid JSON format: {jsonEx.Message}");
                return errorResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPriceEstimator: An unexpected error occurred while calculating price estimate.");
                var internalServerErrorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await internalServerErrorResponse.WriteStringAsync($"An internal error occurred: {ex.Message}");
                return internalServerErrorResponse;
            }
        }
    }

    /// <summary>
    /// Klasa pomocnicza do parsowania ciała żądania POST.
    /// </summary>
    public class PriceEstimateRequest
    {
        public int RoomId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
    }

    /// <summary>
    /// Klasa pomocnicza do formatowania odpowiedzi.
    /// </summary>
    public class PriceEstimateResponse
    {
        public int RoomId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public decimal EstimatedPrice { get; set; }
        public string Currency { get; set; }
        public string Notes { get; set; }
    }
}