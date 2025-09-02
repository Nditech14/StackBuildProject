using Microsoft.Extensions.DependencyInjection;
using StackBuld.Application.Interfaces;
using StackBuld.Application.Mappers;
using StackBuld.Application.Services;
using System.Reflection;

namespace StackBuld.Application.Extensions
{
    public static class ApplicationServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register AutoMapper
            
            services.AddAutoMapper(typeof(ProductMappingProfile));

            // Register Application Services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IOrderService, OrderService>();

            // Register all services that implement service interfaces automatically
            // This will scan and register any service that follows the I{Name}Service -> {Name}Service pattern
            RegisterServicesAutomatically(services);

            // Add MediatR if using CQRS pattern (optional)
            // services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // Add FluentValidation if using it (optional)
            // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            return services;
        }

        private static void RegisterServicesAutomatically(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Get all types that end with "Service" and are not interfaces
            var serviceTypes = assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Service") &&
                           !t.IsInterface &&
                           !t.IsAbstract &&
                           t.IsClass)
                .ToList();

            foreach (var serviceType in serviceTypes)
            {
                // Look for corresponding interface (I{ServiceName})
                var interfaceName = $"I{serviceType.Name}";
                var interfaceType = assembly.GetTypes()
                    .FirstOrDefault(t => t.Name == interfaceName && t.IsInterface);

                if (interfaceType != null)
                {
                    services.AddScoped(interfaceType, serviceType);
                }
            }
        }

        // Optional: Add specific service configurations
        public static IServiceCollection AddApplicationServicesWithOptions(this IServiceCollection services, Action<ApplicationOptions> configureOptions = null)
        {
            // Configure options if needed
            if (configureOptions != null)
            {
                services.Configure(configureOptions);
            }

            return services.AddApplicationServices();
        }
    }

}


public class ApplicationOptions
{
    public bool EnableCaching { get; set; } = true;
    public bool EnableValidation { get; set; } = true;
    public int DefaultPageSize { get; set; } = 10;
    public int MaxPageSize { get; set; } = 100;
}
