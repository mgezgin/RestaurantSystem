using Microsoft.Extensions.DependencyInjection;
using RestaurantSystem.Api.Common.Conventers;
using RestaurantSystem.Infrastructure.Persistence;
using RestaurantSystem.IntegrationTests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new StringEnumConverterFactory() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InitializeAsync()
    {
        // Create factory and client after DatabaseFixture is initialized
        Factory = new TestWebApplicationFactory(DatabaseFixture.ConnectionString);
        Client = Factory.CreateClient();

        Client.DefaultRequestHeaders.Accept.Clear();

        Client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));


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

    // Helper methods for JSON serialization/deserialization with correct options
    protected async Task<T?> GetFromJsonAsync<T>(string requestUri)
    {
        var response = await Client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    protected async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await Client.PostAsync(requestUri, content);
    }

    protected async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await Client.PutAsync(requestUri, content);
    }

    protected async Task<T?> ReadResponseAsync<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

}