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
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

namespace SmartHotel.Backend.Functions
{
    public class GetPriceEstimator
    {
        private readonly HotelDbContext _context;
        private readonly ILogger<GetPriceEstimator> _logger;

        public GetPriceEstimator(HotelDbContext context, ILogger<GetPriceEstimator> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Function("GetPriceEstimator")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "priceEstimate")] HttpRequestData req)
        {
            _logger.LogInformation("GetPriceEstimator: Processing request.");

            int roomId = 0;
            DateOnly checkInDate = default;
            DateOnly checkOutDate = default;

            try
            {
                // 1. Pobieranie parametrów
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

                // 2. Walidacja
                if (roomId == 0 || checkInDate == default || checkOutDate == default || checkOutDate <= checkInDate)
                {
                    var badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badRequestResponse.WriteStringAsync("Invalid input. Provide roomId and valid dates (checkOut > checkIn).");
                    return badRequestResponse;
                }

                // 3. Pobieranie danych pokoju
                var room = await _context.Rooms
                    .Include(r => r.SeasonalPrices)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == roomId);

                if (room == null)
                {
                    var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                    await notFoundResponse.WriteStringAsync($"Room with ID '{roomId}' not found.");
                    return notFoundResponse;
                }

                // 4. Obliczanie ceny
                decimal totalPrice = 0;
                DateOnly currentDate = checkInDate;
                
                // Pobieramy całkowitą liczbę pokoi tego typu
                var totalRoomsOfType = await _context.Rooms.CountAsync(r => r.RoomType == room.RoomType);

                while (currentDate < checkOutDate)
                {
                    decimal dailyPriceBeforeOccupancy = room.BasePrice;

                    // --- Krok 1: Sezony (Operacje na DateOnly) ---
                    // Skoro SeasonalPrice.StartDate to DateOnly (wg błędu kompilatora),
                    // porównujemy bezpośrednio z currentDate.
                    var applicableSeason = room.SeasonalPrices.FirstOrDefault(sp =>
                        currentDate >= sp.StartDate && 
                        currentDate <= sp.EndDate);

                    if (applicableSeason != null)
                    {
                        if (applicableSeason.FixedPrice.HasValue && applicableSeason.FixedPrice.Value > 0)
                            dailyPriceBeforeOccupancy = applicableSeason.FixedPrice.Value;
                        else if (applicableSeason.PriceMultiplier > 0)
                            dailyPriceBeforeOccupancy *= applicableSeason.PriceMultiplier;
                    }

                    // --- Krok 2: Obłożenie (Operacje na DateTime w bazie) ---
                    decimal currentOccupancyPercentage = 0;
                    if (totalRoomsOfType > 0)
                    {
                        // Tutaj musimy skonwertować currentDate na DateTime, bo Reservation używa DateTime
                        DateTime sqlCurrentDate = currentDate.ToDateTime(TimeOnly.MinValue);

                        var bookedRoomsOfType = await _context.Reservations.CountAsync(res =>
                            res.Room != null && // Fix CS8602: Sprawdzamy czy pokój istnieje
                            res.Room.RoomType == room.RoomType &&
                            res.CheckInDate <= sqlCurrentDate && 
                            res.CheckOutDate > sqlCurrentDate);

                        currentOccupancyPercentage = ((decimal)bookedRoomsOfType / totalRoomsOfType) * 100;
                    }

                    // --- Krok 3: Mnożnik obłożenia ---
                    decimal occupancyMultiplier = 1.0m;
                    if (currentOccupancyPercentage > 90) occupancyMultiplier = 1.50m;
                    else if (currentOccupancyPercentage > 75) occupancyMultiplier = 1.30m;
                    else if (currentOccupancyPercentage > 50) occupancyMultiplier = 1.15m;

                    totalPrice += (dailyPriceBeforeOccupancy * occupancyMultiplier);
                    
                    // Przejdź do następnego dnia
                    currentDate = currentDate.AddDays(1);
                }

                var okResponse = req.CreateResponse(HttpStatusCode.OK);
                await okResponse.WriteAsJsonAsync(new PriceEstimateResponse
                {
                    RoomId = roomId,
                    CheckInDate = checkInDate,
                    CheckOutDate = checkOutDate,
                    EstimatedPrice = totalPrice,
                    Currency = "USD",
                    Notes = "Estimated price based on seasonal rates and occupancy."
                });
                return okResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating price.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync(ex.Message);
                return errorResponse;
            }
        }
    }

    public class PriceEstimateRequest
    {
        public int RoomId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
    }

    public class PriceEstimateResponse
    {
        public int RoomId { get; set; }
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
        public decimal EstimatedPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string Notes { get; set; } = string.Empty;
    }
}