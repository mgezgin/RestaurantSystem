﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RestaurantSystem.Infrastructure.Persistence;
using RestaurantSystem.IntegrationTests.Common;

namespace RestaurantSystem.IntegrationTests.Infrastructure;
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public TestWebApplicationFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Setup database
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            // Add DbContext using the test container
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(dataSource);
            });

            // Replace authentication with test authentication
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", options => { });

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
                    .RequireAuthenticatedUser()
                    .Build();
            });

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });

        builder.UseEnvironment("Test");
    }
}
