using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Events;
using Functorium.Applications.Persistence;
using Functorium.Domains.Events;
using Functorium.Testing.Arrangements.Effects;
using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseTransactionPipeline 테스트
/// Command에 대해 UoW.SaveChanges + 도메인 이벤트 발행을 자동 처리하는 파이프라인 테스트
/// </summary>
public sealed class UsecaseTransactionPipelineTests
{
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDomainEventPublisher _eventPublisher = Substitute.For<IDomainEventPublisher>();
    private readonly ILogger<UsecaseTransactionPipeline<TestCommandRequest, TestResponse>> _logger =
        NullLogger<UsecaseTransactionPipeline<TestCommandRequest, TestResponse>>.Instance;

    private UsecaseTransactionPipeline<TestCommandRequest, TestResponse> CreateCommandPipeline()
        => new(_unitOfWork, _eventPublisher, _logger);

    [Fact]
    public async Task Handle_Command_ShouldCallSaveChanges_WhenHandlerSucceeds()
    {
        // Arrange
        var pipeline = CreateCommandPipeline();
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));
        _eventPublisher.PublishTrackedEvents(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(new Seq<PublishResult>()));

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        var result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        _unitOfWork.Received(1).SaveChanges(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Command_ShouldNotCallSaveChanges_WhenHandlerFails()
    {
        // Arrange
        var pipeline = CreateCommandPipeline();
        var request = new TestCommandRequest("Test");
        var failResponse = TestResponse.CreateFail(Error.New("Handler failed"));

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(failResponse);

        // Act
        var result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        _unitOfWork.DidNotReceive().SaveChanges(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Command_ShouldReturnFail_WhenSaveChangesFails()
    {
        // Arrange
        var pipeline = CreateCommandPipeline();
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());
        var saveError = Error.New("SaveChanges failed");

        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Fail<LanguageExt.Unit>(saveError));

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        var result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Command_ShouldCallPublishTrackedEvents_WhenSaveChangesSucceeds()
    {
        // Arrange
        var pipeline = CreateCommandPipeline();
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));
        _eventPublisher.PublishTrackedEvents(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(new Seq<PublishResult>()));

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        var result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        _eventPublisher.Received(1).PublishTrackedEvents(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Command_ShouldReturnSuccess_WhenEventPublishFails()
    {
        // Arrange — 이벤트 발행 실패 시에도 데이터는 이미 커밋됨, 성공 응답 유지
        var pipeline = CreateCommandPipeline();
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));
        _eventPublisher.PublishTrackedEvents(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Fail<Seq<PublishResult>>(Error.New("Publish failed")));

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        var result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert — 성공 응답 유지 (데이터는 이미 커밋됨)
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Query_ShouldBypass_WithoutCallingSaveChanges()
    {
        // Arrange
        var queryPipeline = new UsecaseTransactionPipeline<TestQueryRequest, TestResponse>(
            _unitOfWork, _eventPublisher,
            NullLogger<UsecaseTransactionPipeline<TestQueryRequest, TestResponse>>.Instance);
        var request = new TestQueryRequest(Guid.NewGuid());
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestQueryRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        var result = await queryPipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        _unitOfWork.DidNotReceive().SaveChanges(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Command_ShouldSucceed_WhenNoEventsToPublish()
    {
        // Arrange — 추적된 Aggregate 없이 SaveChanges만 성공하는 경우
        var pipeline = CreateCommandPipeline();
        var request = new TestCommandRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        _unitOfWork.SaveChanges(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(unit));
        _eventPublisher.PublishTrackedEvents(Arg.Any<CancellationToken>())
            .Returns(FinTFactory.Succ(new Seq<PublishResult>()));

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        var result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        _unitOfWork.Received(1).SaveChanges(Arg.Any<CancellationToken>());
        _eventPublisher.Received(1).PublishTrackedEvents(Arg.Any<CancellationToken>());
    }
}
