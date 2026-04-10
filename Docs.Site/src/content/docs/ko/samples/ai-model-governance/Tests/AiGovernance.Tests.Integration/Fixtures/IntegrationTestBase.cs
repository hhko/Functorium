namespace AiGovernance.Tests.Integration.Fixtures;

public abstract class IntegrationTestBase : IClassFixture<GovernanceFixture>
{
    protected HttpClient Client { get; }

    protected IntegrationTestBase(GovernanceFixture fixture) => Client = fixture.Client;
}
