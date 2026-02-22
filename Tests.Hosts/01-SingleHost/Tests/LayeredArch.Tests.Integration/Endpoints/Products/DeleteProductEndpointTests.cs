using LayeredArch.Adapters.Presentation.Endpoints.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class DeleteProductEndpointTests : IntegrationTestBase
{
    public DeleteProductEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DeleteProduct_ShouldReturn200Ok_WhenProductExists()
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

        // Act
        var response = await Client.DeleteAsync($"/api/products/{created.ProductId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DeleteProductEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.ProductId.ShouldBe(created.ProductId);
    }

    [Fact]
    public async Task DeleteProduct_ShouldReturn404NotFound_WhenProductNotExists()
    {
        // Arrange
        var nonExistentId = Ulid.NewUlid().ToString();

        // Act
        var response = await Client.DeleteAsync($"/api/products/{nonExistentId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_DeletedProductShouldNotBeReturnedByGetById()
    {
        // Arrange - Create a product
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

        // Act - Delete the product
        var deleteResponse = await Client.DeleteAsync($"/api/products/{created.ProductId}", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert - GetById should return 404
        var getResponse = await Client.GetAsync($"/api/products/{created.ProductId}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
