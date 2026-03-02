using Functorium.Adapters.Observabilities.Pipelines;
using Functorium.Applications.Events;
using Functorium.Applications.Persistence;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Functorium.Adapters.Observabilities.Builders.Configurators;

/// <summary>
/// Usecase 파이프라인 설정을 위한 Configurator 클래스
/// Fluent API를 통해 파이프라인 활성화/비활성화 및 커스터마이징 지원
/// </summary>
/// <remarks>
/// 파이프라인 실행 순서 (등록 순서):
/// Request → Metrics → Tracing → Logging → Validation → Caching → Exception → Transaction → Custom → Handler
/// </remarks>
public class PipelineConfigurator
{
    private bool _useMetrics;
    private bool _useTracing;
    private bool _useLogging;
    private bool _useValidation;
    private bool _useCaching;
    private bool _useException;
    private bool _useTransaction;
    private ServiceLifetime _lifetime = ServiceLifetime.Scoped;
    private readonly List<Type> _customPipelines = new();
    private readonly List<string> _registeredPipelineNames = new();

    internal PipelineConfigurator()
    {
    }

    /// <summary>
    /// 모든 기본 파이프라인을 활성화합니다.
    /// Metrics, Tracing, Logging, Validation, Exception, Transaction 파이프라인이 등록됩니다.
    /// </summary>
    public PipelineConfigurator UseAll()
    {
        _useMetrics = true;
        _useTracing = true;
        _useLogging = true;
        _useValidation = true;
        _useException = true;
        _useTransaction = true;
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
    /// Caching Pipeline을 활성화합니다.
    /// ICacheable을 구현한 Query 요청에 대해 IMemoryCache 기반 캐싱을 수행합니다.
    /// IMemoryCache가 DI에 등록되어 있어야 합니다 (AddMemoryCache()).
    /// </summary>
    public PipelineConfigurator UseCaching()
    {
        _useCaching = true;
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
    /// Transaction Pipeline을 활성화합니다.
    /// Command Usecase에 대해 UoW.SaveChanges + 도메인 이벤트 발행을 자동 처리합니다.
    /// Query는 바이패스됩니다.
    /// </summary>
    /// <remarks>
    /// IUnitOfWork, IDomainEventPublisher, IDomainEventCollector가 DI에 등록되어 있어야 합니다.
    /// UseAll()에 포함되어 있으므로 별도 호출 없이도 활성화됩니다.
    /// </remarks>
    public PipelineConfigurator UseTransaction()
    {
        _useTransaction = true;
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
        // Request → Metrics → Tracing → Logging → Validation → Caching → Exception → Transaction → Custom → Handler

        _registeredPipelineNames.Clear();

        if (_useMetrics)
        {
            RegisterPipeline(services, typeof(UsecaseMetricsPipeline<,>));
            _registeredPipelineNames.Add("Metrics");
        }

        if (_useTracing)
        {
            RegisterPipeline(services, typeof(UsecaseTracingPipeline<,>));
            _registeredPipelineNames.Add("Tracing");
        }

        if (_useLogging)
        {
            RegisterPipeline(services, typeof(UsecaseLoggingPipeline<,>));
            _registeredPipelineNames.Add("Logging");
        }

        if (_useValidation)
        {
            RegisterPipeline(services, typeof(UsecaseValidationPipeline<,>));
            _registeredPipelineNames.Add("Validation");
        }

        if (_useCaching)
        {
            RegisterPipeline(services, typeof(UsecaseCachingPipeline<,>));
            _registeredPipelineNames.Add("Caching");
        }

        if (_useException)
        {
            RegisterPipeline(services, typeof(UsecaseExceptionPipeline<,>));
            _registeredPipelineNames.Add("Exception");
        }

        if (_useTransaction && HasTransactionDependencies(services))
        {
            RegisterPipeline(services, typeof(UsecaseTransactionPipeline<,>));
            _registeredPipelineNames.Add("Transaction");
        }

        // 커스텀 파이프라인 등록
        foreach (Type customPipeline in _customPipelines)
        {
            RegisterPipeline(services, customPipeline);
            _registeredPipelineNames.Add(ExtractPipelineName(customPipeline));
        }

        _registeredPipelineNames.Add("Handler");
    }

    /// <summary>
    /// Apply() 이후 등록된 파이프라인 이름 목록을 반환합니다.
    /// </summary>
    internal IReadOnlyList<string> GetRegisteredPipelineNames() => _registeredPipelineNames;

    /// <summary>
    /// 타입명에서 파이프라인 표시 이름을 추출합니다.
    /// 제네릭 backtick 제거 및 "Pipeline" 접미사 제거.
    /// </summary>
    private static string ExtractPipelineName(Type pipelineType)
    {
        string name = pipelineType.Name;

        // 제네릭 backtick 제거 (예: "MyPipeline`2" → "MyPipeline")
        int backtickIndex = name.IndexOf('`');
        if (backtickIndex > 0)
            name = name.Substring(0, backtickIndex);

        // "Pipeline" 접미사 제거
        if (name.EndsWith("Pipeline", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Pipeline".Length);

        return name;
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

    /// <summary>
    /// Transaction 파이프라인에 필요한 서비스가 DI에 등록되어 있는지 확인합니다.
    /// IUnitOfWork, IDomainEventPublisher, IDomainEventCollector 모두 등록되어야 합니다.
    /// </summary>
    private static bool HasTransactionDependencies(IServiceCollection services)
    {
        return services.Any(s => s.ServiceType == typeof(IUnitOfWork))
            && services.Any(s => s.ServiceType == typeof(IDomainEventPublisher))
            && services.Any(s => s.ServiceType == typeof(IDomainEventCollector));
    }
}
