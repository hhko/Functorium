using LayeredArch.Application.Usecases.Orders;
using LayeredArch.Application.Usecases.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Orders;

public class GetOrderByIdEndpointTests : IntegrationTestBase
{
    public GetOrderByIdEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetOrderById_ShouldReturn200Ok_WhenOrderExists()
    {
        // Arrange - Create a product, then an order
        var productRequest = new
        {
            Name = $"Product {Guid.NewGuid()}",
            Description = "Test Product",
            Price = 150.00m,
            StockQuantity = 50
        };
        var productResponse = await Client.PostAsJsonAsync("/api/products", productRequest, TestContext.Current.CancellationToken);
        productResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var product = await productResponse.Content.ReadFromJsonAsync<CreateProductCommand.Response>(TestContext.Current.CancellationToken);
        product.ShouldNotBeNull();

        var orderRequest = new
        {
            ProductId = product.ProductId,
            Quantity = 3,
            ShippingAddress = "Busan, Korea"
        };
        var orderResponse = await Client.PostAsJsonAsync("/api/orders", orderRequest, TestContext.Current.CancellationToken);
        orderResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var order = await orderResponse.Content.ReadFromJsonAsync<CreateOrderCommand.Response>(TestContext.Current.CancellationToken);
        order.ShouldNotBeNull();

        // Act
        var response = await Client.GetAsync($"/api/orders/{order.OrderId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetOrderByIdQuery.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Quantity.ShouldBe(3);
        result.TotalAmount.ShouldBe(450.00m);
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnNotFoundOrBadRequest_WhenOrderDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/orders/{nonExistentId}", TestContext.Current.CancellationToken);

        // Assert
        var statusCode = (int)response.StatusCode;
        statusCode.ShouldBeOneOf(400, 404);
    }
}
