using System.Diagnostics;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Testing.Arrangements.Logging;
using Functorium.Tests.Unit.DomainsTests.Entities;
using Mediator;
using Microsoft.Extensions.Logging;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

/// <summary>
/// ObservableDomainEventNotificationPublisher의 Handler 로그 필드 검증 테스트.
/// README.md Observability 섹션에 정의된 필드가 정확히 출력되는지 검증합니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 로깅 필드 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// 로그 필드 구조 비교표:
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+-------------------+
/// | Field Key                | Request           | Response Success  | Response Failure  |
/// +--------------------------+-------------------+-------------------+-------------------+
/// | request.layer            | "application"     | "application"     | "application"     |
/// | request.category         | "usecase"         | "usecase"         | "usecase"         |
/// | request.category.type    | "event"           | "event"           | "event"           |
/// | request.handler          | handler type name | handler type name | handler type name |
/// | request.handler.method   | "Handle"          | "Handle"          | "Handle"          |
/// | @request.message         | event object      | (none)            | (none)            |
/// | response.status          | (none)            | "success"         | "failure"         |
/// | response.elapsed         | (none)            | elapsed (s)       | elapsed (s)       |
/// | error.type               | (none)            | (none)            | "expected"/       |
/// |                          |                   |                   | "exceptional"     |
/// | error.code               | (none)            | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+-------------------+
/// </code>
/// <para>
/// Note: DomainEventHandler의 ErrorResponse는 Exception 객체가 직접 로깅됩니다 (@error 대신).
/// </para>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class DomainEventHandlerLoggingStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;

    public DomainEventHandlerLoggingStructureTests()
    {
        _activitySource = new ActivitySource("Test.DomainEventHandlerLogging");
    }

    public void Dispose()
    {
        _activitySource.Dispose();
    }

    // ===== Request 로그 필드 검증 =====

    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task Request_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(_activitySource, loggerFactory);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        var handler = new TestLoggingDomainEventHandler();
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act
        await sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots/DomainEventHandlerLoggingStructure");
    }

    // ===== Success Response 로그 필드 검증 =====

    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task SuccessResponse_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(_activitySource, loggerFactory);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
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
            .UseDirectory("Snapshots/DomainEventHandlerLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Warning Response 로그 필드 검증 (Expected Error) =====
    // Note: DomainEventHandler는 Expected Error를 직접 지원하지 않고,
    // Exception을 통해서만 에러를 전달합니다.
    // 따라서 이 케이스는 ApplicationException (비즈니스 에러 시뮬레이션)으로 테스트합니다.

    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task WarningResponse_WithExpectedError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(_activitySource, loggerFactory);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        // ApplicationException으로 비즈니스 에러 시뮬레이션
        var handler = new TestLoggingDomainEventHandler
        {
            ThrowException = new ApplicationException("Event.Handler.NotFound: 핸들러를 찾을 수 없습니다")
        };
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act - 에러가 발생해도 로그는 기록됨
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
            .UseDirectory("Snapshots/DomainEventHandlerLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Error Response 로그 필드 검증 (Exceptional Error) =====

    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task ErrorResponse_WithExceptionalError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(_activitySource, loggerFactory);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        var handler = new TestLoggingDomainEventHandler
        {
            ThrowException = new InvalidOperationException("데이터베이스 연결 실패")
        };
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act - 에러가 발생해도 로그는 기록됨
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
            .UseDirectory("Snapshots/DomainEventHandlerLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Error Response 로그 필드 검증 (Nested Exception) =====

    [Fact(Skip = ".NET 10 preview: AccessViolationException occurs when calling GetType() on proxy objects")]
    public async Task ErrorResponse_WithException_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        using var loggerFactory = new TestLoggerFactory(context);
        var sut = new ObservableDomainEventNotificationPublisher(_activitySource, loggerFactory);

        var domainEvent = new TestDomainEvent("TestMessage") with
        {
            EventId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            OccurredAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero)
        };

        var innerException = new TimeoutException("연결 시간 초과");
        var handler = new TestLoggingDomainEventHandler
        {
            ThrowException = new InvalidOperationException("작업 실패", innerException)
        };
        var handlers = new NotificationHandlers<TestDomainEvent>(
            [handler],
            isArray: true);

        // Act - 에러가 발생해도 로그는 기록됨
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
            .UseDirectory("Snapshots/DomainEventHandlerLoggingStructure")
            .ScrubMember("response.elapsed");
    }
}

#region Test Helpers

/// <summary>
/// LogTestContext를 사용하여 ILoggerFactory를 구현하는 테스트용 팩토리.
/// </summary>
internal sealed class TestLoggerFactory : ILoggerFactory
{
    private readonly LogTestContext _context;

    public TestLoggerFactory(LogTestContext context)
    {
        _context = context;
    }

    public ILogger CreateLogger(string categoryName)
    {
        // 동적 타입을 사용하여 CreateLogger<T> 호출
        // LogTestContext는 제네릭 CreateLogger<T>만 지원하므로
        // object 타입으로 생성
        return _context.CreateLogger<object>();
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // 테스트 용도이므로 구현하지 않음
    }

    public void Dispose()
    {
        // LogTestContext가 별도로 Dispose됨
    }
}

/// <summary>
/// 로그 필드 테스트용 DomainEvent 핸들러.
/// 설정된 Exception이 있으면 해당 Exception을 던집니다.
/// </summary>
internal sealed class TestLoggingDomainEventHandler : INotificationHandler<TestDomainEvent>
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
