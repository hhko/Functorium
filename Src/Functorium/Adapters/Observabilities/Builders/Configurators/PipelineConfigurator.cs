using Functorium.Adapters.Observabilities.Pipelines;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Functorium.Adapters.Observabilities.Builders.Configurators;

/// <summary>
/// Usecase 파이프라인 설정을 위한 Configurator 클래스
/// Fluent API를 통해 파이프라인 활성화/비활성화 및 커스터마이징 지원
/// </summary>
/// <remarks>
/// 파이프라인 실행 순서 (등록 순서):
/// Request → Metrics → Tracing → Logging → Validation → Exception → Custom → Handler
/// </remarks>
public class PipelineConfigurator
{
    private bool _useMetrics;
    private bool _useTracing;
    private bool _useLogging;
    private bool _useValidation;
    private bool _useException;
    private ServiceLifetime _lifetime = ServiceLifetime.Scoped;
    private readonly List<Type> _customPipelines = new();

    internal PipelineConfigurator()
    {
    }

    /// <summary>
    /// 모든 기본 파이프라인을 활성화합니다.
    /// Metrics, Tracing, Logging, Validation, Exception 파이프라인이 등록됩니다.
    /// </summary>
    public PipelineConfigurator UseAll()
    {
        _useMetrics = true;
        _useTracing = true;
        _useLogging = true;
        _useValidation = true;
        _useException = true;
        return this;
    }

    /// <summary>
    /// Metrics Pipeline을 활성화합니다.
    /// OpenTelemetry Metrics를 통해 요청 수, 응답 시간 등을 수집합니다.
    /// </summary>
    public PipelineConfigurator UseMetrics()
    {
        _useMetrics = true;
        return this;
    }

    /// <summary>
    /// Tracing Pipeline을 활성화합니다.
    /// OpenTelemetry Activity 기반 분산 추적을 수행합니다.
    /// </summary>
    public PipelineConfigurator UseTracing()
    {
        _useTracing = true;
        return this;
    }

    /// <summary>
    /// Logging Pipeline을 활성화합니다.
    /// 요청/응답 정보를 로그로 기록합니다.
    /// </summary>
    public PipelineConfigurator UseLogging()
    {
        _useLogging = true;
        return this;
    }

    /// <summary>
    /// Validation Pipeline을 활성화합니다.
    /// FluentValidation을 통해 요청 유효성 검사를 수행합니다.
    /// </summary>
    public PipelineConfigurator UseValidation()
    {
        _useValidation = true;
        return this;
    }

    /// <summary>
    /// Exception Pipeline을 활성화합니다.
    /// 예외를 FinResponse.Fail로 변환합니다.
    /// </summary>
    public PipelineConfigurator UseException()
    {
        _useException = true;
        return this;
    }

    /// <summary>
    /// 파이프라인 서비스의 Lifetime을 설정합니다.
    /// 기본값: Scoped
    /// </summary>
    /// <param name="lifetime">서비스 Lifetime</param>
    public PipelineConfigurator WithLifetime(ServiceLifetime lifetime)
    {
        _lifetime = lifetime;
        return this;
    }

    /// <summary>
    /// 커스텀 파이프라인을 추가합니다.
    /// 커스텀 파이프라인은 기본 파이프라인 다음에 등록됩니다 (Handler에 가장 가까움).
    /// </summary>
    /// <typeparam name="TPipeline">IPipelineBehavior를 구현하는 파이프라인 타입</typeparam>
    public PipelineConfigurator AddCustomPipeline<TPipeline>()
        where TPipeline : class
    {
        _customPipelines.Add(typeof(TPipeline));
        return this;
    }

    /// <summary>
    /// 설정을 IServiceCollection에 적용합니다.
    /// </summary>
    /// <param name="services">IServiceCollection 인스턴스</param>
    internal void Apply(IServiceCollection services)
    {
        // 파이프라인 등록 순서:
        // Request → Metrics → Tracing → Logging → Validation → Exception → Custom → Handler

        if (_useMetrics)
        {
            RegisterPipeline(services, typeof(UsecaseMetricsPipeline<,>));
        }

        if (_useTracing)
        {
            RegisterPipeline(services, typeof(UsecaseTracingPipeline<,>));
        }

        if (_useLogging)
        {
            RegisterPipeline(services, typeof(UsecaseLoggingPipeline<,>));
        }

        if (_useValidation)
        {
            RegisterPipeline(services, typeof(UsecaseValidationPipeline<,>));
        }

        if (_useException)
        {
            RegisterPipeline(services, typeof(UsecaseExceptionPipeline<,>));
        }

        // 커스텀 파이프라인 등록
        foreach (Type customPipeline in _customPipelines)
        {
            RegisterPipeline(services, customPipeline);
        }
    }

    private void RegisterPipeline(IServiceCollection services, Type pipelineType)
    {
        switch (_lifetime)
        {
            case ServiceLifetime.Singleton:
                services.AddSingleton(typeof(IPipelineBehavior<,>), pipelineType);
                break;
            case ServiceLifetime.Scoped:
                services.AddScoped(typeof(IPipelineBehavior<,>), pipelineType);
                break;
            case ServiceLifetime.Transient:
                services.AddTransient(typeof(IPipelineBehavior<,>), pipelineType);
                break;
        }
    }
}
