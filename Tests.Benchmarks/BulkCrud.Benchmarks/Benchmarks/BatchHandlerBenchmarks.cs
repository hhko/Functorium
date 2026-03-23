using System.Security.Cryptography;
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
/// IDomainEventBatchHandler를 사용한 개별 vs 벌크 성능 비교.
///
/// Section A: 경량 핸들러 (리스트 수집만) — 파이프라인 순수 비용
/// Section B: 현실적 핸들러 (per-event 오버헤드 시뮬레이션) — 실제 사용 패턴
///
/// Section B의 핸들러는 이벤트별로 Dictionary 조회 + 문자열 해시 계산을 수행합니다.
/// 배치 핸들러는 동일한 작업을 벌크로 1회 수행하여 per-event 오버헤드를 제거합니다.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class BatchHandlerBenchmarks
{
    [Params(100, 1_000, 10_000)]
    public int Count;

    private List<Product> _products = null!;

    [GlobalSetup]
    public void Setup()
    {
        _products = TestDataGenerator.GenerateProducts(Count);
    }

    // ═══════════════════════════════════════════════════════════
    // Section A: 경량 핸들러 (파이프라인 순수 비용)
    // ═══════════════════════════════════════════════════════════

    [Benchmark(Baseline = true, Description = "A. Individual: N handler calls (lightweight)")]
    public async Task A_Individual_Lightweight()
    {
        var handler = new LightweightEventHandler();
        var publisher = new HandlerAwarePublisher(handler);
        var collector = new DomainEventCollector();
        collector.TrackRange(_products);

        var sut = new DomainEventPublisher(publisher, collector, NullServiceProvider.Instance);
        await sut.PublishTrackedEvents().Run().RunAsync();
    }

    [Benchmark(Description = "A. Batch: 1 handler call (lightweight)")]
    public async Task A_Batch_Lightweight()
    {
        var batchHandler = new LightweightBatchHandler();
        var publisher = new NoHandlerPublisher();
        var serviceProvider = new BatchHandlerServiceProvider(batchHandler);
        var collector = new DomainEventCollector();
        collector.TrackRange(_products);

        var sut = new DomainEventPublisher(publisher, collector, serviceProvider);
        await sut.PublishTrackedEvents().Run().RunAsync();
    }

    // ═══════════════════════════════════════════════════════════
    // Section B: 현실적 핸들러 (per-event 오버헤드 시뮬레이션)
    //
    // 개별 핸들러: 이벤트마다 Dictionary에 저장 + SHA256 해시 계산
    // 배치 핸들러: 전체 이벤트를 한 번에 Dictionary에 저장 + 1회 해시 계산
    //
    // 이는 실제 사용 패턴을 시뮬레이션합니다:
    //   개별: 이벤트마다 DB INSERT + 알림 전송
    //   배치: 벌크 INSERT + 1회 알림
    // ═══════════════════════════════════════════════════════════

    [Benchmark(Description = "B. Individual: N handler calls (realistic work)")]
    public async Task B_Individual_RealisticWork()
    {
        var handler = new RealisticEventHandler();
        var publisher = new HandlerAwarePublisher(handler);
        var collector = new DomainEventCollector();
        collector.TrackRange(_products);

        var sut = new DomainEventPublisher(publisher, collector, NullServiceProvider.Instance);
        await sut.PublishTrackedEvents().Run().RunAsync();
    }

    [Benchmark(Description = "B. Batch: 1 handler call (realistic work)")]
    public async Task B_Batch_RealisticWork()
    {
        var batchHandler = new RealisticBatchHandler();
        var publisher = new NoHandlerPublisher();
        var serviceProvider = new BatchHandlerServiceProvider(batchHandler);
        var collector = new DomainEventCollector();
        collector.TrackRange(_products);

        var sut = new DomainEventPublisher(publisher, collector, serviceProvider);
        await sut.PublishTrackedEvents().Run().RunAsync();
    }

    [Benchmark(Description = "B. Both: batch + N individual (realistic work)")]
    public async Task B_Both_RealisticWork()
    {
        var handler = new RealisticEventHandler();
        var batchHandler = new RealisticBatchHandler();
        var publisher = new HandlerAwarePublisher(handler);
        var serviceProvider = new BatchHandlerServiceProvider(batchHandler);
        var collector = new DomainEventCollector();
        collector.TrackRange(_products);

        var sut = new DomainEventPublisher(publisher, collector, serviceProvider);
        await sut.PublishTrackedEvents().Run().RunAsync();
    }

    // ─── 경량 핸들러 구현체 ──────────────────────────────

    private sealed class LightweightEventHandler : INotificationHandler<Product.CreatedEvent>
    {
        public List<Product.CreatedEvent> Collected { get; } = [];

        public ValueTask Handle(Product.CreatedEvent notification, CancellationToken ct)
        {
            Collected.Add(notification);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class LightweightBatchHandler : IDomainEventBatchHandler<Product.CreatedEvent>
    {
        public List<Product.CreatedEvent> Collected { get; } = [];

        public ValueTask HandleBatch(Seq<Product.CreatedEvent> events, CancellationToken ct)
        {
            Collected.AddRange(events);
            return ValueTask.CompletedTask;
        }
    }

    // ─── 현실적 핸들러 구현체 ─────────────────────────────

    /// <summary>
    /// 이벤트마다: Dictionary 저장 + SHA256 해시 계산 (per-event 오버헤드)
    /// 실제 시나리오: DB INSERT + 알림 전송 시뮬레이션
    /// </summary>
    private sealed class RealisticEventHandler : INotificationHandler<Product.CreatedEvent>
    {
        public Dictionary<string, byte[]> ProcessedEvents { get; } = new();

        public ValueTask Handle(Product.CreatedEvent notification, CancellationToken ct)
        {
            // per-event 오버헤드: 문자열 생성 + 해시 계산 + Dictionary 저장
            var key = $"{notification.ProductId}:{notification.Name}:{notification.Price}";
            var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key));
            ProcessedEvents[notification.ProductId.ToString()] = hash;
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// 전체 이벤트를 한 번에: 벌크 Dictionary 저장 + 1회 SHA256 해시 계산
    /// 실제 시나리오: 벌크 INSERT + 1회 알림 시뮬레이션
    /// </summary>
    private sealed class RealisticBatchHandler : IDomainEventBatchHandler<Product.CreatedEvent>
    {
        public Dictionary<string, byte[]> ProcessedEvents { get; } = new();

        public ValueTask HandleBatch(Seq<Product.CreatedEvent> events, CancellationToken ct)
        {
            // 벌크 처리: 전체 키 연결 + 1회 해시 계산 + 벌크 Dictionary 저장
            var allKeys = string.Join("|",
                events.Select(e => $"{e.ProductId}:{e.Name}:{e.Price}"));
            var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(allKeys));

            foreach (var evt in events)
            {
                ProcessedEvents[evt.ProductId.ToString()] = hash;
            }

            return ValueTask.CompletedTask;
        }
    }

    // ─── 인프라 구현체 ──────────────────────────────────

    private sealed class HandlerAwarePublisher : IPublisher
    {
        private readonly INotificationHandler<Product.CreatedEvent> _handler;
        public HandlerAwarePublisher(INotificationHandler<Product.CreatedEvent> h) => _handler = h;

        public ValueTask Publish<TNotification>(
            TNotification notification, CancellationToken ct = default)
            where TNotification : INotification
        {
            if (notification is Product.CreatedEvent evt)
                return _handler.Handle(evt, ct);
            return ValueTask.CompletedTask;
        }

        public ValueTask Publish(object notification, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }

    private sealed class NoHandlerPublisher : IPublisher
    {
        public ValueTask Publish<TNotification>(
            TNotification notification, CancellationToken ct = default)
            where TNotification : INotification => ValueTask.CompletedTask;

        public ValueTask Publish(object notification, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }

    private sealed class BatchHandlerServiceProvider : IServiceProvider
    {
        private readonly object _handler;
        public BatchHandlerServiceProvider(IDomainEventBatchHandler<Product.CreatedEvent> h) => _handler = h;

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IDomainEventBatchHandler<Product.CreatedEvent>))
                return _handler;
            return null;
        }
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public static readonly NullServiceProvider Instance = new();
        public object? GetService(Type serviceType) => null;
    }
}
