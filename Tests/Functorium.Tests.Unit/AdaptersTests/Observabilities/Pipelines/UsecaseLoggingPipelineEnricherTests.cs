using Functorium.Adapters.Pipelines;
using Functorium.Applications.Observabilities;

using Mediator;

using NSubstitute;

using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// CtxEnricherPipeline의 Enricher 통합 테스트
/// IUsecaseCtxEnricher를 통한 3-Pillar ctx.* 속성 추가 검증
/// </summary>
public sealed class UsecaseLoggingPipelineEnricherTests
{
    [Fact]
    public async Task Handle_WithEnricher_CallsEnrichRequest()
    {
        // Arrange
        var enricher = Substitute.For<IUsecaseCtxEnricher<TestCommandRequest, TestResponse>>();

        var pipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        enricher.Received(1).EnrichRequest(request);
    }

    [Fact]
    public async Task Handle_WithEnricher_CallsEnrichResponse()
    {
        // Arrange
        var enricher = Substitute.For<IUsecaseCtxEnricher<TestCommandRequest, TestResponse>>();

        var pipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        enricher.Received(1).EnrichResponse(request, expectedResponse);
    }

    [Fact]
    public async Task Handle_WithEnricher_DisposesRequestEnrichment()
    {
        // Arrange
        var enricher = Substitute.For<IUsecaseCtxEnricher<TestCommandRequest, TestResponse>>();
        var disposable = Substitute.For<IDisposable>();
        enricher.EnrichRequest(Arg.Any<TestCommandRequest>()).Returns(disposable);

        var pipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
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
        var enricher = Substitute.For<IUsecaseCtxEnricher<TestCommandRequest, TestResponse>>();
        var disposable = Substitute.For<IDisposable>();
        enricher.EnrichResponse(Arg.Any<TestCommandRequest>(), Arg.Any<TestResponse>()).Returns(disposable);

        var pipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
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
        // Arrange: enricher = null (기본값)
        var pipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>();
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
        var enricher = Substitute.For<IUsecaseCtxEnricher<TestCommandRequest, TestResponse>>();
        enricher.EnrichRequest(Arg.Any<TestCommandRequest>()).Returns((IDisposable?)null);
        enricher.EnrichResponse(Arg.Any<TestCommandRequest>(), Arg.Any<TestResponse>()).Returns((IDisposable?)null);

        var pipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
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
        var enricher = Substitute.For<IUsecaseCtxEnricher<TestCommandRequest, TestResponse>>();

        var pipeline = new CtxEnricherPipeline<TestCommandRequest, TestResponse>(enricher);
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
