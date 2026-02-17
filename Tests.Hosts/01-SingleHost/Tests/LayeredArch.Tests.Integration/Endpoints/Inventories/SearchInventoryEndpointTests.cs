using LayeredArch.Adapters.Presentation.Endpoints.Inventories;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.Inventories;

public class SearchInventoryEndpointTests : IntegrationTestBase
{
    public SearchInventoryEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task SearchInventory_ShouldReturn200Ok_WhenNoFiltersProvided()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/inventories/search",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchInventoryEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(20);
    }

    [Fact]
    public async Task SearchInventory_ShouldReturn200Ok_WhenLowStockThresholdProvided()
    {
        // Arrange - create a product with low stock
        var createRequest = new
        {
            Name = $"LowStock Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 100.00m,
            StockQuantity = 3
        };
        await Client.PostAsJsonAsync("/api/products", createRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAsync(
            "/api/inventories/search?lowStockThreshold=10",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchInventoryEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Inventories.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchInventory_ShouldReturn200Ok_WhenPaginationProvided()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/inventories/search?page=1&pageSize=5",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchInventoryEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(5);
    }
}
