using Functorium.Abstractions.Errors;
using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;
using Mediator;
using NSubstitute;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Events;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class DomainEventPublisherTests
{
    private readonly IPublisher _mockPublisher;
    private readonly IDomainEventCollector _mockCollector;
    private readonly DomainEventPublisher _sut;

    public DomainEventPublisherTests()
    {
        _mockPublisher = Substitute.For<IPublisher>();
        _mockCollector = Substitute.For<IDomainEventCollector>();
        _mockCollector.GetTrackedAggregates().Returns(new List<IHasDomainEvents>());
        _mockCollector.GetDirectlyTrackedEvents().Returns(new List<IDomainEvent>());
        _sut = new DomainEventPublisher(_mockPublisher, _mockCollector);
    }

    #region Publish Tests

    [Fact]
    public async Task Publish_ReturnsSuccess_WhenEventIsValid()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Publish_CallsPublisher_WithCorrectEvent()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");

        // Act
        await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        await _mockPublisher.Received(1).Publish(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_PassesCancellationToken_ToMediator()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        // Act
        await _sut.Publish(domainEvent, expectedToken).Run().RunAsync();

        // Assert
        await _mockPublisher.Received(1).Publish(domainEvent, expectedToken);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Publish_ReturnsFail_WhenPublisherThrowsException()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var expectedException = new InvalidOperationException("Handler failed");
        _mockPublisher
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw expectedException);

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: error =>
            {
                error.IsExceptional.ShouldBeTrue();
                error.ShouldBeAssignableTo<IHasErrorCode>();
                ((IHasErrorCode)error).ErrorCode.ShouldContain("PublishFailed");
            });
    }

    [Fact]
    public async Task Publish_ReturnsFail_WhenOperationCancelled()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        _mockPublisher
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new OperationCanceledException());

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: error =>
            {
                error.IsExpected.ShouldBeTrue();
                error.ShouldBeAssignableTo<IHasErrorCode>();
                ((IHasErrorCode)error).ErrorCode.ShouldContain("PublishCancelled");
            });
    }

    #endregion

    #region PublishTrackedEvents Tests

    [Fact]
    public async Task PublishTrackedEvents_PublishesEachEventIndividually()
    {
        // Arrange
        var aggregate1 = new TestAggregate();
        aggregate1.AddEvent(new TestDomainEvent("e1"));
        var aggregate2 = new TestAggregate();
        aggregate2.AddEvent(new TestDomainEvent("e2"));

        _mockCollector.GetTrackedAggregates().Returns(
            new List<IHasDomainEvents> { aggregate1, aggregate2 });

        // Act
        var actual = await _sut.PublishTrackedEvents().Run().RunAsync();

        // Assert — 동일 타입 2개 이벤트 → 2회 개별 발행
        actual.IsSucc.ShouldBeTrue();
        await _mockPublisher.Received(2).Publish(
            Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishTrackedEvents_GroupsMultipleTypes_PublishesPerEvent()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.AddEvent(new TestDomainEvent("e1"));
        aggregate.AddEvent(new AnotherTestEvent());

        _mockCollector.GetTrackedAggregates().Returns(
            new List<IHasDomainEvents> { aggregate });

        // Act
        var actual = await _sut.PublishTrackedEvents().Run().RunAsync();

        // Assert — 2가지 이벤트 타입 각 1개 → 2회 개별 발행
        actual.IsSucc.ShouldBeTrue();
        await _mockPublisher.Received(2).Publish(
            Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishTrackedEvents_ReturnsEmpty_WhenNoEvents()
    {
        // Act
        var actual = await _sut.PublishTrackedEvents().Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        await _mockPublisher.DidNotReceive().Publish(
            Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishTrackedEvents_ClearsAggregateEvents()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.AddEvent(new TestDomainEvent("e1"));

        _mockCollector.GetTrackedAggregates().Returns(
            new List<IHasDomainEvents> { aggregate });

        // Act
        await _sut.PublishTrackedEvents().Run().RunAsync();

        // Assert — Aggregate 이벤트는 발행 후 클리어
        aggregate.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public async Task PublishTrackedEvents_ReturnsPublishResultWithFailures_WhenPublishThrows()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.AddEvent(new TestDomainEvent("e1"));

        _mockCollector.GetTrackedAggregates().Returns(
            new List<IHasDomainEvents> { aggregate });

        _mockPublisher
            .Publish(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new InvalidOperationException("Handler failed"));

        // Act
        var actual = await _sut.PublishTrackedEvents().Run().RunAsync();

        // Assert — 실패해도 Succ(PublishResult with failures)를 반환
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: results =>
            {
                results.Count.ShouldBe(1);
                results[0].HasFailures.ShouldBeTrue();
            },
            Fail: _ => throw new Exception("Expected success"));
    }

    #endregion

    // ─── 테스트 헬퍼 ───────────────────────────────────

    private sealed class TestAggregate : IDomainEventDrain
    {
        private readonly List<IDomainEvent> _events = [];
        public IReadOnlyList<IDomainEvent> DomainEvents => _events;
        public void ClearDomainEvents() => _events.Clear();
        public void AddEvent(IDomainEvent e) => _events.Add(e);
    }

    private sealed record AnotherTestEvent : DomainEvent
    {
        public AnotherTestEvent() : base() { }
    }
}
