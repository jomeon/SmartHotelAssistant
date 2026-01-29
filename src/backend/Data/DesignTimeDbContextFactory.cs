using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace SmartHotel.Backend.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<HotelDbContext>
    {
        public HotelDbContext CreateDbContext(string[] args)
        {
            // 1. Wczytaj konfigurację z pliku local.settings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
                .Build();

            // 2. Pobierz connection string (szukamy w sekcji Values)
            var connectionString = configuration["Values:SqlConnectionString"];

          
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new System.Exception("Nie znaleziono 'SqlConnectionString' w pliku local.settings.json");
            }

            // 3. Utwórz opcje
            var optionsBuilder = new DbContextOptionsBuilder<HotelDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new HotelDbContext(optionsBuilder.Options);
        }
    }
}