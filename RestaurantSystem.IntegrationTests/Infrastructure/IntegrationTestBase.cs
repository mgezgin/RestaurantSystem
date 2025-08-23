using Microsoft.Extensions.DependencyInjection;
using RestaurantSystem.Infrastructure.Persistence;
using RestaurantSystem.IntegrationTests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSystem.IntegrationTests.Infrastructure;
[Collection("Database")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly DatabaseFixture DatabaseFixture;
    protected TestWebApplicationFactory Factory = null!;
    protected HttpClient Client = null!;

    protected IntegrationTestBase(DatabaseFixture databaseFixture)
    {
        DatabaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
    }

    public async Task InitializeAsync()
    {
        // Create factory and client after DatabaseFixture is initialized
        Factory = new TestWebApplicationFactory(DatabaseFixture.ConnectionString);
        Client = Factory.CreateClient();

        await DatabaseFixture.ResetDatabaseAsync();
        await SeedTestData();
    }

    public Task DisposeAsync()
    {
        Client?.Dispose();
        Factory?.Dispose();
        return Task.CompletedTask;
    }

    protected virtual async Task SeedTestData()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await TestDataSeeder.SeedBasicDataAsync(context);
    }

    protected void AuthenticateAsAdmin()
    {
        Client.DefaultRequestHeaders.Remove("X-Test-Admin");
        Client.DefaultRequestHeaders.Add("X-Test-Admin", "true");
    }

    protected void AuthenticateAsUser()
    {
        Client.DefaultRequestHeaders.Remove("X-Test-Admin");
    }
}