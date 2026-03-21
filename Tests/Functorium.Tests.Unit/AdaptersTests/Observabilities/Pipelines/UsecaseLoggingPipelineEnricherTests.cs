using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Observabilities;

using Mediator;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseLoggingPipeline의 Enricher 통합 테스트
/// IUsecaseLogEnricher를 통한 LogContext 속성 추가 검증
/// </summary>
public sealed class UsecaseLoggingPipelineEnricherTests
{
    [Fact]
    public async Task Handle_WithEnricher_CallsEnrichRequestLog()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = Substitute.For<IUsecaseLogEnricher<TestCommandRequest, TestResponse>>();

        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger, enricher);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        enricher.Received(1).EnrichRequestLog(request);
    }

    [Fact]
    public async Task Handle_WithEnricher_CallsEnrichResponseLog()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = Substitute.For<IUsecaseLogEnricher<TestCommandRequest, TestResponse>>();

        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger, enricher);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        enricher.Received(1).EnrichResponseLog(request, expectedResponse);
    }

    [Fact]
    public async Task Handle_WithEnricher_DisposesRequestEnrichment()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = Substitute.For<IUsecaseLogEnricher<TestCommandRequest, TestResponse>>();
        var disposable = Substitute.For<IDisposable>();
        enricher.EnrichRequestLog(Arg.Any<TestCommandRequest>()).Returns(disposable);

        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger, enricher);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        disposable.Received().Dispose();
    }

    [Fact]
    public async Task Handle_WithEnricher_DisposesResponseEnrichment()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = Substitute.For<IUsecaseLogEnricher<TestCommandRequest, TestResponse>>();
        var disposable = Substitute.For<IDisposable>();
        enricher.EnrichResponseLog(Arg.Any<TestCommandRequest>(), Arg.Any<TestResponse>()).Returns(disposable);

        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger, enricher);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        disposable.Received().Dispose();
    }

    [Fact]
    public async Task Handle_WithoutEnricher_BehavesIdentically()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();

        // enricher = null (기본값)
        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse actual = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_EnricherReturnsNull_NoException()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = Substitute.For<IUsecaseLogEnricher<TestCommandRequest, TestResponse>>();
        enricher.EnrichRequestLog(Arg.Any<TestCommandRequest>()).Returns((IDisposable?)null);
        enricher.EnrichResponseLog(Arg.Any<TestCommandRequest>(), Arg.Any<TestResponse>()).Returns((IDisposable?)null);

        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger, enricher);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse actual = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithEnricher_PreservesResponse()
    {
        // Arrange
        var logger = NullLoggerFactory.Instance.CreateLogger<UsecaseLoggingPipeline<TestCommandRequest, TestResponse>>();
        var enricher = Substitute.For<IUsecaseLogEnricher<TestCommandRequest, TestResponse>>();

        var pipeline = new UsecaseLoggingPipeline<TestCommandRequest, TestResponse>(logger, enricher);
        var request = new TestCommandRequest("Test");
        var expectedId = Guid.NewGuid();
        var expectedResponse = TestResponse.CreateSuccess(expectedId);

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse actual = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Id.ShouldBe(expectedId);
    }
}
