using Functorium.Abstractions.Diagnostics;
using Functorium.Tests.Integration.Fixtures;

namespace Functorium.Tests.Integration.Diagnostics;

public class CrashDiagnosticsEndpointTests : CrashDiagnosticsTestBase
{
    private const string BasePath = "/api/diagnostics/crash";

    public CrashDiagnosticsEndpointTests(CrashDiagnosticsFixture fixture) : base(fixture) { }

    private sealed record DumpDirectoryResponse(string Path, bool Exists);
    private sealed record DumpFilesResponse(string Directory, DumpFileInfo[] Files);
    private sealed record DumpFileInfo(string Name, string FullPath, long SizeBytes, DateTime CreatedAt);

    [Fact]
    public async Task GetDumpDirectory_ShouldReturn200Ok_WithValidPath()
    {
        // Act
        var response = await Client.GetAsync($"{BasePath}/dump-directory", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DumpDirectoryResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Path.ShouldNotBeNullOrWhiteSpace();
        result.Exists.ShouldBeTrue();
    }

    [Fact]
    public async Task GetDumpDirectory_PathShouldMatchHandlerProperty()
    {
        // Act
        var response = await Client.GetAsync($"{BasePath}/dump-directory", TestContext.Current.CancellationToken);

        // Assert
        var result = await response.Content.ReadFromJsonAsync<DumpDirectoryResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Path.ShouldBe(CrashDumpHandler.DumpDirectory);
    }

    [Fact]
    public async Task ListDumpFiles_ShouldReturn200Ok()
    {
        // Act
        var response = await Client.GetAsync($"{BasePath}/dumps", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DumpFilesResponse>(TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Files.ShouldNotBeNull();
    }

    [Fact]
    public async Task NormalException_ShouldReturn200Ok_WhenCatchEnabled()
    {
        // Act
        var response = await Client.PostAsync($"{BasePath}/normal-exception?shouldCatch=true", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
