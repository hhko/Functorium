using Cqrs06EndpointLayered.Applications.Commands;
using Cqrs06EndpointLayered.Applications.Queries;
using Cqrs06EndpointLayered.Tests.Integration.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cqrs06EndpointLayered.Tests.Integration.Endpoints;

public class GetProductEndpointTests : WebApplicationFixture
{
    public GetProductEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    /// <summary>
    /// 테스트용 Response DTO - LanguageExt.Seq 대신 List 사용
    /// </summary>
    private sealed record GetAllProductsResponse(List<ProductDto> Products);
    private sealed record ProductDto(Guid ProductId, string Name, decimal Price, int StockQuantity);

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

    [Fact]
    public async Task GetProductById_ShouldReturn404NotFound_WhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/products/{nonExistentId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

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

        var createdProduct = await createResponse.Content.ReadFromJsonAsync<CreateProductCommand.Response>(TestContext.Current.CancellationToken);
        createdProduct.ShouldNotBeNull();

        // Act
        var response = await Client.GetAsync($"/api/products/{createdProduct.ProductId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetProductByIdQuery.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.ProductId.ShouldBe(createdProduct.ProductId);
        result.Name.ShouldBe(createRequest.Name);
    }
}
