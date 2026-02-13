using Functorium.Testing.Arrangements.Hosting;

namespace Functorium.Tests.Integration.Fixtures;

public class CrashDiagnosticsFixture : HostTestFixture<Program>
{
    // CrashDiagnosticsEndpoints는 Development 환경에서만 매핑됨
    protected override string EnvironmentName => "Development";
}
