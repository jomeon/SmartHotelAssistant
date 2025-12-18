using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using SmartHotel.Backend.Data;
using System.Net;

namespace SmartHotel.Backend.Functions
{
    public class GetMyReservations
    {
        private readonly HotelDbContext _dbContext;

        public GetMyReservations(HotelDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [Function("GetMyReservations")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "my-reservations/{email}")] HttpRequestData req,
            string email)
        {
            // Pobieramy rezerwacje dla danego maila wraz z detalami pokoju
            var reservations = await _dbContext.Reservations
                .Include(r => r.Room)
                .Where(r => r.GuestEmail == email)
                .OrderByDescending(r => r.CheckInDate)
                .ToListAsync();

            // Żeby uniknąć cykli w JSON (Reservation -> Room -> Reservation...), rzutujemy na anonimowy obiekt
            var result = reservations.Select(r => new {
                r.Id,
                r.CheckInDate,
                r.CheckOutDate,
                r.TotalPrice,
                RoomNumber = r.Room?.RoomNumber,
                RoomType = r.Room?.Type
            });

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}