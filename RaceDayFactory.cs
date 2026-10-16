using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Data;

namespace RaceDay.Tests
{
    /// <summary>
    /// Starts the real API in memory and swaps SQL Server for an in-memory database.
    /// </summary>
    public class RaceDayFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbName = Guid.NewGuid().ToString();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the SQL Server registration of the DbContext
                var toRemove = services
                    .Where(d => d.ServiceType.IsGenericType &&
                                d.ServiceType.GenericTypeArguments.Contains(typeof(RaceDayDbContext)))
                    .ToList();

                foreach (var descriptor in toRemove)
                    services.Remove(descriptor);

                services.AddDbContext<RaceDayDbContext>(options =>
                    options.UseInMemoryDatabase(_dbName));
            });
        }

        /// <summary>A client with its own cookie jar, so each client has its own login session.</summary>
        public HttpClient CreateApiClient() =>
            CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                HandleCookies = true
            });
    }
}
