using LayeredArch.Application.Usecases.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class GetProductByIdEndpointTests : IntegrationTestBase
{
    public GetProductByIdEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetProductById_ShouldReturn200Ok_WhenProductExists()
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

        var created = await createResponse.Content.ReadFromJsonAsync<CreateProductCommand.Response>(TestContext.Current.CancellationToken);
        created.ShouldNotBeNull();

        // Act
        var response = await Client.GetAsync($"/api/products/{created.ProductId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetProductByIdQuery.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(createRequest.Name);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnNotFoundOrBadRequest_WhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/products/{nonExistentId}", TestContext.Current.CancellationToken);

        // Assert
        var statusCode = (int)response.StatusCode;
        statusCode.ShouldBeOneOf(400, 404);
    }
}
