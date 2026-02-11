using LayeredArch.Application.Usecases.TestErrors;
using LayeredArch.Tests.Integration.Fixtures;

namespace LayeredArch.Tests.Integration.Endpoints.ErrorScenarios;

public class TestErrorEndpointTests : IntegrationTestBase
{
    public TestErrorEndpointTests(LayeredArchFixture fixture) : base(fixture) { }

    [Fact]
    public async Task TestError_ShouldReturn200Ok_WhenSuccess()
    {
        // Arrange
        var request = new
        {
            Scenario = "Success",
            TestMessage = "test message"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/test-error", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("SingleExpected")]
    [InlineData("SingleExceptional")]
    [InlineData("ManyExpected")]
    [InlineData("ManyMixed")]
    [InlineData("GenericExpected")]
    public async Task TestError_ShouldReturn400BadRequest_WhenErrorScenario(string scenario)
    {
        // Arrange
        var request = new
        {
            Scenario = scenario,
            TestMessage = "test message"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/test-error", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
