using AiGovernance.Adapters.Presentation.Endpoints.Deployments;
using AiGovernance.Adapters.Presentation.Endpoints.Models;
using AiGovernance.Tests.Integration.Fixtures;

namespace AiGovernance.Tests.Integration.Endpoints.Deployments;

public class CreateDeploymentEndpointTests : IntegrationTestBase
{
    public CreateDeploymentEndpointTests(GovernanceFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreateDeployment_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange - Create a model first
        var modelId = await CreateModelAsync();

        var request = new
        {
            ModelId = modelId,
            EndpointUrl = "https://api.example.com/v1/predict",
            Environment = "Staging",
            DriftThreshold = 0.05m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/deployments", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateDeploymentEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.DeploymentId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateDeployment_ShouldReturn400BadRequest_WhenModelIdIsInvalid()
    {
        // Arrange
        var request = new
        {
            ModelId = "invalid-id",
            EndpointUrl = "https://api.example.com/v1/predict",
            Environment = "Staging",
            DriftThreshold = 0.05m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/deployments", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDeployment_ShouldReturn400BadRequest_WhenEnvironmentIsInvalid()
    {
        // Arrange
        var modelId = await CreateModelAsync();

        var request = new
        {
            ModelId = modelId,
            EndpointUrl = "https://api.example.com/v1/predict",
            Environment = "InvalidEnv",
            DriftThreshold = 0.05m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/deployments", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateDeployment_ShouldReturn400BadRequest_WhenDriftThresholdOutOfRange()
    {
        // Arrange
        var modelId = await CreateModelAsync();

        var request = new
        {
            ModelId = modelId,
            EndpointUrl = "https://api.example.com/v1/predict",
            Environment = "Staging",
            DriftThreshold = 1.5m
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/deployments", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<string> CreateModelAsync()
    {
        var request = new
        {
            Name = $"Deploy Test Model {Guid.NewGuid()}",
            Version = "1.0.0",
            Purpose = "Text generation chatbot"
        };
        var response = await Client.PostAsJsonAsync("/api/models", request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RegisterModelEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        return result.ModelId;
    }
}
