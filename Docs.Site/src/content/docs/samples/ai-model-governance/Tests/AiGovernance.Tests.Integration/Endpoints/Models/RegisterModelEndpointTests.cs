using AiGovernance.Adapters.Presentation.Endpoints.Models;
using AiGovernance.Tests.Integration.Fixtures;

namespace AiGovernance.Tests.Integration.Endpoints.Models;

public class RegisterModelEndpointTests : IntegrationTestBase
{
    public RegisterModelEndpointTests(GovernanceFixture fixture) : base(fixture) { }

    [Fact]
    public async Task RegisterModel_ShouldReturn201Created_WhenRequestIsValid()
    {
        // Arrange
        var request = new
        {
            Name = $"GPT-4 Clone {Guid.NewGuid()}",
            Version = "1.0.0",
            Purpose = "Text generation chatbot"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/models", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<RegisterModelEndpoint.Response>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.ModelId.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task RegisterModel_ShouldReturn400BadRequest_WhenNameIsEmpty()
    {
        // Arrange
        var request = new
        {
            Name = "",
            Version = "1.0.0",
            Purpose = "Text generation chatbot"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/models", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterModel_ShouldReturn400BadRequest_WhenVersionIsEmpty()
    {
        // Arrange
        var request = new
        {
            Name = $"Test Model {Guid.NewGuid()}",
            Version = "",
            Purpose = "Text generation chatbot"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/models", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterModel_ShouldReturn400BadRequest_WhenPurposeIsEmpty()
    {
        // Arrange
        var request = new
        {
            Name = $"Test Model {Guid.NewGuid()}",
            Version = "1.0.0",
            Purpose = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/models", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
