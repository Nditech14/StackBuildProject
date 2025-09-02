using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackBuld.Infrastructure.Data;
using StackBuld.Infrastructure.Repositories.OrderRepo.Abstraction;
using StackBuld.Infrastructure.Repositories.OrderRepo.Implementation;
using StackBuld.Infrastructure.Services.Abstraction;
using StackBuld.Infrastructure.Services.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StackBuld.Infrastructure.Extensions
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register DbContext
            services.AddDbContext<ECommerceDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly("StackBuld.Infrastructure"); // ← ADD THIS
                        sqlOptions.EnableRetryOnFailure(                          // ← ADD THIS
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });

                // Enable detailed logging for development
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                if (environment == "Development")
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                    options.LogTo(Console.WriteLine, LogLevel.Information);
                }
            });

            // Register Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register Repositories explicitly (based on your code)
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();

            // Register all repositories automatically
            RegisterRepositoriesAutomatically(services);

            // Register generic repository if you have one
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));


            return services;
        }


        private static void RegisterRepositoriesAutomatically(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Get all types that end with "Repository" and are not interfaces
            var repositoryTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Repository") &&
                           !t.IsInterface &&
                           !t.IsAbstract &&
                           t.IsClass &&
                           !t.Name.Equals("GenericRepository") && // Exclude generic repository
                           !t.Name.Equals("UnitOfWork")) // Exclude UnitOfWork
                .ToList();

            foreach (var repositoryType in repositoryTypes)
            {
                // Look for corresponding interface (I{RepositoryName})
                var interfaceName = $"I{repositoryType.Name}";
                var interfaceType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == interfaceName && t.IsInterface);

                if (interfaceType != null)
                {
                    services.AddScoped(interfaceType, repositoryType);
                }
            }
        }

        // Extension for adding caching
        public static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
        {
            // Add memory cache
            services.AddMemoryCache();

            

            return services;
        }

        // Extension for adding background services
        public static IServiceCollection AddBackgroundServices(this IServiceCollection services)
        {
            // Add hosted services
            // services.AddHostedService<OrderProcessingService>();
            // services.AddHostedService<StockReplenishmentService>();

            return services;
        }

      

    }

    // Configuration options for infrastructure services
    public class InfrastructureOptions
    {
        public bool EnableSensitiveDataLogging { get; set; } = false;
        public bool EnableDetailedErrors { get; set; } = false;
        public int CommandTimeout { get; set; } = 30;
        public bool EnableRetryOnFailure { get; set; } = true;
        public int MaxRetryCount { get; set; } = 3;
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

        // Caching options
        public bool EnableCaching { get; set; } = true;
        public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromMinutes(10);

        // Connection pool options
        public int MaxPoolSize { get; set; } = 100;
        public int MinPoolSize { get; set; } = 0;
    }
}

