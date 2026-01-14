using Cqrs06EndpointLayered.Applications.Commands;
using Cqrs06EndpointLayered.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cqrs06EndpointLayered.Tests.Integration.Endpoints;

public class CreateProductEndpointTests : WebApplicationFixture
{
    public CreateProductEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange
        var request = new
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateProductCommand.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(request.Name);
        result.Description.ShouldBe(request.Description);
        result.Price.ShouldBe(request.Price);
        result.StockQuantity.ShouldBe(request.StockQuantity);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn400BadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new
        {
            Name = "",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn400BadRequest_WhenPriceIsZero()
    {
        // Arrange
        var request = new
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 0.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
