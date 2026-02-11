using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.ErrorScenarios;

public class HandlerExceptionTests : IntegrationTestBase
{
    public HandlerExceptionTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateProduct_ShouldReturn400BadRequest_WhenHandlerError()
    {
        // Arrange - [handler-error] triggers exception in OnProductCreated event handler
        var request = new
        {
            Name = $"[handler-error] Product {Guid.NewGuid()}",
            Description = "Handler error test",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
