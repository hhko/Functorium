using Functorium.Adapters.Observabilities.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class ObservableDomainEventNotificationPublisherTests
{
    private readonly ObservableDomainEventNotificationPublisher _sut;

    public ObservableDomainEventNotificationPublisherTests()
    {
        _sut = new ObservableDomainEventNotificationPublisher(NullLoggerFactory.Instance);
    }

    #region Non-IDomainEvent Handling Tests

    [Fact]
    public async Task Publish_CallsHandlers_WhenNotificationIsNotDomainEvent()
    {
        // Arrange
        var notification = new TestNonDomainEventNotification("Test");
        var trackingHandler = new TrackingNotificationHandler();

        var handlers = new NotificationHandlers<TestNonDomainEventNotification>(
            [trackingHandler],
            isArray: true);

        // Act
        await _sut.Publish(handlers, notification, CancellationToken.None);

        // Assert
        Assert.Equal(1, trackingHandler.HandleCount);
    }

    [Fact]
    public async Task Publish_CallsAllHandlers_WhenMultipleHandlersExist()
    {
        // Arrange
        var notification = new TestNonDomainEventNotification("Test");
        var trackingHandler1 = new TrackingNotificationHandler();
        var trackingHandler2 = new TrackingNotificationHandler();

        var handlers = new NotificationHandlers<TestNonDomainEventNotification>(
            [trackingHandler1, trackingHandler2],
            isArray: true);

        // Act
        await _sut.Publish(handlers, notification, CancellationToken.None);

        // Assert
        Assert.Equal(1, trackingHandler1.HandleCount);
        Assert.Equal(1, trackingHandler2.HandleCount);
    }

    [Fact]
    public async Task Publish_ThrowsAggregateException_WhenHandlerThrows()
    {
        // Arrange
        var notification = new TestNonDomainEventNotification("Test");
        var throwingHandler = new ThrowingNotificationHandler();

        var handlers = new NotificationHandlers<TestNonDomainEventNotification>(
            [throwingHandler],
            isArray: true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => _sut.Publish(handlers, notification, CancellationToken.None).AsTask());
        Assert.Single(exception.InnerExceptions);
        Assert.IsType<InvalidOperationException>(exception.InnerExceptions[0]);
    }

    #endregion

    #region IDomainEvent Handling Tests

    [Fact]
    public async Task Publish_CallsHandlers_WhenNotificationIsDomainEvent()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var trackingHandler = new TrackingDomainEventHandler();

        var handlers = new NotificationHandlers<TestDomainEvent>(
            [trackingHandler],
            isArray: true);

        // Act
        await _sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        Assert.Equal(1, trackingHandler.HandleCount);
    }

    [Fact]
    public async Task Publish_ThrowsAggregateException_WhenDomainEventHandlerThrows()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var throwingHandler = new ThrowingDomainEventHandler();

        var handlers = new NotificationHandlers<TestDomainEvent>(
            [throwingHandler],
            isArray: true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<AggregateException>(
            () => _sut.Publish(handlers, domainEvent, CancellationToken.None).AsTask());
        Assert.Single(exception.InnerExceptions);
        Assert.IsType<InvalidOperationException>(exception.InnerExceptions[0]);
    }

    [Fact]
    public async Task Publish_CompletesSuccessfully_WhenNoHandlersExist()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var handlers = new NotificationHandlers<TestDomainEvent>(
            System.Array.Empty<INotificationHandler<TestDomainEvent>>(),
            isArray: true);

        // Act & Assert (should not throw)
        await _sut.Publish(handlers, domainEvent, CancellationToken.None);
    }

    #endregion
}

#region Test Helpers

/// <summary>
/// IDomainEvent가 아닌 일반 Notification (테스트용)
/// </summary>
public sealed record TestNonDomainEventNotification(string Message) : INotification;

/// <summary>
/// 호출 횟수를 추적하는 Notification 핸들러 (테스트용)
/// </summary>
/// <remarks>
/// NSubstitute Mock 대신 실제 구현체 사용.
/// .NET 10 preview에서 NSubstitute 프록시 객체의 GetType() 호출 시
/// AccessViolationException이 발생하는 문제 회피.
/// </remarks>
public sealed class TrackingNotificationHandler : INotificationHandler<TestNonDomainEventNotification>
{
    public int HandleCount { get; private set; }

    public ValueTask Handle(TestNonDomainEventNotification notification, CancellationToken cancellationToken)
    {
        HandleCount++;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// 예외를 던지는 Notification 핸들러 (테스트용)
/// </summary>
public sealed class ThrowingNotificationHandler : INotificationHandler<TestNonDomainEventNotification>
{
    public ValueTask Handle(TestNonDomainEventNotification notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Test error");
    }
}

/// <summary>
/// 호출 횟수를 추적하는 DomainEvent 핸들러 (테스트용)
/// </summary>
public sealed class TrackingDomainEventHandler : INotificationHandler<TestDomainEvent>
{
    public int HandleCount { get; private set; }

    public ValueTask Handle(TestDomainEvent notification, CancellationToken cancellationToken)
    {
        HandleCount++;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// 예외를 던지는 DomainEvent 핸들러 (테스트용)
/// </summary>
public sealed class ThrowingDomainEventHandler : INotificationHandler<TestDomainEvent>
{
    public ValueTask Handle(TestDomainEvent notification, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Test error");
    }
}

#endregion
