using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OrchestratorPattern.Api.Common.Persistence;
using OrchestratorPattern.Api.Common.Persistence.Seed;

namespace OrchestratorPattern.Tests.Common;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "IntegrationTestsDb_" + Guid.NewGuid().ToString("N");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing EF Core registrations
            var efServices = services
                .Where(d => d.ServiceType == typeof(AppDbContext) ||
                            d.ServiceType == typeof(DbContextOptions) ||
                            d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                            d.ServiceType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                            d.ImplementationType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
                .ToList();

            foreach (var service in efServices)
            {
                services.Remove(service);
            }

            // Register InMemory database with an isolated service provider
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });
        });

        builder.UseEnvironment("Development");
    }
}
