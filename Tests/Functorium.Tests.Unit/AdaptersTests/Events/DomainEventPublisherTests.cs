using Functorium.Adapters.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;
using Mediator;
using NSubstitute;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Events;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class DomainEventPublisherTests
{
    private readonly IPublisher _mockPublisher;
    private readonly DomainEventPublisher _sut;

    public DomainEventPublisherTests()
    {
        _mockPublisher = Substitute.For<IPublisher>();
        _sut = new DomainEventPublisher(_mockPublisher);
    }

    #region PublishEvents Tests

    [Fact]
    public async Task PublishEvents_ReturnsSuccess_WhenAggregateHasEvents()
    {
        // Arrange
        var aggregate = TestAggregateRoot.Create(TestEntityId.New(), "Test");

        // Act
        var actual = await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task PublishEvents_ReturnsSuccess_WhenAggregateHasNoEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");

        // Act
        var actual = await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task PublishEvents_PublishesAllEvents_WhenAggregateHasMultipleEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Created");
        var event2 = new TestDomainEvent("Updated");
        aggregate.AddEvent(event1);
        aggregate.AddEvent(event2);

        // Act
        await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        await _mockPublisher.Received(2).Publish(
            Arg.Any<TestDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishEvents_ClearsEvents_WhenPublishingCompletes()
    {
        // Arrange
        var aggregate = TestAggregateRoot.Create(TestEntityId.New(), "Test");
        aggregate.DomainEvents.Count.ShouldBe(1);

        // Act
        await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public async Task PublishEvents_CallsPublisher_ForEachEvent()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Event1");
        var event2 = new AnotherTestDomainEvent(42);
        aggregate.AddEvent(event1);
        aggregate.AddEvent(event2);

        // Act
        await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        await _mockPublisher.Received(1).Publish(event1, Arg.Any<CancellationToken>());
        await _mockPublisher.Received(1).Publish(event2, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Publish Tests

    [Fact]
    public async Task Publish_ReturnsSuccess_WhenEventIsValid()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Publish_CallsPublisher_WithCorrectEvent()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");

        // Act
        await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        await _mockPublisher.Received(1).Publish(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_PassesCancellationToken_ToMediator()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        using var cts = new CancellationTokenSource();
        var expectedToken = cts.Token;

        // Act
        await _sut.Publish(domainEvent, expectedToken).Run().RunAsync();

        // Assert
        await _mockPublisher.Received(1).Publish(domainEvent, expectedToken);
    }

    #endregion
}
