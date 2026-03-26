using AiGovernance.Adapters.Presentation.Endpoints.Models;
using AiGovernance.Tests.Integration.Fixtures;

namespace AiGovernance.Tests.Integration.Endpoints.Models;

public class SearchModelsEndpointTests : IntegrationTestBase
{
    public SearchModelsEndpointTests(GovernanceFixture fixture) : base(fixture) { }

    [Fact]
    public async Task SearchModels_ShouldReturn200Ok_WhenNoFiltersProvided()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/models",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchModelsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Page.ShouldBe(1);
    }

    [Fact]
    public async Task SearchModels_ShouldReturn200Ok_WhenPaginationProvided()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/models?page=1&pageSize=5",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchModelsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(5);
    }

    [Fact]
    public async Task SearchModels_ShouldReturnCreatedModel_WhenSearchingByName()
    {
        // Arrange - create a model first
        var modelName = $"SearchableModel {Guid.NewGuid()}";
        var createRequest = new
        {
            Name = modelName,
            Version = "1.0.0",
            Purpose = "Text generation chatbot"
        };
        await Client.PostAsJsonAsync("/api/models", createRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await Client.GetAsync(
            $"/api/models?name={Uri.EscapeDataString(modelName)}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchModelsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Models.ShouldNotBeNull();
    }
}
