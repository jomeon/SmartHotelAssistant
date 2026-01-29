using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SmartHotel.Backend.Data;
using SmartHotel.Backend.Functions;
using SmartHotel.Backend.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker;
using System.Text;
using Newtonsoft.Json;
using System.Net;

namespace SmartHotel.Backend.Tests
{
    public class ReservationTests
    {
        [Fact]
        public async Task CreateReservation_ShouldFail_WhenCheckOutDateIsBeforeCheckIn()
        {
            // --- 1. ARRANGE (Przygotowanie) ---
            
            // Baza w pamięci
            var options = new DbContextOptionsBuilder<HotelDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var dbContext = new HotelDbContext(options);

            // Logger
            var loggerMock = new Mock<ILogger<CreateReservation>>();
            var loggerFactoryMock = new Mock<ILoggerFactory>();
            loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

            var function = new CreateReservation(loggerFactoryMock.Object, dbContext);

            // BŁĘDNE DANE
            var badReservation = new Reservation
            {
                GuestName = "Tester",
                GuestEmail = "test@test.pl",
                RoomId = 1,
                CheckInDate = DateTime.UtcNow.AddDays(5),
                CheckOutDate = DateTime.UtcNow.AddDays(2) 
            };
            string jsonBody = JsonConvert.SerializeObject(badReservation);
            
            // Mockowanie Context
            var contextMock = new Mock<FunctionContext>();
            var requestMock = new Mock<HttpRequestData>(contextMock.Object);
            
            // Input Stream (wejście)
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody));
            requestMock.Setup(r => r.Body).Returns(memoryStream);
            
            // Mockowanie Response (To naprawiamy!)
            // Zamiast mockować WriteStringAsync, dajemy mu prawdziwy strumień, żeby miał gdzie pisać
            var responseBodyStream = new MemoryStream();
            var responseMock = new Mock<HttpResponseData>(contextMock.Object);
            
            responseMock.SetupProperty(r => r.StatusCode);
            responseMock.Setup(r => r.Body).Returns(responseBodyStream); // <-- TO JEST KLUCZOWE
            responseMock.Setup(r => r.Headers).Returns(new HttpHeadersCollection());

            requestMock.Setup(r => r.CreateResponse()).Returns(responseMock.Object);

            // --- 2. ACT (Wykonanie) ---
            var result = await function.Run(requestMock.Object);

            // --- 3. ASSERT (Sprawdzenie) ---
            Assert.Equal(HttpStatusCode.BadRequest, result.HttpResponse.StatusCode);
        }
    }
}