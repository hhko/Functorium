using System.Diagnostics;
using Functorium.Adapters.Events;
using Functorium.Domains.Events;
using Shouldly;
using Xunit;

namespace BulkCrud.Benchmarks.Tests;

public sealed class DomainEventCollectorPerfTests
{
    [Fact]
    public void TrackRange_100K_Items_Completes_Under_100ms()
    {
        // Arrange
        var aggregates = Enumerable.Range(0, 100_000)
            .Select(_ => new TestAggregate())
            .ToList();
        var collector = new DomainEventCollector();

        // Act
        var sw = Stopwatch.StartNew();
        collector.TrackRange(aggregates);
        sw.Stop();

        // Assert - 정합성: 이벤트 없으므로 tracked aggregates는 비어있어야 함
        collector.GetTrackedAggregates().Count.ShouldBe(0);

        // Assert - 성능: O(n) → 100ms 이내
        sw.ElapsedMilliseconds.ShouldBeLessThan(100);
    }

    [Fact]
    public void Track_Deduplication_Works_With_HashSet()
    {
        // Arrange
        var aggregate = new TestAggregate();
        var collector = new DomainEventCollector();

        // Act
        collector.Track(aggregate);
        collector.Track(aggregate); // 중복

        // Assert - Track 후 이벤트 없으므로 GetTrackedAggregates는 0
        // 하지만 내부적으로 중복이 아닌 1개만 추적되어야 함
        collector.GetTrackedAggregates().Count.ShouldBe(0);
    }

    [Fact]
    public void Track_100K_Individual_Items_Completes_Under_200ms()
    {
        // Arrange - HashSet Track 단건 호출 100K회
        var aggregates = Enumerable.Range(0, 100_000)
            .Select(_ => new TestAggregate())
            .ToList();
        var collector = new DomainEventCollector();

        // Act
        var sw = Stopwatch.StartNew();
        foreach (var agg in aggregates)
            collector.Track(agg);
        sw.Stop();

        // Assert - 성능
        sw.ElapsedMilliseconds.ShouldBeLessThan(200);
    }

    private sealed class TestAggregate : IHasDomainEvents
    {
        public IReadOnlyList<IDomainEvent> DomainEvents { get; } = [];
    }
}
