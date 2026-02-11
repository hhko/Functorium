using LayeredArch.Application.Usecases.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class CreateProductEndpointTests : IntegrationTestBase
{
    public CreateProductEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

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
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 0.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_ShouldReturn400BadRequest_WhenDuplicateName()
    {
        // Arrange
        var name = $"Duplicate Product {Guid.NewGuid()}";
        var request = new
        {
            Name = name,
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Create first product
        var firstResponse = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act - try to create duplicate
        var response = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
