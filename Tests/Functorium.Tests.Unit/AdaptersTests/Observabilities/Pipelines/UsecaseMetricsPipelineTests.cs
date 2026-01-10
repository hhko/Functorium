using System.Diagnostics.Metrics;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Configurations;
using Functorium.Adapters.Observabilities.Pipelines;
using LanguageExt.Common;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using static Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines.TestFixtures;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Pipelines;

/// <summary>
/// UsecaseMetricsPipeline 테스트
/// 메트릭 수집 파이프라인 테스트
/// </summary>
public sealed class UsecaseMetricsPipelineTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IMeterFactory _meterFactory;
    private readonly MeterListener _meterListener;

    public UsecaseMetricsPipelineTests()
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

    private static Microsoft.Extensions.Options.IOptions<OpenTelemetryOptions> CreateTestOpenTelemetryOptions()
    {
        return Microsoft.Extensions.Options.Options.Create(new OpenTelemetryOptions { ServiceNamespace = "Test.Service" });
    }

    private static Microsoft.Extensions.Options.IOptions<SloConfiguration> CreateTestSloOptions()
    {
        return Microsoft.Extensions.Options.Options.Create(new SloConfiguration());
    }

    [Fact]
    public async Task Handle_SuccessfulRequest_RecordsMetrics()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloOptions = CreateTestSloOptions();
        var pipeline = new UsecaseMetricsPipeline<SimpleTestRequest, TestResponse>(options, _meterFactory, sloOptions);
        var request = new SimpleTestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedResponse.Id);
    }

    [Fact]
    public async Task Handle_FailedRequest_RecordsFailureMetrics()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloOptions = CreateTestSloOptions();
        var pipeline = new UsecaseMetricsPipeline<SimpleTestRequest, TestResponse>(options, _meterFactory, sloOptions);
        var request = new SimpleTestRequest("Test");
        var errorResponse = TestResponse.CreateFail(Error.New("Test error"));

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(errorResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFail.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_MeasuresElapsedTime()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloOptions = CreateTestSloOptions();
        var pipeline = new UsecaseMetricsPipeline<SimpleTestRequest, TestResponse>(options, _meterFactory, sloOptions);
        var request = new SimpleTestRequest("Test");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            async (_, _) =>
            {
                await Task.Delay(10); // Small delay to measure
                return expectedResponse;
            };

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_PreservesResponseFromHandler()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloOptions = CreateTestSloOptions();
        var pipeline = new UsecaseMetricsPipeline<SimpleTestRequest, TestResponse>(options, _meterFactory, sloOptions);
        var request = new SimpleTestRequest("Test");
        var expectedId = Guid.NewGuid();
        var expectedResponse = TestResponse.CreateSuccess(expectedId);

        MessageHandlerDelegate<SimpleTestRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
        result.Id.ShouldBe(expectedId);
    }

    [Fact]
    public async Task Handle_CommandRequest_RecordsMetrics()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloOptions = CreateTestSloOptions();
        var pipeline = new UsecaseMetricsPipeline<TestCommandRequest, TestResponse>(options, _meterFactory, sloOptions);
        var request = new TestCommandRequest("Command");
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestCommandRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_QueryRequest_RecordsMetrics()
    {
        // Arrange
        var options = CreateTestOpenTelemetryOptions();
        var sloOptions = CreateTestSloOptions();
        var pipeline = new UsecaseMetricsPipeline<TestQueryRequest, TestResponse>(options, _meterFactory, sloOptions);
        var request = new TestQueryRequest(Guid.NewGuid());
        var expectedResponse = TestResponse.CreateSuccess(Guid.NewGuid());

        MessageHandlerDelegate<TestQueryRequest, TestResponse> next =
            (_, _) => ValueTask.FromResult(expectedResponse);

        // Act
        TestResponse result = await pipeline.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSucc.ShouldBeTrue();
    }
}
