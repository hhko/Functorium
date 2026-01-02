using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cqrs04Endpoint.WebApi.Tests.Unit.EndpointsTests;

public sealed class CreateProductEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public CreateProductEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100m,
            StockQuantity = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateProduct_InvalidPrice_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = -100m,  // 유효하지 않은 가격
            StockQuantity = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Name = "",
            Description = "Test Description",
            Price = 100m,
            StockQuantity = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_NegativeStockQuantity_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 100m,
            StockQuantity = -10  // 유효하지 않은 재고 수량
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
