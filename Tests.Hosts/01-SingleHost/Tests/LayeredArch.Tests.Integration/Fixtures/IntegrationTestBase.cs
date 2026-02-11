namespace LayeredArch.Tests.Integration.Fixtures;

public abstract class IntegrationTestBase : IClassFixture<LayeredArchFixture>
{
    protected HttpClient Client { get; }

    protected IntegrationTestBase(LayeredArchFixture fixture) => Client = fixture.Client;
}
