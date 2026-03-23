using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Abstractions.Errors;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Events;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Events;
using Functorium.Domains.Events;
using Functorium.Tests.Unit.DomainsTests.Entities;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using MsOptions = Microsoft.Extensions.Options.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Events;

/// <summary>
/// ObservableDomainEventPublisher의 Tracing 태그 구조를 검증하는 테스트입니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 Tracing 태그 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// Publish 메서드 Activity 태그 구조 비교표:
/// </para>
/// <code>
/// +--------------------------+-------------------+-------------------+
/// | Tag Key                  | Success           | Failure           |
/// +--------------------------+-------------------+-------------------+
/// | request.layer            | "adapter"         | "adapter"         |
/// | request.category         | "event"           | "event"           |
/// | request.handler          | event type name   | event type name   |
/// | request.handler_method   | "Publish"         | "Publish"         |
/// | response.elapsed         | elapsed seconds   | elapsed seconds   |
/// | response.status          | "success"         | "failure"         |
/// | error.type               | (none)            | "expected"/       |
/// |                          |                   | "exceptional"     |
/// | error.code               | (none)            | error code        |
/// +--------------------------+-------------------+-------------------+
/// | Total Tags               | 6                 | 8                 |
/// +--------------------------+-------------------+-------------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public sealed class DomainEventPublisherTracingStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _activityListener;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;
    private readonly IDomainEventPublisher _mockInner;
    private readonly IDomainEventCollector _mockCollector;
    private readonly ILogger<ObservableDomainEventPublisher> _mockLogger;
    private Activity? _capturedActivity;

    public DomainEventPublisherTracingStructureTests()
    {
        _activitySource = new ActivitySource("Test.DomainEventPublisherTracing");
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _activitySource.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _capturedActivity = activity
        };
        ActivitySource.AddActivityListener(_activityListener);

        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestPublisherTracing" });
        _mockInner = Substitute.For<IDomainEventPublisher>();
        _mockCollector = Substitute.For<IDomainEventCollector>();
        _mockCollector.GetTrackedAggregates().Returns(new List<IHasDomainEvents>());
        _mockLogger = Substitute.For<ILogger<ObservableDomainEventPublisher>>();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _activitySource.Dispose();
        (_meterFactory as IDisposable)?.Dispose();
    }

    #region Span 이름 검증

    /// <summary>
    /// Publish 메서드 호출 시 Span 이름이 올바른 패턴을 따라야 합니다.
    /// 패턴: "adapter event {EventType}.Publish"
    /// </summary>
    [Fact]
    public async Task Publish_ShouldCreateActivityWithCorrectName()
    {
        // Arrange
        var sut = CreateSut();
        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.DisplayName.ShouldBe("adapter event TestDomainEvent.Publish");
    }

    #endregion

    #region Tag 개수 검증

    /// <summary>
    /// Publish 성공 시 6개 태그를 가져야 합니다.
    /// (request 4개 + response.elapsed + response.status)
    /// </summary>
    [Fact]
    public async Task Publish_Success_ShouldHaveSixTags()
    {
        // Arrange
        var sut = CreateSut();
        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.TagObjects.Count().ShouldBe(6);
    }

    /// <summary>
    /// Publish 실패 시 8개 태그를 가져야 합니다.
    /// (request 4개 + response.elapsed + response.status + error.type + error.code)
    /// </summary>
    [Fact]
    public async Task Publish_Failure_ShouldHaveEightTags()
    {
        // Arrange
        var sut = CreateSut();
        var domainEvent = new TestDomainEvent("Test");
        var error = new ErrorCodeExpected("Event.NotFound", "testValue", "Event not found");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.TagObjects.Count().ShouldBe(8);
    }

    #endregion

    #region ActivityStatus 검증

    /// <summary>
    /// Publish 성공 시 ActivityStatus가 Ok여야 합니다.
    /// </summary>
    [Fact]
    public async Task Publish_Success_ShouldSetActivityStatusOk()
    {
        // Arrange
        var sut = CreateSut();
        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.Status.ShouldBe(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Publish 실패 시 ActivityStatus가 Error여야 합니다.
    /// </summary>
    [Fact]
    public async Task Publish_Failure_ShouldSetActivityStatusError()
    {
        // Arrange
        var sut = CreateSut();
        var domainEvent = new TestDomainEvent("Test");
        var error = new ErrorCodeExpected("Event.NotFound", "testValue", "Event not found");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        _capturedActivity.ShouldNotBeNull();
        _capturedActivity.Status.ShouldBe(ActivityStatusCode.Error);
    }

    #endregion

    #region Snapshot 태그 구조 테스트

    /// <summary>
    /// Publish 메서드 Success 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Publish_SuccessTags()
    {
        // Arrange
        var sut = CreateSut();
        var domainEvent = new TestDomainEvent("Test");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Succ<IO, LanguageExt.Unit>(LanguageExt.Unit.Default));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots/DomainEventPublisherTracingStructure")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Publish 메서드 Failure (Expected Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Publish_FailureResponse_ExpectedError_Tags()
    {
        // Arrange
        var sut = CreateSut();
        var domainEvent = new TestDomainEvent("Test");
        var error = new ErrorCodeExpected("Event.NotFound", "testValue", "Event not found");
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots/DomainEventPublisherTracingStructure")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    /// <summary>
    /// Publish 메서드 Failure (Exceptional Error) 태그 구조를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public async Task Snapshot_Publish_FailureResponse_ExceptionalError_Tags()
    {
        // Arrange
        var sut = CreateSut();
        var domainEvent = new TestDomainEvent("Test");
        var exception = new InvalidOperationException("Connection failed");
        var error = new ErrorCodeExceptional("Event.ConnectionError", exception);
        _mockInner
            .Publish(Arg.Any<TestDomainEvent>(), Arg.Any<CancellationToken>())
            .Returns(FinT.Fail<IO, LanguageExt.Unit>(error));

        // Act
        await sut.Publish(domainEvent).Run().RunAsync();

        // Assert
        var tags = ExtractActivityTags();
        await Verify(tags)
            .UseDirectory("Snapshots/DomainEventPublisherTracingStructure")
            .ScrubMember(ObservabilityNaming.CustomAttributes.ResponseElapsed);
    }

    #endregion

    #region Helper Methods

    private ObservableDomainEventPublisher CreateSut()
    {
        return new ObservableDomainEventPublisher(
            _activitySource, _mockInner, _mockCollector, _mockLogger, _meterFactory, _openTelemetryOptions);
    }

    private Dictionary<string, string?> ExtractActivityTags()
    {
        _capturedActivity.ShouldNotBeNull();
        return _capturedActivity.TagObjects
            .OrderBy(t => t.Key)
            .ToDictionary(t => t.Key, t => t.Value?.ToString());
    }

    #endregion

    #region Test Types

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly List<Meter> _meters = [];

        public Meter Create(MeterOptions options)
        {
            var meter = new Meter(options);
            _meters.Add(meter);
            return meter;
        }

        public void Dispose()
        {
            foreach (var meter in _meters)
            {
                meter.Dispose();
            }
            _meters.Clear();
        }
    }

    #endregion
}
