using AiGovernance.Adapters.Presentation.Endpoints.Assessments;
using AiGovernance.Adapters.Presentation.Endpoints.Deployments;
using AiGovernance.Adapters.Presentation.Endpoints.Models;
using AiGovernance.Tests.Integration.Fixtures;

namespace AiGovernance.Tests.Integration.Endpoints.Assessments;

public class InitiateAssessmentEndpointTests : IntegrationTestBase
{
    public InitiateAssessmentEndpointTests(GovernanceFixture fixture) : base(fixture) { }

    [Fact]
    public async Task InitiateAssessment_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange - Create model and deployment
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);

        var request = new
        {
            ModelId = modelId,
            DeploymentId = deploymentId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/assessments", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<InitiateAssessmentEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.AssessmentId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task InitiateAssessment_ShouldReturn400BadRequest_WhenModelIdIsInvalid()
    {
        // Arrange
        var request = new
        {
            ModelId = "invalid-id",
            DeploymentId = Ulid.NewUlid().ToString()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/assessments", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAssessmentById_ShouldReturn200Ok_WhenAssessmentExists()
    {
        // Arrange
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);
        var assessmentId = await CreateAssessmentAsync(modelId, deploymentId);

        // Act
        var response = await Client.GetAsync(
            $"/api/assessments/{assessmentId}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetAssessmentByIdEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(assessmentId);
        result.ModelId.ShouldBe(modelId);
        result.DeploymentId.ShouldBe(deploymentId);
        result.Status.ShouldBe("Initiated");
        result.Criteria.ShouldNotBeNull();
        result.Criteria.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task GetAssessmentById_ShouldReturnNotFoundOrBadRequest_WhenAssessmentDoesNotExist()
    {
        // Arrange
        var nonExistentId = Ulid.NewUlid().ToString();

        // Act
        var response = await Client.GetAsync(
            $"/api/assessments/{nonExistentId}",
            TestContext.Current.CancellationToken);

        // Assert
        var statusCode = (int)response.StatusCode;
        statusCode.ShouldBeOneOf(400, 404);
    }

    private async Task<string> CreateModelAsync()
    {
        var request = new
        {
            Name = $"Assessment Model {Guid.NewGuid()}",
            Version = "1.0.0",
            Purpose = "Text generation chatbot"
        };
        var response = await Client.PostAsJsonAsync("/api/models", request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RegisterModelEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        return result.ModelId;
    }

    private async Task<string> CreateDeploymentAsync(string modelId)
    {
        var request = new
        {
            ModelId = modelId,
            EndpointUrl = "https://api.example.com/v1/predict",
            Environment = "Staging",
            DriftThreshold = 0.05m
        };
        var response = await Client.PostAsJsonAsync("/api/deployments", request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateDeploymentEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        return result.DeploymentId;
    }

    private async Task<string> CreateAssessmentAsync(string modelId, string deploymentId)
    {
        var request = new
        {
            ModelId = modelId,
            DeploymentId = deploymentId
        };
        var response = await Client.PostAsJsonAsync("/api/assessments", request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<InitiateAssessmentEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        return result.AssessmentId;
    }
}
