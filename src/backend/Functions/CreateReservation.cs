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

            // Walidacja
            if (data == null || string.IsNullOrEmpty(data.GuestName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Brak wymaganych danych.");
                return new MultiResponse { HttpResponse = badResponse };
            }

            // Zapis do SQL (Entity Framework)
            try 
            {
                data.Id = Guid.NewGuid();
                data.CreatedAt = DateTime.UtcNow;
                
                await _dbContext.Reservations.AddAsync(data);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Zapisano w SQL. ID: {data.Id}");

                // Sukces - przygotowujemy odpowiedź HTTP
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Status = "Success", ReservationId = data.Id });

                // Zwracamy obiekt, który trafi i do HTTP, i na Kolejkę
                return new MultiResponse
                {
                    HttpResponse = response,
                    // To trafi na kolejkę jako JSON:
                    QueueMessage = JsonConvert.SerializeObject(new { data.Id, data.GuestName }) 
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

    // Klasa pomocnicza do zwracania wielu wyjść naraz
    public class MultiResponse
    {
        [HttpResult]
        public HttpResponseData? HttpResponse { get; set; }

        // Ta adnotacja wrzuca wiadomość na kolejkę 'rezerwacje-queue'
        // Connection = "AzureWebJobsStorage" to domyślne konto storage funkcji
        [QueueOutput("rezerwacje-queue", Connection = "AzureWebJobsStorage")]
        public string? QueueMessage { get; set; }
    }
}