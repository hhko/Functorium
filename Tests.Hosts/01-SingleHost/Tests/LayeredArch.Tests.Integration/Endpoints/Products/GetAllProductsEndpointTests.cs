using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class GetAllProductsEndpointTests : IntegrationTestBase
{
    public GetAllProductsEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    /// <summary>
    /// Seq&lt;T&gt; 역직렬화 불가로 테스트용 DTO 사용
    /// </summary>
    private sealed record GetAllProductsResponse(List<ProductDto> Products);
    private sealed record ProductDto(string ProductId, string Name, decimal Price, int StockQuantity);

    [Fact]
    public async Task GetAllProducts_ShouldReturn200Ok()
    {
        // Act
        var response = await Client.GetAsync("/api/products", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetAllProductsResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
    }
}
