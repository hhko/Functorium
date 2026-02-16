using LayeredArch.Adapters.Presentation.Endpoints.Orders;
using LayeredArch.Adapters.Presentation.Endpoints.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Orders;

public class CreateOrderEndpointTests : IntegrationTestBase
{
    public CreateOrderEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateOrder_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange - Create a product first
        var productRequest = new
        {
            Name = $"Product {Guid.NewGuid()}",
            Description = "Test Product",
            Price = 100.00m,
            StockQuantity = 50
        };
        var productResponse = await Client.PostAsJsonAsync("/api/products", productRequest, TestContext.Current.CancellationToken);
        productResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var product = await productResponse.Content.ReadFromJsonAsync<CreateProductEndpoint.Response>(TestContext.Current.CancellationToken);
        product.ShouldNotBeNull();

        var orderRequest = new
        {
            ProductId = product.ProductId,
            Quantity = 2,
            ShippingAddress = "Seoul, Korea"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateOrderEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Quantity.ShouldBe(2);
        result.TotalAmount.ShouldBe(200.00m);
    }

    [Fact]
    public async Task CreateOrder_ShouldReturn400BadRequest_WhenProductDoesNotExist()
    {
        // Arrange
        var orderRequest = new
        {
            ProductId = Guid.NewGuid().ToString(),
            Quantity = 2,
            ShippingAddress = "Seoul, Korea"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/orders", orderRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
