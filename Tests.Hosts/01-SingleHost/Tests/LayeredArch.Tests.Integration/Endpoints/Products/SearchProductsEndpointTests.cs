using LayeredArch.Adapters.Presentation.Endpoints.Products;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Products;

public class SearchProductsEndpointTests : IntegrationTestBase
{
    public SearchProductsEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task SearchProducts_ShouldReturn200Ok_WhenNoFiltersProvided()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/products/search",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchProductsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task SearchProducts_ShouldReturn200Ok_WhenPaginationProvided()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/products/search?page=1&pageSize=5",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchProductsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(5);
    }

    [Fact]
    public async Task SearchProducts_ShouldReturn200Ok_WhenSortByProvided()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/products/search?sortBy=Price&sortDirection=desc",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchProductsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchProducts_ShouldReturn200Ok_WhenPriceRangeProvided()
    {
        // Arrange - create a product first
        var createRequest = new
        {
            Name = $"SearchTest Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 150.00m,
            StockQuantity = 10
        };
        await Client.PostAsJsonAsync("/api/products", createRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAsync(
            "/api/products/search?minPrice=100&maxPrice=200",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchProductsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Products.ShouldNotBeNull();
    }
}
