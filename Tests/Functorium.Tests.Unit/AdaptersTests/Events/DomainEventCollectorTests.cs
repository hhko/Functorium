using Functorium.Adapters.Events;
using Functorium.Domains.Events;

namespace Functorium.Tests.Unit.AdaptersTests.Events;

public sealed class DomainEventCollectorTests
{
    private readonly DomainEventCollector _sut = new();

    [Fact]
    public void Track_ShouldAddAggregate()
    {
        // Arrange
        var aggregate = new TestAggregate();

        // Act
        _sut.Track(aggregate);

        // Assert — aggregate has no events yet, so GetTrackedAggregates returns empty
        _sut.GetTrackedAggregates().Count.ShouldBe(0);
    }

    [Fact]
    public void Track_ShouldNotDuplicateAggregate()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.AddTestEvent();

        // Act
        _sut.Track(aggregate);
        _sut.Track(aggregate);

        // Assert
        _sut.GetTrackedAggregates().Count.ShouldBe(1);
    }

    [Fact]
    public void GetTrackedAggregates_ShouldReturnOnlyAggregatesWithEvents()
    {
        // Arrange
        var withEvents = new TestAggregate();
        withEvents.AddTestEvent();
        var withoutEvents = new TestAggregate();

        _sut.Track(withEvents);
        _sut.Track(withoutEvents);

        // Act
        var result = _sut.GetTrackedAggregates();

        // Assert
        result.Count.ShouldBe(1);
        result[0].ShouldBeSameAs(withEvents);
    }

    [Fact]
    public void GetTrackedAggregates_ShouldReturnEmpty_WhenNoAggregatesTracked()
    {
        // Act
        var result = _sut.GetTrackedAggregates();

        // Assert
        result.Count.ShouldBe(0);
    }

    [Fact]
    public void GetTrackedAggregates_ShouldReturnMultipleAggregates_WhenAllHaveEvents()
    {
        // Arrange
        var aggregate1 = new TestAggregate();
        aggregate1.AddTestEvent();
        var aggregate2 = new TestAggregate();
        aggregate2.AddTestEvent();

        _sut.Track(aggregate1);
        _sut.Track(aggregate2);

        // Act
        var result = _sut.GetTrackedAggregates();

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public void GetTrackedAggregates_ShouldReturnEmpty_AfterEventsCleared()
    {
        // Arrange
        var aggregate = new TestAggregate();
        aggregate.AddTestEvent();
        _sut.Track(aggregate);
        _sut.GetTrackedAggregates().Count.ShouldBe(1); // 이벤트 있을 때 확인

        // Act
        aggregate.ClearDomainEvents();

        // Assert — 이벤트 클리어 후 GetTrackedAggregates는 빈 결과 반환
        _sut.GetTrackedAggregates().Count.ShouldBe(0);
    }

    /// <summary>
    /// 테스트용 IHasDomainEvents 구현
    /// </summary>
    private sealed class TestAggregate : IDomainEventDrain
    {
        private readonly List<IDomainEvent> _events = [];

        public IReadOnlyList<IDomainEvent> DomainEvents => _events;

        public void ClearDomainEvents() => _events.Clear();

        public void AddTestEvent() => _events.Add(new TestDomainEvent());
    }

    private sealed record TestDomainEvent : IDomainEvent
    {
        public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
        public Ulid EventId { get; } = Ulid.NewUlid();
        public string? CorrelationId => null;
        public string? CausationId => null;
    }
}
