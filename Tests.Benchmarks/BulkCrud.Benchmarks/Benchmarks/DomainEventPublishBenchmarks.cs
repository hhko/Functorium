using BenchmarkDotNet.Attributes;
using BulkCrud.Benchmarks.Helpers;
using Functorium.Adapters.Events;
using Functorium.Domains.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Mediator;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// 도메인 이벤트 발행 성능: 개별 이벤트 발행 벤치마크
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

    [Benchmark(Baseline = true, Description = "Individual Event Publish")]
    public async Task IndividualEvent_Publish()
    {
        foreach (var product in _products)
        {
            foreach (var evt in product.DomainEvents)
            {
                await _noOpPublisher.Publish(evt);
            }
        }
    }

    [Benchmark(Description = "Individual Event Publish with GroupBy")]
    public async Task IndividualEvent_GroupBy_Publish()
    {
        var allEvents = new List<IDomainEvent>(_products.Count);
        foreach (var product in _products)
            allEvents.AddRange(product.DomainEvents);

        foreach (var group in allEvents.GroupBy(e => e.GetType()))
        {
            foreach (var evt in group)
            {
                await _noOpPublisher.Publish(evt);
            }
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
