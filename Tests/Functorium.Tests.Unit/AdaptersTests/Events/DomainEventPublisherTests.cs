using Functorium.Abstractions.Errors;
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

    #region Error Handling Tests

    [Fact]
    public async Task Publish_ReturnsFail_WhenPublisherThrowsException()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var expectedException = new InvalidOperationException("Handler failed");
        _mockPublisher
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw expectedException);

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: error =>
            {
                error.IsExceptional.ShouldBeTrue();
                error.ShouldBeAssignableTo<IHasErrorCode>();
                ((IHasErrorCode)error).ErrorCode.ShouldContain("PublishFailed");
            });
    }

    [Fact]
    public async Task Publish_ReturnsFail_WhenOperationCancelled()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        _mockPublisher
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new OperationCanceledException());

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: error =>
            {
                error.IsExpected.ShouldBeTrue();
                error.ShouldBeAssignableTo<IHasErrorCode>();
                ((IHasErrorCode)error).ErrorCode.ShouldContain("PublishCancelled");
            });
    }

    [Fact]
    public async Task PublishEvents_ReturnsFail_WhenFirstEventThrowsException()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Event1");
        var event2 = new TestDomainEvent("Event2");
        aggregate.AddEvent(event1);
        aggregate.AddEvent(event2);

        _mockPublisher
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new InvalidOperationException("Handler failed"));

        // Act
        var actual = await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: error =>
            {
                error.IsExceptional.ShouldBeTrue();
                error.ShouldBeAssignableTo<IHasErrorCode>();
                ((IHasErrorCode)error).ErrorCode.ShouldContain("PublishFailed");
            });
    }

    [Fact]
    public async Task PublishEvents_ReturnsFail_WhenOperationCancelled()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        aggregate.AddEvent(new TestDomainEvent("Test"));

        _mockPublisher
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new OperationCanceledException());

        // Act
        var actual = await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Should not succeed"),
            Fail: error =>
            {
                error.IsExpected.ShouldBeTrue();
                error.ShouldBeAssignableTo<IHasErrorCode>();
                ((IHasErrorCode)error).ErrorCode.ShouldContain("PublishCancelled");
            });
    }

    [Fact]
    public async Task PublishEvents_ClearsEventsBeforePublishing_EvenWhenExceptionOccurs()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        aggregate.AddEvent(new TestDomainEvent("Test"));
        aggregate.DomainEvents.Count.ShouldBe(1);

        _mockPublisher
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new InvalidOperationException("Handler failed"));

        // Act
        await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region PublishEventsWithResult Tests

    [Fact]
    public async Task PublishEventsWithResult_ReturnsSuccess_WhenAllEventsPublished()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Event1");
        var event2 = new TestDomainEvent("Event2");
        aggregate.AddEvent(event1);
        aggregate.AddEvent(event2);

        // Act
        var actual = await _sut.PublishEventsWithResult(aggregate).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: result =>
            {
                result.IsAllSuccessful.ShouldBeTrue();
                result.SuccessCount.ShouldBe(2);
                result.FailureCount.ShouldBe(0);
            },
            Fail: _ => throw new Exception("Should not fail"));
    }

    [Fact]
    public async Task PublishEventsWithResult_ReturnsPartialFailure_WhenSomeEventsFail()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Event1");
        var event2 = new AnotherTestDomainEvent(42);
        aggregate.AddEvent(event1);
        aggregate.AddEvent(event2);

        // event1 succeeds, event2 fails
        _mockPublisher
            .Publish(Arg.Any<AnotherTestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new InvalidOperationException("Handler failed"));

        // Act
        var actual = await _sut.PublishEventsWithResult(aggregate).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: result =>
            {
                result.IsAllSuccessful.ShouldBeFalse();
                result.HasFailures.ShouldBeTrue();
                result.SuccessCount.ShouldBe(1);
                result.FailureCount.ShouldBe(1);
            },
            Fail: _ => throw new Exception("Should not fail"));
    }

    [Fact]
    public async Task PublishEventsWithResult_ContinuesAfterFailure_AndPublishesRemainingEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        var event1 = new TestDomainEvent("Event1");
        var event2 = new AnotherTestDomainEvent(42);
        var event3 = new TestDomainEvent("Event3");
        aggregate.AddEvent(event1);
        aggregate.AddEvent(event2);
        aggregate.AddEvent(event3);

        // event2 fails, but event1 and event3 should succeed
        _mockPublisher
            .Publish(Arg.Any<AnotherTestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(x => throw new InvalidOperationException("Handler failed"));

        // Act
        var actual = await _sut.PublishEventsWithResult(aggregate).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: result =>
            {
                result.SuccessCount.ShouldBe(2);
                result.FailureCount.ShouldBe(1);
                result.TotalCount.ShouldBe(3);
            },
            Fail: _ => throw new Exception("Should not fail"));

        // Verify all events were attempted (2 TestDomainEvent + 1 AnotherTestDomainEvent)
        await _mockPublisher.Received(2).Publish(
            Arg.Any<TestDomainEvent>(),
            Arg.Any<CancellationToken>());
        await _mockPublisher.Received(1).Publish(
            Arg.Any<AnotherTestDomainEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishEventsWithResult_ReturnsEmptyResult_WhenNoEvents()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");

        // Act
        var actual = await _sut.PublishEventsWithResult(aggregate).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: result =>
            {
                result.IsAllSuccessful.ShouldBeTrue();
                result.TotalCount.ShouldBe(0);
            },
            Fail: _ => throw new Exception("Should not fail"));
    }

    [Fact]
    public async Task PublishEventsWithResult_ClearsEvents_WhenPublishingCompletes()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        aggregate.AddEvent(new TestDomainEvent("Test"));
        aggregate.DomainEvents.Count.ShouldBe(1);

        // Act
        await _sut.PublishEventsWithResult(aggregate).Run().RunAsync();

        // Assert
        aggregate.DomainEvents.ShouldBeEmpty();
    }

    #endregion
}
