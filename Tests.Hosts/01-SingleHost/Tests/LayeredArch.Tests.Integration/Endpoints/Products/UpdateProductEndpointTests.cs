using LayeredArch.Adapters.Presentation.Endpoints.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class UpdateProductEndpointTests : IntegrationTestBase
{
    public UpdateProductEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task UpdateProduct_ShouldReturn200Ok_WhenRequestIsValid()
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

        var updateRequest = new
        {
            Name = $"Updated Product {Guid.NewGuid()}",
            Description = "Updated Description",
            Price = 200.00m
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/products/{created.ProductId}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UpdateProductEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(updateRequest.Name);
        result.Price.ShouldBe(updateRequest.Price);
    }

    [Fact]
    public async Task UpdateProduct_ShouldReturn400BadRequest_WhenNameIsEmpty()
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

        var updateRequest = new
        {
            Name = "",
            Description = "Updated Description",
            Price = 200.00m
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/products/{created.ProductId}", updateRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
