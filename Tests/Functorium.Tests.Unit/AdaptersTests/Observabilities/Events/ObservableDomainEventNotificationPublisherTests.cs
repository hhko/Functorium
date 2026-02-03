using Functorium.Adapters.Observabilities.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
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
        var mockHandler = Substitute.For<INotificationHandler<TestNonDomainEventNotification>>();
        mockHandler.Handle(notification, Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        var handlers = new NotificationHandlers<TestNonDomainEventNotification>(
            [mockHandler],
            isArray: true);

        // Act
        await _sut.Publish(handlers, notification, CancellationToken.None);

        // Assert
        await mockHandler.Received(1).Handle(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_CallsAllHandlers_WhenMultipleHandlersExist()
    {
        // Arrange
        var notification = new TestNonDomainEventNotification("Test");
        var mockHandler1 = Substitute.For<INotificationHandler<TestNonDomainEventNotification>>();
        var mockHandler2 = Substitute.For<INotificationHandler<TestNonDomainEventNotification>>();
        mockHandler1.Handle(notification, Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        mockHandler2.Handle(notification, Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        var handlers = new NotificationHandlers<TestNonDomainEventNotification>(
            [mockHandler1, mockHandler2],
            isArray: true);

        // Act
        await _sut.Publish(handlers, notification, CancellationToken.None);

        // Assert
        await mockHandler1.Received(1).Handle(notification, Arg.Any<CancellationToken>());
        await mockHandler2.Received(1).Handle(notification, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_ThrowsAggregateException_WhenHandlerThrows()
    {
        // Arrange
        var notification = new TestNonDomainEventNotification("Test");
        var mockHandler = Substitute.For<INotificationHandler<TestNonDomainEventNotification>>();
        mockHandler.Handle(notification, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException(new InvalidOperationException("Test error")));

        var handlers = new NotificationHandlers<TestNonDomainEventNotification>(
            [mockHandler],
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
        var mockHandler = Substitute.For<INotificationHandler<TestDomainEvent>>();
        mockHandler.Handle(domainEvent, Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);

        var handlers = new NotificationHandlers<TestDomainEvent>(
            [mockHandler],
            isArray: true);

        // Act
        await _sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        await mockHandler.Received(1).Handle(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_ThrowsAggregateException_WhenDomainEventHandlerThrows()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var mockHandler = Substitute.For<INotificationHandler<TestDomainEvent>>();
        mockHandler.Handle(domainEvent, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromException(new InvalidOperationException("Test error")));

        var handlers = new NotificationHandlers<TestDomainEvent>(
            [mockHandler],
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

/// <summary>
/// IDomainEvent가 아닌 일반 Notification (테스트용)
/// </summary>
public sealed record TestNonDomainEventNotification(string Message) : INotification;
