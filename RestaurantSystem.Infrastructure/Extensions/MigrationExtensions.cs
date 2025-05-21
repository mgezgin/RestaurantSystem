using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RestaurantSystem.Infrastructure.Persistence;


namespace RestaurantSystem.Infrastructure.Extensions
{
    public static class MigrationExtensions
    {
        public static async Task MigrateApplicationDatabaseAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                logger.LogInformation("Applying migrations for database");
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations successfully applied");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying migrations");
                throw;
            }
        }

        public static async Task EnsureDatabaseCreatedAsync(this IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                if (!await dbContext.Database.CanConnectAsync())
                {
                    logger.LogInformation("Creating database as it doesn't exist");
                    await dbContext.Database.EnsureCreatedAsync();
                    logger.LogInformation("Database created successfully");
                }
                else
                {
                    logger.LogInformation("Database connection verified");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while ensuring database exists");
                throw;
            }
        }
    }
}
