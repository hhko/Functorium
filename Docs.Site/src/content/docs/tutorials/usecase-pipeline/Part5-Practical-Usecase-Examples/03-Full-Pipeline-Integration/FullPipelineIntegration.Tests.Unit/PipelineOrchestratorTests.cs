using Functorium.Applications.Usecases;
using LanguageExt.Common;
using Shouldly;

namespace FullPipelineIntegration.Tests.Unit;

public class PipelineOrchestratorTests
{
    [Fact]
    public void Execute_CommitsTransaction_WhenCommandSucceeds()
    {
        // Arrange
        var sut = new PipelineOrchestrator<FinResponse<string>>();

        // Act
        var actual = sut.Execute(
            isValid: true,
            isCommand: true,
            handler: () => FinResponse.Succ("OK"));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.ExecutionLog.ShouldContain("Transaction: BEGIN");
        sut.ExecutionLog.ShouldContain("Transaction: COMMIT");
    }

    [Fact]
    public void Execute_RollsBackTransaction_WhenCommandFails()
    {
        // Arrange
        var sut = new PipelineOrchestrator<FinResponse<string>>();

        // Act
        var actual = sut.Execute(
            isValid: true,
            isCommand: true,
            handler: () => FinResponse.Fail<string>(Error.New("business error")));

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.ExecutionLog.ShouldContain("Transaction: ROLLBACK");
    }

    [Fact]
    public void Execute_SkipsTransaction_WhenQuery()
    {
        // Arrange
        var sut = new PipelineOrchestrator<FinResponse<string>>();

        // Act
        sut.Execute(
            isValid: true,
            isCommand: false,
            handler: () => FinResponse.Succ("OK"));

        // Assert
        sut.ExecutionLog.ShouldNotContain("Transaction: BEGIN");
    }

    [Fact]
    public void Execute_ReturnsFail_WhenValidationFails()
    {
        // Arrange
        var sut = new PipelineOrchestrator<FinResponse<string>>();

        // Act
        var actual = sut.Execute(
            isValid: false,
            isCommand: true,
            handler: () => FinResponse.Succ("OK"));

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.ExecutionLog.ShouldContain("Validation: FAIL");
        sut.ExecutionLog.ShouldNotContain("Handler: executed (IsSucc=True)");

        // 외부 Pipeline(Metrics, Tracing, Logging)은 Validation 실패에도 기록됨
        sut.ExecutionLog.ShouldContain("Metrics: Request count++");
        sut.ExecutionLog.ShouldContain("Tracing: Activity started");
        sut.ExecutionLog.ShouldContain("Logging: Request received");
    }

    [Fact]
    public void Execute_ReturnsFail_WhenExceptionThrown()
    {
        // Arrange
        var sut = new PipelineOrchestrator<FinResponse<string>>();

        // Act
        var actual = sut.Execute(
            isValid: true,
            isCommand: false,
            handler: () => throw new InvalidOperationException("boom"));

        // Assert
        actual.IsFail.ShouldBeTrue();
        sut.ExecutionLog.Any(l => l.StartsWith("Exception:")).ShouldBeTrue();
    }

    [Fact]
    public void Execute_LogsCustomPipeline_WhenCustomPipelineProvided()
    {
        // Arrange
        var sut = new PipelineOrchestrator<FinResponse<string>>();

        // Act
        sut.Execute(
            isValid: true,
            isCommand: false,
            handler: () => FinResponse.Succ("OK"),
            customPipeline: response => response);

        // Assert
        sut.ExecutionLog.ShouldContain("Custom: before handler");
        sut.ExecutionLog.ShouldContain("Custom: after handler");
    }

    [Fact]
    public void Execute_SkipsCustomPipeline_WhenNull()
    {
        // Arrange
        var sut = new PipelineOrchestrator<FinResponse<string>>();

        // Act
        sut.Execute(
            isValid: true,
            isCommand: false,
            handler: () => FinResponse.Succ("OK"));

        // Assert
        sut.ExecutionLog.ShouldNotContain("Custom: before handler");
        sut.ExecutionLog.ShouldNotContain("Custom: after handler");
    }

    [Fact]
    public void Execute_LogsAllStages_WhenCommandSucceeds()
    {
        // Arrange
        var sut = new PipelineOrchestrator<FinResponse<string>>();

        // Act
        sut.Execute(
            isValid: true,
            isCommand: true,
            handler: () => FinResponse.Succ("OK"));

        // Assert
        sut.ExecutionLog.ShouldContain("Metrics: Request count++");
        sut.ExecutionLog.ShouldContain("Tracing: Activity started");
        sut.ExecutionLog.ShouldContain("Logging: Request received");
        sut.ExecutionLog.ShouldContain("Validation: PASS");
        sut.ExecutionLog.ShouldContain("Logging: Success");
        sut.ExecutionLog.ShouldContain("Tracing: Activity completed (status=OK)");
        sut.ExecutionLog.ShouldContain("Metrics: Response count++ (success=True)");
    }
}
