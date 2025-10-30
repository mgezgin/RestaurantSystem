using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Infrastructure.Persistence.Seeders;

public static class TableSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        // Check if tables already exist
        if (await context.Tables.AnyAsync())
        {
            logger.LogInformation("Tables already seeded, skipping...");
            return;
        }

        logger.LogInformation("Seeding tables...");

        var tables = new List<Table>
        {
            // Indoor Tables (1-10)
            // Table 1 - at entrance
            new Table
            {
                TableNumber = "1",
                MaxGuests = 4,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 50,
                PositionY = 50,
                Width = 100,
                Height = 100,
                CreatedBy = "System"
            },

            // Table 2 - parallel to Table 1 near terrace
            new Table
            {
                TableNumber = "2",
                MaxGuests = 4,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 200,
                PositionY = 50,
                Width = 100,
                Height = 100,
                CreatedBy = "System"
            },

            // Tables 3-10 - Indoor
            new Table
            {
                TableNumber = "3",
                MaxGuests = 6,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 50,
                PositionY = 200,
                Width = 120,
                Height = 100,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "4",
                MaxGuests = 4,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 220,
                PositionY = 200,
                Width = 100,
                Height = 100,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "5",
                MaxGuests = 4,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 370,
                PositionY = 200,
                Width = 100,
                Height = 100,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "6",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 50,
                PositionY = 350,
                Width = 80,
                Height = 80,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "7",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 180,
                PositionY = 350,
                Width = 80,
                Height = 80,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "8",
                MaxGuests = 6,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 310,
                PositionY = 350,
                Width = 120,
                Height = 100,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "9",
                MaxGuests = 4,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 50,
                PositionY = 480,
                Width = 100,
                Height = 100,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "10",
                MaxGuests = 4,
                IsActive = true,
                IsOutdoor = false,
                PositionX = 200,
                PositionY = 480,
                Width = 100,
                Height = 100,
                CreatedBy = "System"
            },

            // Outdoor Tables (11a-14b) - pairs side by side from entrance
            // Table 11a & 11b
            new Table
            {
                TableNumber = "11a",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = true,
                PositionX = 550,
                PositionY = 50,
                Width = 70,
                Height = 70,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "11b",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = true,
                PositionX = 640,
                PositionY = 50,
                Width = 70,
                Height = 70,
                CreatedBy = "System"
            },

            // Table 12a & 12b
            new Table
            {
                TableNumber = "12a",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = true,
                PositionX = 550,
                PositionY = 140,
                Width = 70,
                Height = 70,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "12b",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = true,
                PositionX = 640,
                PositionY = 140,
                Width = 70,
                Height = 70,
                CreatedBy = "System"
            },

            // Table 13a & 13b
            new Table
            {
                TableNumber = "13a",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = true,
                PositionX = 550,
                PositionY = 230,
                Width = 70,
                Height = 70,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "13b",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = true,
                PositionX = 640,
                PositionY = 230,
                Width = 70,
                Height = 70,
                CreatedBy = "System"
            },

            // Table 14a & 14b
            new Table
            {
                TableNumber = "14a",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = true,
                PositionX = 550,
                PositionY = 320,
                Width = 70,
                Height = 70,
                CreatedBy = "System"
            },

            new Table
            {
                TableNumber = "14b",
                MaxGuests = 2,
                IsActive = true,
                IsOutdoor = true,
                PositionX = 640,
                PositionY = 320,
                Width = 70,
                Height = 70,
                CreatedBy = "System"
            }
        };

        await context.Tables.AddRangeAsync(tables);
        await context.SaveChangesAsync();

        logger.LogInformation($"Successfully seeded {tables.Count} tables");
    }
}
