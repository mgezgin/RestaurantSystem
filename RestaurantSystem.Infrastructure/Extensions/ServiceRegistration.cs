using Microsoft.Extensions.DependencyInjection;

namespace RestaurantSystem.Infrastructure.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddInfrastructureRegistration(this IServiceCollection services)
        {
            return services;
        }
    }
}
