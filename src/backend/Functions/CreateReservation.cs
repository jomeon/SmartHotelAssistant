using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SmartHotel.Backend.Data;
using SmartHotel.Backend.Models;
using System.Net;

namespace SmartHotel.Backend.Functions
{
    public class CreateReservation
    {
        private readonly ILogger _logger;
        private readonly HotelDbContext _dbContext;

        public CreateReservation(ILoggerFactory loggerFactory, HotelDbContext dbContext)
        {
            _logger = loggerFactory.CreateLogger<CreateReservation>();
            _dbContext = dbContext;
        }

        [Function("CreateReservation")]
        public async Task<MultiResponse> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reservation")] HttpRequestData req)
        {
            _logger.LogInformation("Otrzymano nową rezerwację.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<Reservation>(requestBody);

            // --- WALIDACJA 1: CZY SĄ DANE? ---
            if (data == null || data.RoomId == 0 || string.IsNullOrEmpty(data.GuestEmail))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Brak wymaganych danych (RoomId, Email).");
                return new MultiResponse { HttpResponse = badResponse };
            }

            // --- WALIDACJA 2: LOGIKA DAT (TO DODAJEMY) ---
            // Data wyjazdu musi być późniejsza niż data przyjazdu
            if (data.CheckOutDate <= data.CheckInDate)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Data wymeldowania musi być późniejsza niż zameldowania!");
                return new MultiResponse { HttpResponse = badResponse };
            }

            // Opcjonalnie: Blokada rezerwacji wstecz (w przeszłości)
            if (data.CheckInDate.Date < DateTime.UtcNow.Date)
            {
                 var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                 await badResponse.WriteStringAsync("Nie można rezerwować dat w przeszłości.");
                 return new MultiResponse { HttpResponse = badResponse };
            }

            // --- 3. SPRAWDZENIE DOSTĘPNOŚCI (SQL) ---
            bool isOccupied = await _dbContext.Reservations.AnyAsync(r => 
                r.RoomId == data.RoomId &&
                data.CheckInDate < r.CheckOutDate && 
                r.CheckInDate < data.CheckOutDate
            );

            if (isOccupied)
            {
                var conflictResponse = req.CreateResponse(HttpStatusCode.Conflict);
                await conflictResponse.WriteStringAsync("Ten pokój jest już zajęty w wybranym terminie!");
                return new MultiResponse { HttpResponse = conflictResponse };
            }

            // --- 4. POBRANIE CENY I ZAPIS ---
            var room = await _dbContext.Rooms.FindAsync(data.RoomId);
            if(room == null) 
            {
                 var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                 await badResponse.WriteStringAsync("Taki pokój nie istnieje.");
                 return new MultiResponse { HttpResponse = badResponse };
            }

            int nights = (data.CheckOutDate - data.CheckInDate).Days;
            
            data.TotalPrice = room.PricePerNight * nights;
            data.Id = Guid.NewGuid();
            data.CreatedAt = DateTime.UtcNow;

            try 
            {
                await _dbContext.Reservations.AddAsync(data);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Zapisano w SQL. ID: {data.Id}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Status = "Success", ReservationId = data.Id, Price = data.TotalPrice });

                return new MultiResponse
                {
                    HttpResponse = response,
                    QueueMessage = JsonConvert.SerializeObject(new { data.Id, data.GuestEmail, Type = "Confirmation" }) 
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd zapisu do bazy.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                return new MultiResponse { HttpResponse = errorResponse };
            }
        }
    }

    public class MultiResponse
    {
        [HttpResult]
        public HttpResponseData? HttpResponse { get; set; }

        [QueueOutput("rezerwacje-queue", Connection = "AzureWebJobsStorage")]
        public string? QueueMessage { get; set; }
    }
}