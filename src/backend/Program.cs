using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SmartHotel.Backend.Data;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Rejestracja Application Insights 
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Rejestracja Bazy Danych
        // Pobieramy Connection String ze zmiennych środowiskowych
        string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

        services.AddDbContext<HotelDbContext>(options =>
            options.UseSqlServer(connectionString));
    })
    .Build();

host.Run();