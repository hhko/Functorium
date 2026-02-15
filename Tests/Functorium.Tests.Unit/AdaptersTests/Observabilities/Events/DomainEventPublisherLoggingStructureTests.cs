using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Applications.Events;
using Functorium.Testing.Arrangements.Logging;
using Functorium.Tests.Unit.DomainsTests.Entities;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Options;
using NSubstitute;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

/// <summary>
/// ObservableDomainEventPublisher 로그 필드 검증 테스트.
/// README.md Observability 섹션에 정의된 필드가 정확히 출력되는지 검증합니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 로깅 필드 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// 로그 필드 구조 비교표 (Single Event):
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+-------------------+
/// | Field Key                | Request           | Response Success  | Response Failure  |
/// +--------------------------+-------------------+-------------------+-------------------+
/// | request.layer            | "adapter"         | "adapter"         | "adapter"         |
/// | request.category         | "event"           | "event"           | "event"           |
/// | request.handler          | event type name   | event type name   | event type name   |
/// | request.handler.method   | "Publish"         | "Publish"         | "Publish"         |
/// | @request.message         | event object      | (none)            | (none)            |
/// | response.status          | (none)            | "success"         | "failure"         |
/// | response.elapsed         | (none)            | elapsed (s)       | elapsed (s)       |
/// | error.type               | (none)            | (none)            | "expected"/       |
/// |                          |                   |                   | "exceptional"     |
/// | error.code               | (none)            | (none)            | error code        |
/// | @error                   | (none)            | (none)            | error object      |
/// +--------------------------+-------------------+-------------------+-------------------+
/// </code>
/// <para>
/// 로그 필드 구조 비교표 (Batch Events):
/// </para>
/// <code>
/// +-------------------------------+-------------------+-------------------+-------------------+
/// | Field Key                     | Request           | Response Success  | Response Failure  |
/// +-------------------------------+-------------------+-------------------+-------------------+
/// | request.layer                 | "adapter"         | "adapter"         | "adapter"         |
/// | request.category              | "event"           | "event"           | "event"           |
/// | request.handler               | aggregate type    | aggregate type    | aggregate type    |
/// | request.handler.method        | method name       | method name       | method name       |
/// | request.event.count           | count             | count             | count             |
/// | response.status               | (none)            | "success"         | "failure"         |
/// | response.elapsed              | (none)            | elapsed (s)       | elapsed (s)       |
/// | response.event.success_count  | (none)            | (partial only)    | (none)            |
/// | response.event.failure_count  | (none)            | (partial only)    | (none)            |
/// | error.type                    | (none)            | (none)            | "expected"/       |
/// |                               |                   |                   | "exceptional"     |
/// | error.code                    | (none)            | (none)            | error code        |
/// | @error                        | (none)            | (none)            | error object      |
/// +-------------------------------+-------------------+-------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class DomainEventPublisherLoggingStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IDomainEventPublisher _mockInner;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;

    public DomainEventPublisherLoggingStructureTests()
    {
        _activitySource = new ActivitySource("Test.DomainEventPublisherLogging");
        _mockInner = Substitute.For<IDomainEventPublisher>();
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestLogging" });
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

    // ===== Single Event - Request 로그 필드 검증 =====

    [Fact]
    public async Task SingleEvent_Request_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Ulid.Parse("01H00000000000000000000001"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots/DomainEventPublisherLoggingStructure");
    }

    // ===== Single Event - Success Response 로그 필드 검증 =====

    [Fact]
    public async Task SingleEvent_SuccessResponse_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Ulid.Parse("01H00000000000000000000002"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventPublisherLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Single Event - Warning Response 로그 필드 검증 (Expected Error) =====

    [Fact]
    public async Task SingleEvent_WarningResponse_WithExpectedError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Ulid.Parse("01H00000000000000000000003"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        // Expected Error: 예상된 비즈니스 에러 (IsExceptional = false)
        var error = Error.New("Event.Handler.NotFound");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventPublisherLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Single Event - Error Response 로그 필드 검증 (Exceptional Error) =====

    [Fact]
    public async Task SingleEvent_ErrorResponse_WithExceptionalError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Ulid.Parse("01H00000000000000000000004"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        // Exceptional Error: 예외적 에러 (IsExceptional = true)
        var exception = new InvalidOperationException("메시지 큐 연결 실패");
        var error = Error.New(exception);
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventPublisherLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Batch Events (PublishEvents) - Request 로그 필드 검증 =====

    [Fact]
    public async Task BatchEvents_Request_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "TestAggregate");
        aggregate.AddEvent(new TestDomainEvent("Event1"));
        aggregate.AddEvent(new TestDomainEvent("Event2"));

        _mockInner
            .PublishEvents(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots/DomainEventPublisherLoggingStructure");
    }

    // ===== Batch Events (PublishEvents) - Success Response 로그 필드 검증 =====

    [Fact]
    public async Task BatchEvents_SuccessResponse_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "TestAggregate");
        aggregate.AddEvent(new TestDomainEvent("Event1"));
        aggregate.AddEvent(new TestDomainEvent("Event2"));
        aggregate.AddEvent(new TestDomainEvent("Event3"));

        _mockInner
            .PublishEvents(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventPublisherLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Batch Events (PublishEvents) - Warning Response 로그 필드 검증 (Expected Error) =====

    [Fact]
    public async Task BatchEvents_WarningResponse_WithExpectedError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "TestAggregate");
        aggregate.AddEvent(new TestDomainEvent("Event1"));
        aggregate.AddEvent(new TestDomainEvent("Event2"));

        // Expected Error: 예상된 비즈니스 에러 (IsExceptional = false)
        var error = Error.New("Event.Batch.Validation.Failed");
        _mockInner
            .PublishEvents(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventPublisherLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Batch Events (PublishEvents) - Error Response 로그 필드 검증 (Exceptional Error) =====

    [Fact]
    public async Task BatchEvents_ErrorResponse_WithExceptionalError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "TestAggregate");
        aggregate.AddEvent(new TestDomainEvent("Event1"));

        // Exceptional Error: 예외적 에러 (IsExceptional = true)
        var exception = new InvalidOperationException("메시지 버스 연결 실패");
        var error = Error.New(exception);
        _mockInner
            .PublishEvents(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventPublisherLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Batch Events (PublishEventsWithResult) - Partial Failure Response 로그 필드 검증 =====

    [Fact]
    public async Task BatchEvents_PartialFailureResponse_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<ObservableDomainEventPublisher>();
        var sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, logger, _meterFactory, _openTelemetryOptions);

        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "TestAggregate");
        var event1 = new TestDomainEvent("Event1");
        var event2 = new TestDomainEvent("Event2");
        var event3 = new TestDomainEvent("Event3");
        aggregate.AddEvent(event1);
        aggregate.AddEvent(event2);
        aggregate.AddEvent(event3);

        // 3개 중 1개 실패 - 성공한 이벤트 2개, 실패한 이벤트 1개
        var successfulEvents = LanguageExt.Seq<Functorium.Domains.Events.IDomainEvent>.Empty.Add(event1).Add(event2);
        var failedEvents = LanguageExt.Seq<(Functorium.Domains.Events.IDomainEvent, Error)>.Empty.Add((event3, Error.New("Event3.Failed")));
        var partialResult = new PublishResult(successfulEvents, failedEvents);

        _mockInner
            .PublishEventsWithResult(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, PublishResult>(partialResult));

        // Act
        await sut.PublishEventsWithResult(aggregate).Run().RunAsync();

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/DomainEventPublisherLoggingStructure")
            .ScrubMember("response.elapsed");
    }
}
