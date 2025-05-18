﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Domain.Common.Enums;
using System.Reflection;

namespace RestaurantSystem.Api.Common.Extensions
{
    public static class ServiceRegistration
    {

        public static IServiceCollection AddApiRegistration(this IServiceCollection services)
        {
            services.AddCustomMediator(typeof(Program).Assembly);
            services.AddHttpContextAccessor();
            return services;
        }


        public static AuthorizationOptions AddRolePolicies(this AuthorizationOptions opt)
        {
            opt.AddPolicy("RequireAdmin", policy =>
            {
                policy.AddRequirements(new RoleRequirement(new[] { UserRole.Admin }));
            });

            opt.AddPolicy("RequireCashier", policy =>
            {
                policy.AddRequirements(new RoleRequirement(new[] { UserRole.Admin, UserRole.Cashier }));
            });

            opt.AddPolicy("RequireKitchenStaff", policy =>
            {
                policy.AddRequirements(new RoleRequirement(new[] { UserRole.Admin, UserRole.KitchenStaff }));
            });

            opt.AddPolicy("RequireServer", policy =>
            {
                policy.AddRequirements(new RoleRequirement(new[] { UserRole.Admin, UserRole.Server }));
            });

            opt.AddPolicy("RequireCustomer", policy =>
            {
                policy.AddRequirements(new RoleRequirement(new[] { UserRole.Admin, UserRole.Customer }));
            });

            opt.AddPolicy("RequireAnyRole", policy =>
            {
                policy.AddRequirements(new RoleRequirement(new[] { UserRole.Admin, UserRole.Cashier, UserRole.KitchenStaff, UserRole.Server, UserRole.Customer }));
            });

            return opt;
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
