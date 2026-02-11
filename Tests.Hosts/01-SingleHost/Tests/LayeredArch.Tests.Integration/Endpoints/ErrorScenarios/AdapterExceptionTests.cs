using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.ErrorScenarios;

public class AdapterExceptionTests : IntegrationTestBase
{
    public AdapterExceptionTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateProduct_ShouldReturn400BadRequest_WhenAdapterError()
    {
        // Arrange - [adapter-error] triggers exception in Repository adapter
        var request = new
        {
            Name = $"[adapter-error] Product {Guid.NewGuid()}",
            Description = "Adapter error test",
            Price = 100.00m,
            StockQuantity = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
