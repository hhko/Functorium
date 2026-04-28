using BenchmarkDotNet.Attributes;
using Functorium.Domains.Events;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// DomainEventCollector Track 성능: List+Any O(n²) vs HashSet O(n)
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DomainEventCollectorBenchmarks
{
    [Params(1_000, 10_000, 100_000)]
    public int Count;

    private List<TestAggregate> _aggregates = null!;

    [GlobalSetup]
    public void Setup()
    {
        _aggregates = Enumerable.Range(0, Count)
            .Select(_ => new TestAggregate())
            .ToList();
    }

    [Benchmark(Description = "HashSet Track (O(n))")]
    public void HashSet_Track()
    {
        var collector = new Functorium.Adapters.Events.DomainEventCollector();
        foreach (var agg in _aggregates)
            collector.Track(agg);
    }

    [Benchmark(Description = "HashSet TrackRange (O(n))")]
    public void HashSet_TrackRange()
    {
        var collector = new Functorium.Adapters.Events.DomainEventCollector();
        collector.TrackRange(_aggregates);
    }

    [Benchmark(Baseline = true, Description = "List+Any Track (O(n²))")]
    public void ListAny_Track()
    {
        var tracked = new List<IHasDomainEvents>();
        foreach (var agg in _aggregates)
        {
            if (!tracked.Any(a => ReferenceEquals(a, agg)))
                tracked.Add(agg);
        }
    }

    internal sealed class TestAggregate : IHasDomainEvents
    {
        public IReadOnlyList<IDomainEvent> DomainEvents { get; } = [];
    }
}
