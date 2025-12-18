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
            var rooms = await _dbContext.Rooms.ToListAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(rooms);
            return response;
        }
    }
}