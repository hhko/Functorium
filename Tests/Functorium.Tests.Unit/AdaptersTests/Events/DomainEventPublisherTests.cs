using Functorium.Abstractions.Errors;
using Functorium.Adapters.Events;
using Functorium.Applications.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;
using Mediator;
using NSubstitute;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Events;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class DomainEventPublisherTests
{
    private readonly IPublisher _mockPublisher;
    private readonly IDomainEventCollector _mockCollector;
    private readonly DomainEventPublisher _sut;

    public DomainEventPublisherTests()
    {
        _mockPublisher = Substitute.For<IPublisher>();
        _mockCollector = Substitute.For<IDomainEventCollector>();
        _sut = new DomainEventPublisher(_mockPublisher, _mockCollector);
    }

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

    #endregion
}
