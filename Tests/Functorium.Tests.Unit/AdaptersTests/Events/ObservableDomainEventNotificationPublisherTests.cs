using Functorium.Adapters.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Events;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class ObservableDomainEventNotificationPublisherTests
{
    private readonly INotificationPublisher _mockInner;
    private readonly ObservableDomainEventNotificationPublisher _sut;

    public ObservableDomainEventNotificationPublisherTests()
    {
        _mockInner = Substitute.For<INotificationPublisher>();
        _sut = new ObservableDomainEventNotificationPublisher(_mockInner, NullLoggerFactory.Instance);
    }

    #region Non-IDomainEvent Handling Tests

    [Fact]
    public async Task Publish_DelegatesToInner_WhenNotificationIsNotDomainEvent()
    {
        // Arrange
        var notification = new TestNonDomainEventNotification("Test");
        var handlers = new NotificationHandlers<TestNonDomainEventNotification>(
            System.Array.Empty<INotificationHandler<TestNonDomainEventNotification>>(),
            isArray: true);

        // Act
        await _sut.Publish(handlers, notification, CancellationToken.None);

        // Assert
        await _mockInner.Received(1).Publish(
            Arg.Any<NotificationHandlers<TestNonDomainEventNotification>>(),
            notification,
            CancellationToken.None);
    }

    [Fact]
    public async Task Publish_DoesNotDelegateToInner_WhenNotificationIsDomainEvent()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var handlers = new NotificationHandlers<TestDomainEvent>(
            System.Array.Empty<INotificationHandler<TestDomainEvent>>(),
            isArray: true);

        // Act
        await _sut.Publish(handlers, domainEvent, CancellationToken.None);

        // Assert
        await _mockInner.DidNotReceive().Publish(
            Arg.Any<NotificationHandlers<TestDomainEvent>>(),
            Arg.Any<TestDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region Test Fixtures

    /// <summary>
    /// IDomainEvent가 아닌 일반 Notification
    /// </summary>
    private sealed record TestNonDomainEventNotification(string Message) : INotification;

    #endregion
}
