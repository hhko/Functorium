using LayeredArch.Adapters.Presentation.Endpoints.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class GetAllProductsEndpointTests : IntegrationTestBase
{
    public GetAllProductsEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetAllProducts_ShouldReturn200Ok()
    {
        // Act
        var response = await Client.GetAsync("/api/products", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetAllProductsEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
    }
}
