using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Functorium.Adapters.Observabilities.Builders.Configurators;

/// <summary>
/// OpenTelemetry Tracing 확장 설정을 위한 Configurator 클래스
/// ActivitySource, Processor 등 프로젝트별 Tracing 확장 포인트 제공
/// </summary>
public class TracingConfigurator
{
    private readonly List<string> _sourceNames = new();
    private readonly List<BaseProcessor<Activity>> _processors = new();
    private readonly List<Action<TracerProviderBuilder>> _additionalConfigurations = new();
    private readonly OpenTelemetryOptions _options;

    internal TracingConfigurator(OpenTelemetryOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// OpenTelemetryOptions 접근
    /// </summary>
    public OpenTelemetryOptions Options => _options;

    /// <summary>
    /// 추가 ActivitySource를 이름으로 등록
    /// </summary>
    /// <param name="sourceName">등록할 ActivitySource 이름 (와일드카드 지원: "MyApp.*")</param>
    public TracingConfigurator AddSource(string sourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        _sourceNames.Add(sourceName);
        return this;
    }

    /// <summary>
    /// 추가 Processor 등록
    /// </summary>
    /// <param name="processor">등록할 Processor 인스턴스</param>
    public TracingConfigurator AddProcessor(BaseProcessor<Activity> processor)
    {
        ArgumentNullException.ThrowIfNull(processor);
        _processors.Add(processor);
        return this;
    }

    /// <summary>
    /// TracerProviderBuilder에 직접 접근하여 추가 설정 수행
    /// </summary>
    /// <param name="configure">TracerProviderBuilder를 수정하는 액션</param>
    public TracingConfigurator Configure(Action<TracerProviderBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _additionalConfigurations.Add(configure);
        return this;
    }

    /// <summary>
    /// 등록된 설정들을 TracerProviderBuilder에 적용
    /// </summary>
    /// <param name="tracerProviderBuilder">적용할 TracerProviderBuilder 인스턴스</param>
    internal void Apply(TracerProviderBuilder tracerProviderBuilder)
    {
        // 추가 ActivitySource 등록
        foreach (string sourceName in _sourceNames)
        {
            tracerProviderBuilder.AddSource(sourceName);
        }

        // 추가 Processor 등록
        foreach (BaseProcessor<Activity> processor in _processors)
        {
            tracerProviderBuilder.AddProcessor(processor);
        }

        // 추가 설정 적용
        foreach (Action<TracerProviderBuilder> configuration in _additionalConfigurations)
        {
            configuration(tracerProviderBuilder);
        }
    }
}
