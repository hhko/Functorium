using AiGovernance.Adapters.Presentation.Endpoints.Deployments;
using AiGovernance.Adapters.Presentation.Endpoints.Incidents;
using AiGovernance.Adapters.Presentation.Endpoints.Models;
using AiGovernance.Tests.Integration.Fixtures;

namespace AiGovernance.Tests.Integration.Endpoints.Incidents;

public class ReportIncidentEndpointTests : IntegrationTestBase
{
    public ReportIncidentEndpointTests(GovernanceFixture fixture) : base(fixture) { }

    [Fact]
    public async Task ReportIncident_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);

        var request = new
        {
            DeploymentId = deploymentId,
            Severity = "Medium",
            Description = "Model producing unexpected outputs for edge cases"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/incidents", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ReportIncidentEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.IncidentId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ReportIncident_ShouldReturn400BadRequest_WhenSeverityIsInvalid()
    {
        // Arrange
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);

        var request = new
        {
            DeploymentId = deploymentId,
            Severity = "InvalidSeverity",
            Description = "Test incident description"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/incidents", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReportIncident_ShouldReturn400BadRequest_WhenDescriptionIsEmpty()
    {
        // Arrange
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);

        var request = new
        {
            DeploymentId = deploymentId,
            Severity = "Low",
            Description = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/incidents", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetIncidentById_ShouldReturn200Ok_WhenIncidentExists()
    {
        // Arrange
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);
        var incidentId = await CreateIncidentAsync(deploymentId);

        // Act
        var response = await Client.GetAsync(
            $"/api/incidents/{incidentId}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<GetIncidentByIdEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(incidentId);
        result.DeploymentId.ShouldBe(deploymentId);
        result.Severity.ShouldBe("Medium");
        result.Status.ShouldBe("Reported");
    }

    [Fact]
    public async Task GetIncidentById_ShouldReturnNotFoundOrBadRequest_WhenIncidentDoesNotExist()
    {
        // Arrange
        var nonExistentId = Ulid.NewUlid().ToString();

        // Act
        var response = await Client.GetAsync(
            $"/api/incidents/{nonExistentId}",
            TestContext.Current.CancellationToken);

        // Assert
        var statusCode = (int)response.StatusCode;
        statusCode.ShouldBeOneOf(400, 404);
    }

    [Fact]
    public async Task SearchIncidents_ShouldReturn200Ok_WhenNoFiltersProvided()
    {
        // Act
        var response = await Client.GetAsync(
            "/api/incidents",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchIncidentsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Page.ShouldBe(1);
    }

    [Fact]
    public async Task SearchIncidents_ShouldReturn200Ok_WhenFilterByDeploymentId()
    {
        // Arrange
        var modelId = await CreateModelAsync();
        var deploymentId = await CreateDeploymentAsync(modelId);
        await CreateIncidentAsync(deploymentId);

        // Act
        var response = await Client.GetAsync(
            $"/api/incidents?deploymentId={deploymentId}",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<SearchIncidentsEndpoint.Response>(
            TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Incidents.ShouldNotBeNull();
    }

    private async Task<string> CreateModelAsync()
    {
        var request = new
        {
            Name = $"Incident Model {Guid.NewGuid()}",
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

    private async Task<string> CreateIncidentAsync(string deploymentId)
    {
        var request = new
        {
            DeploymentId = deploymentId,
            Severity = "Medium",
            Description = "Model producing unexpected outputs for edge cases"
        };
        var response = await Client.PostAsJsonAsync("/api/incidents", request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ReportIncidentEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        return result.IncidentId;
    }
}
