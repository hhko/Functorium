namespace Functorium.Tests.Integration.Fixtures;

public abstract class CrashDiagnosticsTestBase : IClassFixture<CrashDiagnosticsFixture>
{
    protected HttpClient Client { get; }

    protected CrashDiagnosticsTestBase(CrashDiagnosticsFixture fixture) => Client = fixture.Client;
}
