using BenchmarkDotNet.Attributes;
using BulkCrud.Benchmarks.Helpers;
using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using LanguageExt;
using LayeredArch.Domain.AggregateRoots.Products;
using Mediator;

namespace BulkCrud.Benchmarks.Benchmarks;

/// <summary>
/// 도메인 이벤트 발행 성능 비교: 개별 발행 방식 vs 배치 핸들러 방식
/// - Old Bulk: GroupBy → 그룹당 1회 Mediator.Publish
/// - New Individual: GroupBy → N회 Mediator.Publish(event) per event
/// - New Individual + BatchHandler: 위 + opt-in IDomainEventBatchHandler 직접 호출
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DomainEventPublishComparisonBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int Count;

    private List<Product> _products = null!;
    private NoOpPublisher _noOpPublisher = null!;

    [GlobalSetup]
    public void Setup()
    {
        _products = TestDataGenerator.GenerateProducts(Count);
        _noOpPublisher = new NoOpPublisher();
    }

    /// <summary>
    /// 기존 방식 시뮬레이션: 타입별 그룹화 → 그룹당 1회 Mediator.Publish
    /// (BulkDomainEvent 래핑 없이 동일한 호출 패턴만 측정)
    /// </summary>
    [Benchmark(Baseline = true, Description = "Old: 1 Publish per EventType (Bulk)")]
    public async Task Old_BulkPublish_PerType()
    {
        var allEvents = new List<IDomainEvent>(_products.Count);
        foreach (var product in _products)
            allEvents.AddRange(product.DomainEvents);

        foreach (var group in allEvents.GroupBy(e => e.GetType()))
        {
            // 기존: 그룹당 1회 Publish
            await _noOpPublisher.Publish(group.First());
        }
    }

    /// <summary>
    /// 개선된 방식: 타입별 그룹화 → 이벤트마다 개별 Mediator.Publish
    /// </summary>
    [Benchmark(Description = "New: N Publish per Event (Individual)")]
    public async Task New_IndividualPublish()
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

    /// <summary>
    /// 개선된 방식 + 배치 핸들러: 배치 핸들러 직접 호출 + 개별 Publish
    /// </summary>
    [Benchmark(Description = "New: BatchHandler + N Publish (Individual+Batch)")]
    public async Task New_BatchHandlerPlusIndividualPublish()
    {
        var allEvents = new List<IDomainEvent>(_products.Count);
        foreach (var product in _products)
            allEvents.AddRange(product.DomainEvents);

        foreach (var group in allEvents.GroupBy(e => e.GetType()))
        {
            var events = group.ToList();

            // 배치 핸들러 직접 호출 시뮬레이션
            await NoOpBatchProcess(events);

            // 개별 Publish
            foreach (var evt in events)
            {
                await _noOpPublisher.Publish(evt);
            }
        }
    }

    /// <summary>
    /// 순수 개별 발행 (GroupBy 없이): 최적화 없는 단순 반복
    /// </summary>
    [Benchmark(Description = "Baseline: N Publish (No GroupBy)")]
    public async Task Baseline_IndividualPublish_NoGroupBy()
    {
        foreach (var product in _products)
        {
            foreach (var evt in product.DomainEvents)
            {
                await _noOpPublisher.Publish(evt);
            }
        }
    }

    /// <summary>
    /// 실제 DomainEventPublisher.PublishTrackedEvents 전체 경로 시뮬레이션
    /// </summary>
    [Benchmark(Description = "New: Full PublishTrackedEvents Pipeline")]
    public async Task New_FullPublishTrackedEventsPipeline()
    {
        var collector = new DomainEventCollector();
        collector.TrackRange(_products);
        var publisher = new DomainEventPublisher(_noOpPublisher, collector, NoOpServiceProvider.Instance);

        await publisher.PublishTrackedEvents().Run().RunAsync();
    }

    private static ValueTask NoOpBatchProcess(List<IDomainEvent> events)
    {
        // 배치 핸들러 시뮬레이션 (실제로는 검색 인덱스 벌크 업데이트 등)
        return ValueTask.CompletedTask;
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

    private sealed class NoOpServiceProvider : IServiceProvider
    {
        public static readonly NoOpServiceProvider Instance = new();
        public object? GetService(Type serviceType) => null;
    }
}
