using Functorium.Adapters.Observabilities;
using OpenTelemetry.Metrics;

namespace Functorium.Adapters.Observabilities.Builders;

/// <summary>
/// OpenTelemetry Metrics 확장 설정을 위한 Builder 클래스
/// Meter, Instrumentation 등 프로젝트별 Metrics 확장 포인트 제공
/// </summary>
public class ConfigurationBuilderMetric
{
    private readonly List<string> _meterNames = new();
    private readonly List<Action<MeterProviderBuilder>> _instrumentations = new();
    private readonly List<Action<MeterProviderBuilder>> _additionalConfigurations = new();
    private readonly OpenTelemetryOptions _options;

    internal ConfigurationBuilderMetric(OpenTelemetryOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// OpenTelemetryOptions 접근
    /// </summary>
    public OpenTelemetryOptions Options => _options;

    /// <summary>
    /// 추가 Meter를 이름으로 등록
    /// </summary>
    /// <param name="meterName">등록할 Meter 이름 (와일드카드 지원: "MyApp.*")</param>
    public ConfigurationBuilderMetric AddMeter(string meterName)
    {
        if (string.IsNullOrWhiteSpace(meterName))
            throw new ArgumentException("Meter name cannot be null or whitespace.", nameof(meterName));

        _meterNames.Add(meterName);
        return this;
    }

    /// <summary>
    /// 추가 Instrumentation 등록
    /// </summary>
    /// <param name="configure">MeterProviderBuilder에 Instrumentation을 추가하는 액션</param>
    public ConfigurationBuilderMetric AddInstrumentation(Action<MeterProviderBuilder> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        _instrumentations.Add(configure);
        return this;
    }

    /// <summary>
    /// MeterProviderBuilder에 직접 접근하여 추가 설정 수행
    /// </summary>
    /// <param name="configure">MeterProviderBuilder를 수정하는 액션</param>
    public ConfigurationBuilderMetric Configure(Action<MeterProviderBuilder> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        _additionalConfigurations.Add(configure);
        return this;
    }

    /// <summary>
    /// 등록된 설정들을 MeterProviderBuilder에 적용
    /// </summary>
    /// <param name="meterProviderBuilder">적용할 MeterProviderBuilder 인스턴스</param>
    internal void Apply(MeterProviderBuilder meterProviderBuilder)
    {
        // 추가 Meter 등록
        foreach (string meterName in _meterNames)
        {
            meterProviderBuilder.AddMeter(meterName);
        }

        // 추가 Instrumentation 적용
        foreach (Action<MeterProviderBuilder> instrumentation in _instrumentations)
        {
            instrumentation(meterProviderBuilder);
        }

        // 추가 설정 적용
        foreach (Action<MeterProviderBuilder> configuration in _additionalConfigurations)
        {
            configuration(meterProviderBuilder);
        }
    }
}

