using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Testing.Arrangements.Logging;
using Functorium.Tests.Unit.DomainsTests.Entities;

using Mediator;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MsOptions = Microsoft.Extensions.Options.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

/// <summary>
/// ObservableDomainEventNotificationPublisher의 Handler Tracing 태그 구조를 검증하는 테스트입니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 Tracing 태그 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// Activity 태그 구조 비교표:
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+
/// | Tag Key                  | Success           | Failure           |
/// +--------------------------+-------------------+-------------------+
/// | request.layer            | "application"     | "application"     |
/// | request.category         | "usecase"         | "usecase"         |
/// | request.category.type    | "event"           | "event"           |
/// | request.handler          | handler name      | handler name      |
/// | request.handler.method   | "Handle"          | "Handle"          |
/// | event.type               | event type name   | event type name   |
/// | event.id                 | event id          | event id          |
/// | response.status          | "success"         | "failure"         |
/// | error.type               | (none)            | "expected"/       |
/// |                          |                   | "exceptional"     |
/// | error.code               | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+
/// | Total Tags               | 8                 | 10                |
/// +--------------------------+-------------------+-------------------+
/// </code>
/// <para>
/// Note: Handler의 response.elapsed는 Activity 태그에 설정되지 않습니다 (Logging 전용).
/// </para>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class DomainEventHandlerTracingStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private Activity? _capturedActivity;

    public DomainEventHandlerTracingStructureTests()
    {
        _activitySource = new ActivitySource("Test.DomainEventHandlerTracing");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _activitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _capturedActivity = activity
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestHandlerTracing" });
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
        (_meterFactory as IDisposable)?.Dispose();
    }

    #region Span 이름 검증

    /// <summary>
    /// Handler 호출 시 Span 이름이 올바른 패턴을 따라야 합니다.
    /// 패턴: "application usecase.event {HandlerName}.Handle"
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Handle_ShouldCreateActivityWithCorrectName()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestTracingDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.DisplayName.ShouldStartWith("application usecase.event");
        _capturedActivity.DisplayName.ShouldContain(".Handle");
    }

    #endregion

    #region Tag 개수 검증

    /// <summary>
    /// 성공 시 8개 태그를 가져야 합니다.
    /// (request 5개 + event.type + event.id + response.status)
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Handle_Success_ShouldHaveEightTags()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestTracingDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.TagObjects.Count().ShouldBe(8);
    }

    /// <summary>
    /// 실패 시 10개 태그를 가져야 합니다.
    /// (request 5개 + event.type + event.id + response.status + error.type + error.code)
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Handle_Failure_ShouldHaveTenTags()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestTracingDomainEventHandler
        {
            ThrowException = new InvalidOperationException("Database connection failed")
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
            // 에러는 무시 - 태그 검증이 목적
        }

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.TagObjects.Count().ShouldBe(10);
    }

    #endregion

    #region ActivityStatus 검증

    /// <summary>
    /// 성공 시 ActivityStatus가 Ok여야 합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Handle_Success_ShouldSetActivityStatusOk()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestTracingDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// 실패 시 ActivityStatus가 Error여야 합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Handle_Failure_ShouldSetActivityStatusError()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestTracingDomainEventHandler
        {
            ThrowException = new InvalidOperationException("Database connection failed")
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
            // 에러는 무시 - 태그 검증이 목적
        }

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    #endregion

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Handler Success 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Snapshot_Handler_SuccessTags()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestTracingDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots/DomainEventHandlerTracingStructure")
            .ScrubMember(ObservabilityNaming.CustomAttributes.EventId);
    }

    /// <summary>
    /// Handler Failure (Expected Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Snapshot_Handler_FailureResponse_ExpectedError_Tags()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestTracingDomainEventHandler
        {
            ThrowException = new OperationCanceledException("Operation was cancelled")
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
            // 에러는 무시 - 태그 검증이 목적
        }

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots/DomainEventHandlerTracingStructure")
            .ScrubMember(ObservabilityNaming.CustomAttributes.EventId);
    }

    /// <summary>
    /// Handler Failure (Exceptional Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Snapshot_Handler_FailureResponse_ExceptionalError_Tags()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(
            _activitySource, loggerFactory, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("Test");
        var handler = new TestTracingDomainEventHandler
        {
            ThrowException = new InvalidOperationException("Database connection failed")
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
            // 에러는 무시 - 태그 검증이 목적
        }

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots/DomainEventHandlerTracingStructure")
            .ScrubMember(ObservabilityNaming.CustomAttributes.EventId);
    }

    #endregion

    #region Helper Methods

    private Dictionary<string, string?> ExtractActivityTags()
    {
        _capturedActivity.ShouldNotBeNull();
        return _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());
    }

    #endregion

    #region Test Types

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter.Dispose();
            }
            _meters.Clear();
        }
    }

    /// <summary>
    /// Tracing 테스트용 DomainEvent 핸들러.
    /// </summary>
    private sealed class TestTracingDomainEventHandler : INotificationHandler<TestDomainEvent>
    {
        public Exception? ThrowException { get; set; }

        public ValueTask Handle(TestDomainEvent notification, CancellationToken cancellationToken)
        {
            if (ThrowException is not null)
            {
                throw ThrowException;
            }

            return ValueTask.CompletedTask;
        }
    }

    #endregion
}
