using LayeredArch.Adapters.Presentation.Endpoints.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class RestoreProductEndpointTests : IntegrationTestBase
{
    public RestoreProductEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task RestoreProduct_ShouldReturn200Ok_WhenDeletedProductExists()
    {
        // Arrange - Create and delete a product
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

        var deleteResponse = await Client.DeleteAsync($"/api/products/{created.ProductId}", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act - Restore the product
        var response = await Client.PostAsJsonAsync($"/api/products/{created.ProductId}/restore", new { }, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<RestoreProductEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.ProductId.ShouldBe(created.ProductId);
        result.Name.ShouldBe(createRequest.Name);
        result.Price.ShouldBe(createRequest.Price);
    }

    [Fact]
    public async Task RestoreProduct_RestoredProductShouldBeReturnedByGetById()
    {
        // Arrange - Create, delete, and restore a product
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

        await Client.DeleteAsync($"/api/products/{created.ProductId}", TestContext.Current.CancellationToken);

        // Act - Restore the product
        var restoreResponse = await Client.PostAsJsonAsync($"/api/products/{created.ProductId}/restore", new { }, TestContext.Current.CancellationToken);
        restoreResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Assert - GetById should return the product
        var getResponse = await Client.GetAsync($"/api/products/{created.ProductId}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var product = await getResponse.Content.ReadFromJsonAsync<GetProductByIdEndpoint.Response>(TestContext.Current.CancellationToken);
        product.ShouldNotBeNull();
        product.Name.ShouldBe(createRequest.Name);
    }
}
