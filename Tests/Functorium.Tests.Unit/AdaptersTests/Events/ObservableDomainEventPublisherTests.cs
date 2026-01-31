using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Events;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class ObservableDomainEventPublisherTests
{
    private readonly IDomainEventPublisher _mockInner;
    private readonly ILogger<ObservableDomainEventPublisher> _mockLogger;
    private readonly ObservableDomainEventPublisher _sut;

    public ObservableDomainEventPublisherTests()
    {
        _mockInner = Substitute.For<IDomainEventPublisher>();
        _mockLogger = Substitute.For<ILogger<ObservableDomainEventPublisher>>();
        _sut = new ObservableDomainEventPublisher(_mockInner, _mockLogger);
    }

    #region Publish Tests

    [Fact]
    public async Task Publish_DelegatesToInner_WhenCalled()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        _mockInner.Received(1).Publish(domainEvent, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Publish_LogsPublishing_WhenCalled()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        _mockLogger.ReceivedCalls().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task Publish_ReturnsSuccess_WhenInnerSucceeds()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Publish_ReturnsFail_WhenInnerFails()
    {
        // Arrange
        var domainEvent = new TestDomainEvent("Test");
        var error = LanguageExt.Common.Error.New("Test error");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        var actual = await _sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region PublishEvents Tests

    [Fact]
    public async Task PublishEvents_DelegatesToInner_WhenCalled()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        aggregate.AddEvent(new TestDomainEvent("Test"));

        _mockInner
            .PublishEvents(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        var actual = await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        _mockInner.Received(1).PublishEvents(aggregate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishEvents_LogsPublishing_WhenCalled()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        aggregate.AddEvent(new TestDomainEvent("Test"));

        _mockInner
            .PublishEvents(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        _mockLogger.ReceivedCalls().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task PublishEvents_ReturnsSuccess_WhenInnerSucceeds()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        aggregate.AddEvent(new TestDomainEvent("Test"));

        _mockInner
            .PublishEvents(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        var actual = await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task PublishEvents_ReturnsFail_WhenInnerFails()
    {
        // Arrange
        var aggregate = new TestAggregateRootWithoutEvents(TestEntityId.New(), "Test");
        aggregate.AddEvent(new TestDomainEvent("Test"));

        var error = LanguageExt.Common.Error.New("Test error");
        _mockInner
            .PublishEvents(Arg.Any<TestAggregateRootWithoutEvents>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        var actual = await _sut.PublishEvents(aggregate).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion
}
