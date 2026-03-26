using AiGovernance.Adapters.Presentation.Endpoints.Models;
using AiGovernance.Tests.Integration.Fixtures;

namespace AiGovernance.Tests.Integration.Endpoints.Models;

public class GetModelByIdEndpointTests : IntegrationTestBase
{
    public GetModelByIdEndpointTests(GovernanceFixture fixture) : base(fixture) { }

    [Fact]
    public async Task GetModelById_ShouldReturn200Ok_WhenModelExists()
    {
        // Arrange - Create a model first
        var createRequest = new
        {
            Name = $"Test Model {Guid.NewGuid()}",
            Version = "1.0.0",
            Purpose = "Text generation chatbot"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/models", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<RegisterModelEndpoint.Response>(TestContext.Current.CancellationToken);
        created.ShouldNotBeNull();

        // Act
        var response = await Client.GetAsync($"/api/models/{created.ModelId}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetModelByIdEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Name.ShouldBe(createRequest.Name);
        result.Version.ShouldBe(createRequest.Version);
        result.Purpose.ShouldBe(createRequest.Purpose);
        result.RiskTier.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetModelById_ShouldReturnNotFoundOrBadRequest_WhenModelDoesNotExist()
    {
        // Arrange
        var nonExistentId = Ulid.NewUlid().ToString();

        // Act
        var response = await Client.GetAsync($"/api/models/{nonExistentId}", TestContext.Current.CancellationToken);

        // Assert
        var statusCode = (int)response.StatusCode;
        statusCode.ShouldBeOneOf(400, 404);
    }
}
