using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Applications.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class ObservableDomainEventPublisherTests
{
    private readonly ActivitySource _activitySource;
    private readonly IDomainEventPublisher _mockInner;
    private readonly ILogger<ObservableDomainEventPublisher> _mockLogger;
    private readonly ObservableDomainEventPublisher _sut;

    public ObservableDomainEventPublisherTests()
    {
        _activitySource = new ActivitySource("TestActivitySource");
        _mockInner = Substitute.For<IDomainEventPublisher>();
        _mockLogger = Substitute.For<ILogger<ObservableDomainEventPublisher>>();
        var meterFactory = new TestMeterFactory();
        var openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestPublisher" });
        _sut = new ObservableDomainEventPublisher(_activitySource, _mockInner, _mockLogger, meterFactory, openTelemetryOptions);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];
        public Meter Create(MeterOptions options) { var meter = new Meter(options); _meters.Add(meter); return meter; }
        public void Dispose() { foreach (var meter in _meters) meter.Dispose(); _meters.Clear(); }
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
}
