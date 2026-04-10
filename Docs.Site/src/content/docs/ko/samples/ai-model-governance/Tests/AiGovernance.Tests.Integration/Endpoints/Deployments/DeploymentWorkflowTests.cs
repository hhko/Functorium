using AiGovernance.Adapters.Presentation.Endpoints.Assessments;
using AiGovernance.Adapters.Presentation.Endpoints.Deployments;
using AiGovernance.Adapters.Presentation.Endpoints.Models;
using AiGovernance.Tests.Integration.Fixtures;

namespace AiGovernance.Tests.Integration.Endpoints.Deployments;

public class DeploymentWorkflowTests : IntegrationTestBase
{
    public DeploymentWorkflowTests(GovernanceFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DeploymentWorkflow_ShouldTransition_FromDraftToPendingReview()
    {
        // Arrange - Create model and deployment
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);

        // Act - Submit for review (Draft -> PendingReview)
        var submitResponse = await Client.PutAsJsonAsync(
            $"/api/deployments/{deploymentId}/submit",
            new { },
            TestContext.Current.CancellationToken);

        // Assert
        submitResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify status changed
        var getResponse = await Client.GetAsync(
            $"/api/deployments/{deploymentId}",
            TestContext.Current.CancellationToken);
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var deployment = await getResponse.Content.ReadFromJsonAsync<GetDeploymentByIdEndpoint.Response>(
            TestContext.Current.CancellationToken);
        deployment.ShouldNotBeNull();
        deployment.Status.ShouldBe("PendingReview");
    }

    [Fact]
    public async Task DeploymentWorkflow_ShouldRejectDoubleSubmit()
    {
        // Arrange - Create model, deployment, and submit for review
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);

        var firstSubmit = await Client.PutAsJsonAsync(
            $"/api/deployments/{deploymentId}/submit",
            new { },
            TestContext.Current.CancellationToken);
        firstSubmit.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act - Try to submit again (PendingReview -> PendingReview is not allowed)
        var secondSubmit = await Client.PutAsJsonAsync(
            $"/api/deployments/{deploymentId}/submit",
            new { },
            TestContext.Current.CancellationToken);

        // Assert - Should fail because PendingReview cannot transition to PendingReview
        var statusCode = (int)secondSubmit.StatusCode;
        statusCode.ShouldBeOneOf(400, 404);
    }

    [Fact]
    public async Task DeploymentWorkflow_ShouldGetDeploymentById_WithCorrectDetails()
    {
        // Arrange
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);

        // Act
        var response = await Client.GetAsync(
            $"/api/deployments/{deploymentId}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetDeploymentByIdEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(deploymentId);
        result.ModelId.ShouldBe(modelId);
        result.EndpointUrl.ShouldBe("https://api.example.com/v1/predict");
        result.Status.ShouldBe("Draft");
        result.Environment.ShouldBe("Staging");
        result.DriftThreshold.ShouldBe(0.05m);
    }

    [Fact]
    public async Task DeploymentWorkflow_ShouldSearchDeployments_WithDefaults()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/deployments",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchDeploymentsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Page.ShouldBe(1);
    }

    [Fact]
    public async Task DeploymentWorkflow_ShouldSearchDeployments_ByModelId()
    {
        // Arrange
        var modelId = await CreateModelAsync();
        await CreateDeploymentAsync(modelId);

        // Act
        var response = await Client.GetAsync(
            $"/api/deployments?modelId={modelId}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchDeploymentsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Deployments.ShouldNotBeNull();
    }

    private async Task<string> CreateModelAsync()
    {
        var request = new
        {
            Name = $"Workflow Model {Guid.NewGuid()}",
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
}
