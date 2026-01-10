using Functorium.Adapters.Observabilities.Pipelines;
using LanguageExt.Common;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseLoggingPipeline 테스트
/// 요청/응답 로깅 파이프라인 테스트
/// </summary>
public sealed class UsecaseLoggingPipelineTests
{
    [Fact]
    public async Task Handle_SuccessfulRequest_ReturnsSuccessResponse()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_QueryRequest_ReturnsSuccessfully()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestQueryRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestQueryRequest, TestResponse>(logger);
        var request = new TestQueryRequest(Guid.NewGuid());
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestQueryRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_FailedRequest_ReturnsFailResponse()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var errorResponse = TestResponse.CreateFail(Error.New("Test error"));

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        result.Error!.Message.ShouldBe("Test error");
    }

    [Fact]
    public async Task Handle_Request_CompletesWithDelay()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            async (_, _) =>
            {
                await Task.Delay(10); // Small delay to measure
                return expectedResponse;
            };

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_PreservesResponseFromHandler()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var expectedId = Guid.NewGuid();
        var expectedName = "TestName";
        var expectedResponse = TestResponse.CreateSuccess(expectedId, expectedName);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedId);
        result.Name.ShouldBe(expectedName);
    }

    [Fact]
    public async Task Handle_CommandAndQueryRequests_BothSucceed()
    {
        // Arrange
        var commandLogger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var queryLogger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestQueryRequest, TestResponse>>();
        var commandPipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(commandLogger);
        var queryPipeline = new UsecaseLoggingPipeline<TestQueryRequest, TestResponse>(queryLogger);

        var commandRequest = new TestCommandRequest("Command");
        var queryRequest = new TestQueryRequest(Guid.NewGuid());
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> commandNext =
            (_, _) => ValueTask.FromResult(expectedResponse);
        MessageHandlerDelegate<TestQueryRequest, TestResponse> queryNext =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        var commandResult = await commandPipeline.Handle(commandRequest, commandNext, CancellationToken.None);
        var queryResult = await queryPipeline.Handle(queryRequest, queryNext, CancellationToken.None);

        // Assert
        commandResult.IsSucc.ShouldBeTrue();
        queryResult.IsSucc.ShouldBeTrue();
    }
}
