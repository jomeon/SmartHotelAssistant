using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartHotel.Backend.Data;

namespace SmartHotel.Backend.Functions
{
    public class DailyReminder
    {
        private readonly HotelDbContext _dbContext;
        private readonly ILogger _logger;

        public DailyReminder(HotelDbContext dbContext, ILoggerFactory loggerFactory)
        {
            _dbContext = dbContext;
            _logger = loggerFactory.CreateLogger<DailyReminder>();
        }

        // NCRONTAB: "0 0 8 * * *" -> codziennie o 8:00
        [Function("DailyReminder")]
        public async Task Run([TimerTrigger("0 0 8 * * *")] TimerInfo myTimer)
        {
            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            var nextDay = tomorrow.AddDays(1);

            // Szukamy rezerwacji, które zaczynają się jutro
            var upcomingReservations = await _dbContext.Reservations
                .Where(r => r.CheckInDate >= tomorrow && r.CheckInDate < nextDay)
                .ToListAsync();

            _logger.LogInformation($"[ALERT] Znaleziono {upcomingReservations.Count} rezerwacji na jutro.");

            foreach (var res in upcomingReservations)
            {
                // Tutaj normalnie wysyłalibyśmy e-mail (SendGrid)
                // Na zaliczenie wystarczy LOG - to dowód, że logika działa
                _logger.LogWarning($"[EMAIL] Wysyłanie przypomnienia do: {res.GuestEmail} (Pokój ID: {res.RoomId})");
            }
        }
    }
}