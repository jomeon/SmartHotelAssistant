using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using SmartHotel.Backend.Data;
using System.Net;

namespace SmartHotel.Backend.Functions
{
    public class GetRooms
    {
        private readonly HotelDbContext _dbContext;

        public GetRooms(HotelDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Function("GetRooms")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rooms")] HttpRequestData req)
        {
            // 1. Pobierz wszystkie pokoje
            var rooms = await _dbContext.Rooms.ToListAsync();

            // 2. Pobierz tylko PRZYSZŁE i AKTUALNE rezerwacje (nie interesuje nas historia sprzed roku)
            var today = DateTime.UtcNow.Date;
            var futureReservations = await _dbContext.Reservations
                .Where(r => r.CheckOutDate >= today) 
                .ToListAsync();

            // 3. Złącz dane w jeden obiekt wynikowy (Projection)
            var result = rooms.Select(room => new 
            {
                room.Id,
                room.RoomNumber,
                room.Type,
                room.Capacity,
                room.PricePerNight,
                // Dla każdego pokoju filtrujemy jego rezerwacje
                OccupiedDates = futureReservations
                    .Where(res => res.RoomId == room.Id)
                    .Select(res => new { 
                        res.CheckInDate, 
                        res.CheckOutDate 
                    })
                    .OrderBy(res => res.CheckInDate)
                    .ToList()
            });

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}