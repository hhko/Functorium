using LayeredArch.Adapters.Presentation.Endpoints.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class DeductStockEndpointTests : IntegrationTestBase
{
    public DeductStockEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DeductStock_ShouldReturn200Ok_WhenSufficientStock()
    {
        // Arrange - Create a product first
        var createRequest = new
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 10
        };
        var createResponse = await Client.PostAsJsonAsync("/api/products", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateProductEndpoint.Response>(TestContext.Current.CancellationToken);
        created.ShouldNotBeNull();

        var deductRequest = new { Quantity = 3 };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/products/{created.ProductId}/deduct-stock", deductRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DeductStockEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.RemainingStock.ShouldBe(7);
    }

    [Fact]
    public async Task DeductStock_ShouldReturn400BadRequest_WhenInsufficientStock()
    {
        // Arrange - Create a product with low stock
        var createRequest = new
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 2
        };
        var createResponse = await Client.PostAsJsonAsync("/api/products", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateProductEndpoint.Response>(TestContext.Current.CancellationToken);
        created.ShouldNotBeNull();

        var deductRequest = new { Quantity = 10 };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/products/{created.ProductId}/deduct-stock", deductRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
