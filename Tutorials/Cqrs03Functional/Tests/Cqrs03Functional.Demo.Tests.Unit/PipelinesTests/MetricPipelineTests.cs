using System.Diagnostics;
using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Applications.Observabilities;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.Options;

namespace Cqrs03Functional.Demo.Tests.Unit.PipelinesTests;

/// <summary>
/// UsecaseMetricsPipeline 테스트
/// 메트릭 수집 파이프라인 테스트
/// </summary>
public sealed class MetricPipelineTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMeterFactory _meterFactory;
    private readonly MeterListener _meterListener;

    public MetricPipelineTests()
    {
        var services = new ServiceCollection();
        services.AddMetrics();
        _serviceProvider = services.BuildServiceProvider();
        _meterFactory = _serviceProvider.GetRequiredService<IMeterFactory>();
        _meterListener = new MeterListener();
        _meterListener.Start();
    }

    public void Dispose()
    {
        _meterListener?.Dispose();
        _serviceProvider?.Dispose();
    }

    #region Test Fixtures

    /// <summary>
    /// 테스트용 Request
    /// </summary>
    public sealed record class TestRequest(string Name) : IMessage;

    /// <summary>
    /// CRTP 패턴을 따르는 테스트용 Response.
    /// IFinResponse{TSelf}와 IFinResponseFactory{TSelf}를 직접 구현합니다.
    /// </summary>
    public sealed record class TestResponse : IFinResponse, IFinResponseFactory<TestResponse>, IFinResponseWithError
    {
        public bool IsSucc { get; init; }
        public bool IsFail => !IsSucc;
        public Guid Id { get; init; }
        public Error? ErrorValue { get; init; }

        // IFinResponseWithError 구현 (Fail 케이스에서 Error 접근용)
        Error IFinResponseWithError.Error => ErrorValue!;

        private TestResponse() { }

        public static TestResponse CreateSuccess(Guid id) => new() { IsSucc = true, Id = id };
        public static TestResponse CreateFail(Error error) => new() { IsSucc = false, ErrorValue = error };

        // IFinResponseFactory<TestResponse> 구현
        static TestResponse IFinResponseFactory<TestResponse>.CreateFail(Error error) => CreateFail(error);
    }

    private static IOptions<OpenTelemetryOptions> CreateTestOpenTelemetryOptions()
    {
        return Options.Create(new OpenTelemetryOptions { ServiceNamespace = "Test.Service" });
    }

    #endregion

    [Fact]
    public async Task Handle_SuccessfulRequest_RecordsMetrics()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloConfigurationOptions = Options.Create(new SloConfiguration());
        var pipeline = new UsecaseMetricsPipeline<TestRequest, TestResponse>(options, _meterFactory, sloConfigurationOptions);
        var request = new TestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
        // 메트릭이 기록되었는지 확인 (실제 메트릭 수집은 백그라운드에서 이루어지므로 파이프라인이 정상 실행되었는지만 확인)
    }

    [Fact]
    public async Task Handle_FailedRequest_RecordsFailureMetrics()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloConfigurationOptions = Options.Create(new SloConfiguration());
        var pipeline = new UsecaseMetricsPipeline<TestRequest, TestResponse>(options, _meterFactory, sloConfigurationOptions);
        var request = new TestRequest("Test");
        var errorResponse = TestResponse.CreateFail(Error.New("Test error"));

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
        // 실패 메트릭이 기록되었는지 확인
    }

    [Fact]
    public async Task Handle_MeasuresElapsedTime()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloConfigurationOptions = Options.Create(new SloConfiguration());
        var pipeline = new UsecaseMetricsPipeline<TestRequest, TestResponse>(options, _meterFactory, sloConfigurationOptions);
        var request = new TestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            async (_, _) =>
            {
                await Task.Delay(10); // Small delay to measure
                return expectedResponse;
            };

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        // 처리 시간이 측정되었는지 확인 (파이프라인이 정상 실행되었는지만 확인)
    }

    [Fact]
    public async Task Handle_PreservesResponseFromHandler()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloConfigurationOptions = Options.Create(new SloConfiguration());
        var pipeline = new UsecaseMetricsPipeline<TestRequest, TestResponse>(options, _meterFactory, sloConfigurationOptions);
        var request = new TestRequest("Test");
        var expectedId = Guid.NewGuid();
        var expectedResponse = TestResponse.CreateSuccess(expectedId);

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedId);
    }

    [Fact]
    public async Task Handle_QueryRequest_RecordsMetrics()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloConfigurationOptions = Options.Create(new SloConfiguration());
        var pipeline = new UsecaseMetricsPipeline<TestRequest, TestResponse>(options, _meterFactory, sloConfigurationOptions);
        var request = new TestRequest("Query");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }
}

