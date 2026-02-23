using System.Diagnostics;
using System.Diagnostics.Metrics;

using Functorium.Adapters.Observabilities;
using Functorium.Testing.Arrangements.Logging;

using LanguageExt;

using Microsoft.Extensions.Options;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

/// <summary>
/// SourceGenerator로 생성된 Adapter Observable의 로그 필드 검증 테스트.
/// 런타임에서 실제 Observable을 실행하고 로그 출력을 스냅샷으로 검증합니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 생성된 Observable 코드의 로깅 필드 구조가 실수로 변경되는 것을 방지합니다.
/// </para>
/// <para>
/// 로그 필드 구조 비교표:
/// </para>
/// <code>
/// +--------------------------+---------------+---------------+---------------+
/// | Field Key                | Request       | Response      | Response      |
/// |                          | (Info/Debug)  | (success)     | (failure)     |
/// +--------------------------+---------------+---------------+---------------+
/// | request.layer            | "adapter"     | "adapter"     | "adapter"     |
/// | request.category         | category name | category name | category name |
/// | request.handler          | handler name  | handler name  | handler name  |
/// | request.handler.method   | method name   | method name   | method name   |
/// | request.params.{name}    | param value   | (none)        | (none)        |
/// | response.status          | (none)        | "success"     | "failure"     |
/// | response.elapsed         | (none)        | elapsed (s)   | elapsed (s)   |
/// | response.result          | (none)        | result value  | (none)        |
/// | response.result.count    | (none)        | count (opt)   | (none)        |
/// | error.type               | (none)        | (none)        | "expected"/   |
/// |                          |               |               | "exceptional"/|
/// |                          |               |               | "aggregate"   |
/// | error.code               | (none)        | (none)        | error code    |
/// | @error                   | (none)        | (none)        | error object  |
/// +--------------------------+---------------+---------------+---------------+
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_SourceGenerator)]
public sealed class ObservablePortLoggingStructureTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly IMeterFactory _meterFactory;
    private readonly IOptions<OpenTelemetryOptions> _openTelemetryOptions;

    public ObservablePortLoggingStructureTests()
    {
        _activitySource = new ActivitySource("Test.AdapterLogging");
        _meterFactory = new TestMeterFactory();
        _openTelemetryOptions = MsOptions.Create(new OpenTelemetryOptions { ServiceNamespace = "TestService" });
    }

    public void Dispose()
    {
        _activitySource.Dispose();
        (_meterFactory as IDisposable)?.Dispose();
    }

    // ===== Request 로그 필드 검증 =====

    [Fact]
    public async Task Request_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<TestObservabilityAdapterObservable>();

        var pipeline = new TestObservabilityAdapterObservable(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions);

        var testId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        await pipeline.GetById(testId).Run().RunAsync();

        // Assert
        await Verify(context.ExtractFirstLogData()).UseDirectory("Snapshots/ObservablePortLoggingStructure");
    }

    // ===== Success Response 로그 필드 검증 =====

    [Fact]
    public async Task SuccessResponse_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<TestObservabilityAdapterObservable>();

        var pipeline = new TestObservabilityAdapterObservable(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions);

        var testId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Act
        await pipeline.GetById(testId).Run().RunAsync();

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/ObservablePortLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Warning Response 로그 필드 검증 (Expected Error) =====

    [Fact]
    public async Task WarningResponse_WithExpectedError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<TestObservabilityAdapterObservable>();

        var pipeline = new TestObservabilityAdapterObservable(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateExpectedError()
        };

        var testId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Act - 에러가 발생해도 로그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - 로그 필드 검증이 목적
        }

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/ObservablePortLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    [Fact]
    public async Task WarningResponse_WithExpectedErrorT_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<TestObservabilityAdapterObservable>();

        var pipeline = new TestObservabilityAdapterObservable(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateExpectedErrorT()
        };

        var testId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        // Act - 에러가 발생해도 로그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - 로그 필드 검증이 목적
        }

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/ObservablePortLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Error Response 로그 필드 검증 (Exceptional Error) =====

    [Fact]
    public async Task ErrorResponse_WithExceptionalError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<TestObservabilityAdapterObservable>();

        var pipeline = new TestObservabilityAdapterObservable(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateExceptionalError()
        };

        var testId = Guid.Parse("55555555-5555-5555-5555-555555555555");

        // Act - 에러가 발생해도 로그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - 로그 필드 검증이 목적
        }

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/ObservablePortLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    // ===== Aggregate Error 로그 필드 검증 =====

    [Fact]
    public async Task ErrorResponse_WithAggregateError_Should_Log_Expected_Fields()
    {
        // Arrange
        using var context = new LogTestContext();
        var logger = context.CreateLogger<TestObservabilityAdapterObservable>();

        var pipeline = new TestObservabilityAdapterObservable(
            _activitySource,
            logger,
            _meterFactory,
            _openTelemetryOptions)
        {
            GetByIdHandler = _ => TestErrors.CreateAggregateError()
        };

        var testId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        // Act - 에러가 발생해도 로그는 기록됨
        try
        {
            await pipeline.GetById(testId).Run().RunAsync();
        }
        catch
        {
            // 에러는 무시 - 로그 필드 검증이 목적
        }

        // Assert
        await Verify(context.ExtractSecondLogData())
            .UseDirectory("Snapshots/ObservablePortLoggingStructure")
            .ScrubMember("response.elapsed");
    }

    #region Helper Types

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
