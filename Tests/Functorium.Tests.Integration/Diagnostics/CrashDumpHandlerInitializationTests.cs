using Functorium.Abstractions.Diagnostics;
using Functorium.Tests.Integration.Fixtures;

namespace Functorium.Tests.Integration.Diagnostics;

public class CrashDumpHandlerInitializationTests : CrashDiagnosticsTestBase
{
    public CrashDumpHandlerInitializationTests(CrashDiagnosticsFixture fixture) : base(fixture) { }

    [Fact]
    public void DumpDirectory_ShouldNotBeEmpty_AfterHostStartup()
    {
        // Act
        var dumpDirectory = CrashDumpHandler.DumpDirectory;

        // Assert
        dumpDirectory.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DumpDirectory_ShouldExist_AfterHostStartup()
    {
        // Act
        var dumpDirectory = CrashDumpHandler.DumpDirectory;

        // Assert
        Directory.Exists(dumpDirectory).ShouldBeTrue();
    }
}
