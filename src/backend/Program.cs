using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SmartHotel.Backend.Data;
using System;


var host = new HostBuilder()
    .ConfigureFunctionsWebApplication() // (zamiast ConfigureFunctionsWorkerDefaults)
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Rejestracja bazy danych
        services.AddDbContext<HotelDbContext>(options =>
        {
            var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");
            options.UseSqlServer(connectionString);
        });
    })
    .Build();

host.Run();