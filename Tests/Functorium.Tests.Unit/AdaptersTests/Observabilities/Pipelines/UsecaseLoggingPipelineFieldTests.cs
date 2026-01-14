using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Testing.Arrangements.Logging;
using Mediator;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseLoggingPipeline 로그 필드 검증 테스트
/// README.md Observability 섹션에 정의된 필드가 정확히 출력되는지 검증합니다.
/// </summary>
public sealed class UsecaseLoggingPipelineFieldTests
{
    // ===== Request 로그 필드 검증 =====

    [Fact]
    public async Task Command_Request_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task Query_Request_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestQueryRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestQueryRequest, TestResponse>(logger);
        var request = new TestQueryRequest(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestQueryRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots");
    }

    // ===== Success Response 로그 필드 검증 =====

    [Fact]
    public async Task Command_SuccessResponse_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");
        var expectedResponse = TestResponse.CreateSuccess(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "ResponseName");

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }

    [Fact]
    public async Task Query_SuccessResponse_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestQueryRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestQueryRequest, TestResponse>(logger);
        var request = new TestQueryRequest(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var expectedResponse = TestResponse.CreateSuccess(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "QueryResult");

        MessageHandlerDelegate<TestQueryRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }

    // ===== Warning Response 로그 필드 검증 (Expected Error) =====

    [Fact]
    public async Task WarningResponse_WithExpectedError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");

        var error = ErrorCodeFactory.Create(
            errorCode: "User.NotFound",
            errorCurrentValue: "user123",
            errorMessage: "사용자를 찾을 수 없습니다");
        var errorResponse = TestResponse.CreateFail(error);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }

    [Fact]
    public async Task WarningResponse_WithExpectedErrorT_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");

        var error = ErrorCodeFactory.Create(
            errorCode: "Order.NotFound",
            errorCurrentValue: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            errorMessage: "주문을 찾을 수 없습니다");
        var errorResponse = TestResponse.CreateFail(error);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }

    // ===== Error Response 로그 필드 검증 (Exceptional Error) =====

    [Fact]
    public async Task ErrorResponse_WithExceptionalError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");

        var exception = new InvalidOperationException("데이터베이스 연결 실패");
        var error = ErrorCodeFactory.CreateFromException(
            errorCode: "Database.ConnectionFailed",
            exception: exception);
        var errorResponse = TestResponse.CreateFail(error);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }

    [Fact]
    public async Task ErrorResponse_WithException_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");

        var innerException = new TimeoutException("연결 시간 초과");
        var outerException = new InvalidOperationException("작업 실패", innerException);
        var error = ErrorCodeFactory.CreateFromException(
            errorCode: "Operation.Failed",
            exception: outerException);
        var errorResponse = TestResponse.CreateFail(error);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }

    // ===== Aggregate Error 로그 필드 검증 =====

    [Fact]
    public async Task ErrorResponse_WithAggregateError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("TestName");

        var error1 = ErrorCodeFactory.Create(
            errorCode: "Validation.NameRequired",
            errorCurrentValue: "",
            errorMessage: "이름은 필수입니다");
        var error2 = ErrorCodeFactory.Create(
            errorCode: "Validation.EmailInvalid",
            errorCurrentValue: "invalid-email",
            errorMessage: "유효하지 않은 이메일입니다");

        var aggregateError = Error.Many(error1, error2);
        var errorResponse = TestResponse.CreateFail(aggregateError);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots")
            .ScrubMember("response.elapsed");
    }
}
