using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace SmartHotel.Backend.Functions
{
    public class ProcessReservation
    {
        private readonly ILogger _logger;

        public ProcessReservation(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ProcessReservation>();
        }

        // Trigger uruchamia się, gdy coś wpadnie do kolejki 'rezerwacje-queue'
        [Function("ProcessReservation")]
        public void Run([QueueTrigger("rezerwacje-queue", Connection = "AzureWebJobsStorage")] string myQueueItem)
        {
            _logger.LogInformation($"[INTEGRACJA] Przetwarzanie rezerwacji z kolejki: {myQueueItem}");
            
            // Symulacja wysyłki
            _logger.LogWarning($"-> Wysłano e-mail z potwierdzeniem do klienta! (Symulacja)");
        }
    }
}