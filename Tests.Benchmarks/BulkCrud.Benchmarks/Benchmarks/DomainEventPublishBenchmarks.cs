using BenchmarkDotNet.Attributes;
using BulkCrud.Benchmarks.Helpers;
using Functorium.Adapters.Events;
using Functorium.Domains.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Mediator;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// 도메인 이벤트 발행 성능: Per-Event Sequential vs BulkDomainEvent GroupBy
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DomainEventPublishBenchmarks
{
    [Params(1_000, 10_000)]
    public int Count;

    private List<Product> _products = null!;
    private NoOpPublisher _noOpPublisher = null!;

    [GlobalSetup]
    public void Setup()
    {
        _products = TestDataGenerator.GenerateProducts(Count);
        _noOpPublisher = new NoOpPublisher();
    }

    [Benchmark(Baseline = true, Description = "Per-Event Sequential Publish")]
    public async Task PerEvent_Sequential()
    {
        foreach (var product in _products)
        {
            foreach (var evt in product.DomainEvents)
            {
                await _noOpPublisher.Publish(evt);
            }
        }
    }

    [Benchmark(Description = "BulkDomainEvent GroupBy Publish")]
    public async Task BulkEvent_GroupBy()
    {
        var allEvents = new List<IDomainEvent>(_products.Count);
        foreach (var product in _products)
            allEvents.AddRange(product.DomainEvents);

        foreach (var group in allEvents.GroupBy(e => e.GetType()))
        {
            var bulkEvent = new BulkDomainEvent(group.ToList(), group.Key);
            await _noOpPublisher.Publish(bulkEvent);
        }
    }

    [Benchmark(Description = "DomainEventCollector TrackRange (regression)")]
    public void Collector_TrackRange()
    {
        var collector = new DomainEventCollector();
        collector.TrackRange(_products);
    }

    internal sealed class NoOpPublisher : IPublisher
    {
        public ValueTask Publish<TNotification>(
            TNotification notification,
            CancellationToken cancellationToken = default)
            where TNotification : INotification
            => ValueTask.CompletedTask;

        public ValueTask Publish(
            object notification,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}
