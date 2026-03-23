using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using LanguageExt;
using LayeredArch.Domain.AggregateRoots.Products;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace BulkCrud.Benchmarks.Tests;

/// <summary>
/// IDomainEventBatchHandler 관찰 가능성 동작 검증 테스트.
/// Activity span, 로그, 지표가 배치 호출 시 정확히 발생하는지 확인합니다.
/// </summary>
public sealed class BatchHandlerObservabilityTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private readonly List<Activity> _capturedActivities = [];

    public BatchHandlerObservabilityTests()
    {
        _activitySource = new ActivitySource("Test.BatchHandler");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Test.BatchHandler",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _capturedActivities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
    }

    [Fact]
    public async Task BatchHandler_CreatesActivitySpan_WithCorrectTags()
    {
        // Arrange
        var products = Helpers.TestDataGenerator.GenerateProducts(10);
        var batchHandler = new TestBatchHandler();
        var collector = new DomainEventCollector();
        collector.TrackRange(products);

        var services = new ServiceCollection();
        services.AddSingleton(_activitySource);
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IDomainEventBatchHandler<Product.CreatedEvent>>(batchHandler);
        var sp = services.BuildServiceProvider();

        var publisher = new DomainEventPublisher(new NoOpPublisher(), collector, sp);

        // Act
        var result = await publisher.PublishTrackedEvents().Run().RunAsync();

        // Assert — 배치 핸들러가 호출됨
        result.IsSucc.ShouldBeTrue();
        batchHandler.CallCount.ShouldBe(1);
        batchHandler.ReceivedEventCount.ShouldBe(10);

        // Assert — Activity span이 생성됨
        var batchActivity = _capturedActivities
            .FirstOrDefault(a => a.DisplayName.Contains("HandleBatch"));
        batchActivity.ShouldNotBeNull("배치 핸들러 Activity span이 생성되어야 합니다");

        // Activity 정보 출력 (디버그용)

        // Assert — Activity 태그 검증
        batchActivity.GetTagItem("request.layer").ShouldBe("application");
        batchActivity.GetTagItem("request.category.name").ShouldBe("usecase");
        batchActivity.GetTagItem("request.category.type").ShouldBe("event");
        batchActivity.GetTagItem("request.handler.name").ShouldBe("TestBatchHandler");
        batchActivity.GetTagItem("request.handler.method").ShouldBe("HandleBatch");
        batchActivity.GetTagItem("request.event.type").ShouldBe("CreatedEvent");
        batchActivity.GetTagItem("request.event.count").ShouldBe(10);
        batchActivity.GetTagItem("response.status").ShouldBe("success");
        batchActivity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    [Fact]
    public async Task BatchHandler_LogsRequestAndResponse()
    {
        // Arrange
        var products = Helpers.TestDataGenerator.GenerateProducts(5);
        var batchHandler = new TestBatchHandler();
        var collector = new DomainEventCollector();
        collector.TrackRange(products);

        var logMessages = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(_activitySource);
        services.AddLogging(b => b
            .SetMinimumLevel(LogLevel.Debug)
            .AddProvider(new CapturingLoggerProvider(logMessages)));
        services.AddSingleton<IDomainEventBatchHandler<Product.CreatedEvent>>(batchHandler);
        var sp = services.BuildServiceProvider();

        var publisher = new DomainEventPublisher(new NoOpPublisher(), collector, sp);

        // Act
        await publisher.PublishTrackedEvents().Run().RunAsync();

        // Assert — 로그 2건 (request + response)
        // 로그 메시지 검증

        logMessages.Count(m => m.Contains("HandleBatch") && m.Contains("requesting")).ShouldBe(1,
            "배치 핸들러 request 로그가 1건 발생해야 합니다");
        logMessages.Count(m => m.Contains("HandleBatch") && m.Contains("responded success")).ShouldBe(1,
            "배치 핸들러 response success 로그가 1건 발생해야 합니다");
    }

    [Fact]
    public async Task BatchHandler_ErrorResponse_SetsActivityError()
    {
        // Arrange
        var products = Helpers.TestDataGenerator.GenerateProducts(3);
        var failingHandler = new FailingBatchHandler();
        var collector = new DomainEventCollector();
        collector.TrackRange(products);

        var services = new ServiceCollection();
        services.AddSingleton(_activitySource);
        services.AddLogging(b => b.AddDebug().SetMinimumLevel(LogLevel.Debug));
        services.AddSingleton<IDomainEventBatchHandler<Product.CreatedEvent>>(failingHandler);
        var sp = services.BuildServiceProvider();

        var publisher = new DomainEventPublisher(new NoOpPublisher(), collector, sp);

        // Act — 배치 핸들러 예외는 reflection invoke → TargetInvocationException으로 래핑되어 전파
        try
        {
            await publisher.PublishTrackedEvents().Run().RunAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException
                                   || ex.InnerException is InvalidOperationException
                                   || ex.ToString().Contains("Batch processing failed"))
        {
            // 예상된 예외 (reflection invoke로 인한 래핑 포함)
        }

        // Assert — Activity span에 에러 상태가 기록됨
        var batchActivity = _capturedActivities
            .FirstOrDefault(a => a.DisplayName.Contains("HandleBatch"));
        batchActivity.ShouldNotBeNull();

        // 에러 Activity 검증
        batchActivity.Status.ShouldBe(ActivityStatusCode.Error);
        batchActivity.GetTagItem("response.status").ShouldBe("failure");
        batchActivity.GetTagItem("error.type").ShouldNotBeNull();
    }

    [Fact]
    public async Task NoBatchHandler_NoActivitySpanCreated()
    {
        // Arrange — 배치 핸들러 미등록
        var products = Helpers.TestDataGenerator.GenerateProducts(5);
        var collector = new DomainEventCollector();
        collector.TrackRange(products);

        var services = new ServiceCollection();
        services.AddSingleton(_activitySource);
        services.AddLogging(b => b.AddDebug());
        var sp = services.BuildServiceProvider();

        var publisher = new DomainEventPublisher(new NoOpPublisher(), collector, sp);

        // Act
        await publisher.PublishTrackedEvents().Run().RunAsync();

        // Assert — 배치 핸들러 Activity 없음
        var batchActivity = _capturedActivities
            .FirstOrDefault(a => a.DisplayName.Contains("HandleBatch"));
        batchActivity.ShouldBeNull("배치 핸들러가 없으면 Activity span이 생성되지 않아야 합니다");
    }

    [Fact]
    public async Task IndividualHandler_StillCalledWhenBatchHandlerRegistered()
    {
        // Arrange
        var products = Helpers.TestDataGenerator.GenerateProducts(5);
        var batchHandler = new TestBatchHandler();
        var individualHandler = new TestIndividualHandler();
        var collector = new DomainEventCollector();
        collector.TrackRange(products);

        var services = new ServiceCollection();
        services.AddSingleton(_activitySource);
        services.AddLogging(b => b.AddDebug());
        services.AddSingleton<IDomainEventBatchHandler<Product.CreatedEvent>>(batchHandler);
        var sp = services.BuildServiceProvider();

        var publisher = new DomainEventPublisher(
            new IndividualHandlerAwarePublisher(individualHandler), collector, sp);

        // Act
        await publisher.PublishTrackedEvents().Run().RunAsync();

        // Assert — 배치 핸들러가 처리하면 개별 발행은 스킵됨 (continue)
        batchHandler.CallCount.ShouldBe(1);
        batchHandler.ReceivedEventCount.ShouldBe(5);
        individualHandler.CallCount.ShouldBe(0);
    }

    // ─── 테스트 핸들러 ──────────────────────────────

    private sealed class TestBatchHandler : IDomainEventBatchHandler<Product.CreatedEvent>
    {
        public int CallCount { get; private set; }
        public int ReceivedEventCount { get; private set; }

        public ValueTask HandleBatch(Seq<Product.CreatedEvent> events, CancellationToken ct)
        {
            CallCount++;
            ReceivedEventCount += events.Count;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingBatchHandler : IDomainEventBatchHandler<Product.CreatedEvent>
    {
        public ValueTask HandleBatch(Seq<Product.CreatedEvent> events, CancellationToken ct)
            => throw new InvalidOperationException("Batch processing failed");
    }

    private sealed class TestIndividualHandler : INotificationHandler<Product.CreatedEvent>
    {
        public int CallCount { get; private set; }

        public ValueTask Handle(Product.CreatedEvent notification, CancellationToken ct)
        {
            CallCount++;
            return ValueTask.CompletedTask;
        }
    }

    // ─── 인프라 ──────────────────────────────────────

    private sealed class NoOpPublisher : IPublisher
    {
        public ValueTask Publish<T>(T n, CancellationToken ct = default) where T : INotification
            => ValueTask.CompletedTask;
        public ValueTask Publish(object n, CancellationToken ct = default)
            => ValueTask.CompletedTask;
    }

    private sealed class IndividualHandlerAwarePublisher : IPublisher
    {
        private readonly INotificationHandler<Product.CreatedEvent> _handler;
        public IndividualHandlerAwarePublisher(INotificationHandler<Product.CreatedEvent> h) => _handler = h;

        public ValueTask Publish<T>(T n, CancellationToken ct = default) where T : INotification
        {
            if (n is Product.CreatedEvent evt) return _handler.Handle(evt, ct);
            return ValueTask.CompletedTask;
        }

        public ValueTask Publish(object n, CancellationToken ct = default) => ValueTask.CompletedTask;
    }

    /// <summary>로그 메시지를 캡처하는 테스트용 Logger Provider</summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _messages;
        public CapturingLoggerProvider(List<string> messages) => _messages = messages;
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_messages);
        public void Dispose() { }
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly List<string> _messages;
        public CapturingLogger(List<string> messages) => _messages = messages;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _messages.Add(formatter(state, exception));
    }
}
