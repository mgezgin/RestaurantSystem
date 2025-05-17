using Microsoft.AspNetCore.Hosting;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common;
using System.Reflection;

namespace RestaurantSystem.Api.Extensions
{
    public static class ServiceRegistration
    {

        public static IServiceCollection AddApiRegistration(this IServiceCollection services)
        {
            services.AddCustomMediator(typeof(Program).Assembly);

            services.AddHttpContextAccessor();
            
            return services;
        }


        public static IServiceCollection AddCustomMediator(this IServiceCollection services, params Assembly[] assemblies)
        {
            // Register the mediator
            services.AddSingleton<CustomMediator>();

            // Register all command handlers
            RegisterCommandHandlers(services, assemblies);

            // Register all query handlers
            RegisterQueryHandlers(services, assemblies);

            return services;
        }

        private static void RegisterCommandHandlers(IServiceCollection services, Assembly[] assemblies)
        {
            var commandHandlerTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)));

            foreach (var handlerType in commandHandlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

                foreach (var interfaceType in interfaces)
                {
                    services.AddTransient(interfaceType, handlerType);
                }
            }
        }

        private static void RegisterQueryHandlers(IServiceCollection services, Assembly[] assemblies)
        {
            var queryHandlerTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)));

            foreach (var handlerType in queryHandlerTypes)
            {
                var interfaces = handlerType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

                foreach (var interfaceType in interfaces)
                {
                    services.AddTransient(interfaceType, handlerType);
                }
            }
        }
    }
}
