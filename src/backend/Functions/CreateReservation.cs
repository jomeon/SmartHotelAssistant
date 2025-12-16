using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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

        // Wstrzykiwanie zależności (Dependency Injection)
        public CreateReservation(ILoggerFactory loggerFactory, HotelDbContext dbContext)
        {
            _logger = loggerFactory.CreateLogger<CreateReservation>();
            _dbContext = dbContext;
        }

        [Function("CreateReservation")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "reservation")] HttpRequestData req)
        {
            _logger.LogInformation("A request for a new reservation has been received.");

            // 1. Odczytanie treści żądania
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<Reservation>(requestBody);

            if (data == null || string.IsNullOrEmpty(data.GuestName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Incorrect booking details.");
                return badResponse;
            }

            // 2. Logika biznesowa (prosta walidacja)
            if (data.CheckOutDate <= data.CheckInDate)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("The departure date must be later than the arrival date.");
                return badResponse;
            }

            // 3. Zapis do bazy danych
            try 
            {
                data.Id = Guid.NewGuid(); // Upewniamy się, że ID jest nowe
                data.CreatedAt = DateTime.UtcNow;
                
                await _dbContext.Reservations.AddAsync(data);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Reservation created for: {data.GuestName}, ID: {data.Id}");

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Status = "Success", ReservationId = data.Id });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while writing to the database.");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync("A server error occurred.");
                return errorResponse;
            }
        }
    }
}