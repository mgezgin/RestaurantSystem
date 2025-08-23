using FluentAssertions;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Products.Dtos;
using RestaurantSystem.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantSystem.IntegrationTests.Features.Products;

public class ProductsControllerTests : IntegrationTestBase
{
    public ProductsControllerTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }

    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductSummaryDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data?.Items.Should().HaveCountGreaterOrEqualTo(2); // From seed data
    }
}
