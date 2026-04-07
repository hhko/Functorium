using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Events;
using Functorium.Applications.Observabilities;
using Functorium.Testing.Arrangements.Logging;
using Functorium.Tests.Unit.DomainsTests.Entities;
using Mediator;
using Serilog.Context;
using Serilog.Events;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

/// <summary>
/// ObservableDomainEventNotificationPublisher + IDomainEventCtxEnricher 통합 로그 필드 구조 검증 테스트.
/// Enricher가 LogContext.PushProperty로 Push한 ctx.* 필드가
/// 실제 Handler Request/Response 로그 이벤트에 정확히 출력되는지 스냅샷으로 고정합니다.
/// </summary>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class DomainEventHandlerEnricherLoggingStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IMeterFactory _meterFactory;
    private readonly Microsoft.Extensions.Options.IOptions<OpenTelemetryOptions> _openTelemetryOptions;

    public DomainEventHandlerEnricherLoggingStructureTests()
    {
        _activitySource = new ActivitySource("Test.DomainEventHandlerEnricherLogging");
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestHandlerEnricherLogging" });
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];
        public Meter Create(MeterOptions options) { var meter = new Meter(options); _meters.Add(meter); return meter; }
        public void Dispose() { foreach (var meter in _meters) meter.Dispose(); _meters.Clear(); }
    }

    // ===== Request 로그 + Enricher ctx.* 필드 검증 =====

    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Request_WithEnricher_Should_Log_EnrichedFields()
    {
        // Arrange
        using var context = new LogTestContext(LogEventLevel.Debug, enrichFromLogContext: true);
        using var loggerFactory = new TestLoggerFactory(context);
        var enricher = new TestDomainEventCtxEnricher();
        var serviceProvider = new EnricherServiceProvider(enricher);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions, serviceProvider);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Ulid.Parse("01H00000000000000000000001"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        var handler = new TestLoggingDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        await Verify(context.ExtractFirstLogData())
            .UseDirectory("Snapshots/DomainEventHandlerEnricherLoggingStructure");
    }

    // ===== Success Response 로그 + Enricher ctx.* 필드 검증 =====

    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task SuccessResponse_WithEnricher_Should_Log_EnrichedFields()
    {
        // Arrange
        using var context = new LogTestContext(LogEventLevel.Debug, enrichFromLogContext: true);
        using var loggerFactory = new TestLoggerFactory(context);
        var enricher = new TestDomainEventCtxEnricher();
        var serviceProvider = new EnricherServiceProvider(enricher);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions, serviceProvider);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Ulid.Parse("01H00000000000000000000002"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        var handler = new TestLoggingDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventHandlerEnricherLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Error Response 로그 + Enricher ctx.* 필드 검증 =====

    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task ErrorResponse_WithEnricher_Should_Log_EnrichedFields()
    {
        // Arrange
        using var context = new LogTestContext(LogEventLevel.Debug, enrichFromLogContext: true);
        using var loggerFactory = new TestLoggerFactory(context);
        var enricher = new TestDomainEventCtxEnricher();
        var serviceProvider = new EnricherServiceProvider(enricher);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions, serviceProvider);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Ulid.Parse("01H00000000000000000000003"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        var handler = new TestLoggingDomainEventHandler
        {
            ThrowException = new InvalidOperationException("데이터베이스 연결 실패")
        };
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        try
        {
            await sut.Publish(handlers, domainEvent, CancellationToken.None);
        }
        catch (AggregateException)
        {
            // 에러는 무시 - 로그 필드 검증이 목적
        }

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventHandlerEnricherLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    #region Test Helpers

    /// <summary>
    /// TestDomainEvent용 IDomainEventCtxEnricher 구현.
    /// ctx.customer_id와 ctx.test_domain_event.message 필드를 Push합니다.
    /// </summary>
    internal sealed class TestDomainEventCtxEnricher : IDomainEventCtxEnricher<TestDomainEvent>
    {
        public IDisposable? Enrich(TestDomainEvent domainEvent)
        {
            var disposables = new List<IDisposable>(2);
            disposables.Add(LogContext.PushProperty("ctx.customer_id", "CUST-001"));
            disposables.Add(LogContext.PushProperty("ctx.test_domain_event.message", domainEvent.Message));
            return new CompositeDisposable(disposables);
        }

        private sealed class CompositeDisposable(List<IDisposable> disposables) : IDisposable
        {
            public void Dispose()
            {
                for (int i = disposables.Count - 1; i >= 0; i--)
                    disposables[i].Dispose();
            }
        }
    }

    /// <summary>
    /// TestDomainEventCtxEnricher를 반환하는 테스트용 ServiceProvider.
    /// </summary>
    private sealed class EnricherServiceProvider(TestDomainEventCtxEnricher enricher) : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IDomainEventCtxEnricher<TestDomainEvent>))
                return enricher;
            return null;
        }
    }

    #endregion
}
