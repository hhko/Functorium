using Functorium.Adapters.Observabilities;
using Serilog;
using Serilog.Core;

namespace Functorium.Adapters.Observabilities.Builders;

/// <summary>
/// Serilog 확장 설정을 위한 Builder 클래스
/// Enricher, DestructuringPolicy 등 프로젝트별 Serilog 확장 포인트 제공
/// </summary>
public class ConfigurationBuilderLogger
{
    private readonly List<ILogEventEnricher> _enrichers = new();
    private readonly List<IDestructuringPolicy> _destructuringPolicies = new();
    private readonly List<Action<LoggerConfiguration>> _additionalConfigurations = new();
    //private readonly (int maxDepth, int maxStringLength, int maxCollectionCount) _destructureSettings;
    private readonly OpenTelemetryOptions _options;

    internal ConfigurationBuilderLogger(
        //(int maxDepth, int maxStringLength, int maxCollectionCount) destructureSettings,
        OpenTelemetryOptions options)
    {
        //_destructureSettings = destructureSettings;
        _options = options;
    }

    ///// <summary>
    ///// Destructure 설정값 접근
    ///// </summary>
    //public (int maxDepth, int maxStringLength, int maxCollectionCount) DestructureSettings => _destructureSettings;

    /// <summary>
    /// OpenTelemetryOptions 접근
    /// </summary>
    public OpenTelemetryOptions Options => _options;

    /// <summary>
    /// DestructuringPolicy를 타입으로 등록
    /// </summary>
    /// <typeparam name="TPolicy">IDestructuringPolicy 구현 타입</typeparam>
    public ConfigurationBuilderLogger AddDestructuringPolicy<TPolicy>()
        where TPolicy : IDestructuringPolicy, new()
    {
        _destructuringPolicies.Add(new TPolicy());
        return this;
    }

    /// <summary>
    /// Enricher 인스턴스를 직접 등록
    /// </summary>
    /// <param name="enricher">등록할 Enricher 인스턴스</param>
    public ConfigurationBuilderLogger AddEnricher(ILogEventEnricher enricher)
    {
        if (enricher == null)
            throw new ArgumentNullException(nameof(enricher));

        _enrichers.Add(enricher);
        return this;
    }

    /// <summary>
    /// Enricher를 타입으로 등록
    /// </summary>
    /// <typeparam name="TEnricher">ILogEventEnricher 구현 타입</typeparam>
    public ConfigurationBuilderLogger AddEnricher<TEnricher>()
        where TEnricher : ILogEventEnricher, new()
    {
        _enrichers.Add(new TEnricher());
        return this;
    }

    /// <summary>
    /// LoggerConfiguration에 직접 접근하여 추가 설정 수행
    /// </summary>
    /// <param name="configure">LoggerConfiguration을 수정하는 액션</param>
    public ConfigurationBuilderLogger Configure(Action<LoggerConfiguration> configure)
    {
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        _additionalConfigurations.Add(configure);
        return this;
    }

    /// <summary>
    /// 등록된 설정들을 LoggerConfiguration에 적용
    /// </summary>
    /// <param name="loggerConfiguration">적용할 LoggerConfiguration 인스턴스</param>
    internal void Apply(LoggerConfiguration loggerConfiguration)
    {
        // DestructuringPolicy 적용
        foreach (IDestructuringPolicy policy in _destructuringPolicies)
        {
            loggerConfiguration.Destructure.With(policy);
        }

        // Enricher 적용
        foreach (ILogEventEnricher enricher in _enrichers)
        {
            loggerConfiguration.Enrich.With(enricher);
        }

        // 추가 설정 적용
        foreach (Action<LoggerConfiguration> configuration in _additionalConfigurations)
        {
            configuration(loggerConfiguration);
        }
    }
}

