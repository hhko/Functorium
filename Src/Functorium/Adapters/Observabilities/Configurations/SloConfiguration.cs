using FluentValidation;

namespace Functorium.Adapters.Observabilities.Configurations;

/// <summary>
/// SLO (Service Level Objective) 설정을 정의합니다.
/// 코드 기반 기본값 + appsettings.json 환경별 오버라이드를 지원합니다.
///
/// 설정 우선순위:
/// 1. HandlerOverrides (핸들러별 맞춤 설정)
/// 2. CqrsDefaults (Command/Query 기본값)
/// 3. GlobalDefaults (전역 기본값)
/// </summary>
public sealed class SloConfiguration
{
    public const string SectionName = "Observability:Slo";

    /// <summary>
    /// 전역 기본 SLO 목표값
    /// </summary>
    public SloTargets GlobalDefaults { get; set; } = new();

    /// <summary>
    /// CQRS 타입별 기본 SLO 목표값
    /// </summary>
    public CqrsSloDefaults CqrsDefaults { get; set; } = new();

    /// <summary>
    /// 핸들러별 맞춤 SLO 설정 (예: "CreateOrderCommand" -> SloTargets)
    /// </summary>
    public Dictionary<string, SloTargets> HandlerOverrides { get; set; } = new();

    /// <summary>
    /// Histogram 버킷 경계값 (초 단위)
    /// SLO 임계값과 정렬된 커스텀 버킷으로 정확한 백분위수 계산
    /// </summary>
    public double[] HistogramBuckets { get; set; } = DefaultHistogramBuckets;

    /// <summary>
    /// 기본 Histogram 버킷 (초 단위)
    /// 중요 범위(1ms-1s)에 밀집된 버킷 배치, Long-tail 시나리오(최대 10s) 포함
    /// </summary>
    public static readonly double[] DefaultHistogramBuckets =
    [
        0.001,  // 1ms
        0.005,  // 5ms
        0.01,   // 10ms
        0.025,  // 25ms
        0.05,   // 50ms
        0.1,    // 100ms
        0.25,   // 250ms
        0.5,    // 500ms (Command SLO P95)
        1,      // 1s (Command SLO P99)
        2.5,    // 2.5s
        5,      // 5s
        10      // 10s
    ];

    /// <summary>
    /// 특정 핸들러의 SLO 목표값을 반환합니다.
    /// 우선순위: HandlerOverrides > CqrsDefaults > GlobalDefaults
    /// </summary>
    /// <param name="handlerName">핸들러 이름 (예: "CreateOrderCommand")</param>
    /// <param name="cqrsType">CQRS 타입 ("command" 또는 "query")</param>
    public SloTargets GetTargetsForHandler(string handlerName, string cqrsType)
    {
        // 1순위: 핸들러별 오버라이드
        if (HandlerOverrides.TryGetValue(handlerName, out var handlerTargets))
        {
            return MergeWithDefaults(handlerTargets, cqrsType);
        }

        // 2순위: CQRS 타입별 기본값
        return GetCqrsDefaults(cqrsType);
    }

    /// <summary>
    /// CQRS 타입에 따른 기본 SLO 목표값을 반환합니다.
    /// </summary>
    internal SloTargets GetCqrsDefaults(string cqrsType)
    {
        return cqrsType.ToLowerInvariant() switch
        {
            "command" => MergeWithGlobalDefaults(CqrsDefaults.Command),
            "query" => MergeWithGlobalDefaults(CqrsDefaults.Query),
            _ => GlobalDefaults
        };
    }

    /// <summary>
    /// 핸들러 오버라이드를 CQRS 기본값과 병합합니다.
    /// </summary>
    private SloTargets MergeWithDefaults(SloTargets handlerTargets, string cqrsType)
    {
        var cqrsDefaults = GetCqrsDefaults(cqrsType);
        return new SloTargets
        {
            AvailabilityPercent = handlerTargets.AvailabilityPercent ?? cqrsDefaults.AvailabilityPercent,
            LatencyP95Milliseconds = handlerTargets.LatencyP95Milliseconds ?? cqrsDefaults.LatencyP95Milliseconds,
            LatencyP99Milliseconds = handlerTargets.LatencyP99Milliseconds ?? cqrsDefaults.LatencyP99Milliseconds,
            ErrorBudgetWindowDays = handlerTargets.ErrorBudgetWindowDays ?? cqrsDefaults.ErrorBudgetWindowDays
        };
    }

    /// <summary>
    /// CQRS 기본값을 전역 기본값과 병합합니다.
    /// </summary>
    internal SloTargets MergeWithGlobalDefaults(SloTargets cqrsTargets)
    {
        return new SloTargets
        {
            AvailabilityPercent = cqrsTargets.AvailabilityPercent ?? GlobalDefaults.AvailabilityPercent,
            LatencyP95Milliseconds = cqrsTargets.LatencyP95Milliseconds ?? GlobalDefaults.LatencyP95Milliseconds,
            LatencyP99Milliseconds = cqrsTargets.LatencyP99Milliseconds ?? GlobalDefaults.LatencyP99Milliseconds,
            ErrorBudgetWindowDays = cqrsTargets.ErrorBudgetWindowDays ?? GlobalDefaults.ErrorBudgetWindowDays
        };
    }

    /// <summary>
    /// SloConfiguration 유효성 검사기
    /// </summary>
    public sealed class Validator : AbstractValidator<SloConfiguration>
    {
        public Validator()
        {
            // GlobalDefaults 검증
            RuleFor(x => x.GlobalDefaults)
                .NotNull()
                .WithMessage($"{nameof(GlobalDefaults)} is required.");

            RuleFor(x => x.GlobalDefaults.AvailabilityPercent)
                .InclusiveBetween(0.0, 100.0)
                .When(x => x.GlobalDefaults?.AvailabilityPercent.HasValue == true)
                .WithMessage($"{nameof(GlobalDefaults)}.{nameof(SloTargets.AvailabilityPercent)} must be between 0 and 100.");

            RuleFor(x => x.GlobalDefaults.LatencyP95Milliseconds)
                .GreaterThan(0)
                .When(x => x.GlobalDefaults?.LatencyP95Milliseconds.HasValue == true)
                .WithMessage($"{nameof(GlobalDefaults)}.{nameof(SloTargets.LatencyP95Milliseconds)} must be greater than 0.");

            RuleFor(x => x.GlobalDefaults.LatencyP99Milliseconds)
                .GreaterThan(0)
                .When(x => x.GlobalDefaults?.LatencyP99Milliseconds.HasValue == true)
                .WithMessage($"{nameof(GlobalDefaults)}.{nameof(SloTargets.LatencyP99Milliseconds)} must be greater than 0.");

            // P99 >= P95 검증
            RuleFor(x => x.GlobalDefaults)
                .Must(targets => !targets.LatencyP99Milliseconds.HasValue ||
                                 !targets.LatencyP95Milliseconds.HasValue ||
                                 targets.LatencyP99Milliseconds >= targets.LatencyP95Milliseconds)
                .WithMessage($"{nameof(GlobalDefaults)}.{nameof(SloTargets.LatencyP99Milliseconds)} must be greater than or equal to {nameof(SloTargets.LatencyP95Milliseconds)}.");

            // HistogramBuckets 검증
            RuleFor(x => x.HistogramBuckets)
                .NotEmpty()
                .WithMessage($"{nameof(HistogramBuckets)} must contain at least one bucket.");

            RuleFor(x => x.HistogramBuckets)
                .Must(buckets => buckets.All(b => b > 0))
                .WithMessage($"{nameof(HistogramBuckets)} values must be positive.");

            RuleFor(x => x.HistogramBuckets)
                .Must(buckets => buckets.SequenceEqual(buckets.OrderBy(b => b)))
                .WithMessage($"{nameof(HistogramBuckets)} must be sorted in ascending order.");
        }
    }
}

/// <summary>
/// SLO 목표값을 정의합니다.
/// null 값은 상위 계층에서 값을 상속받음을 의미합니다.
/// </summary>
public sealed class SloTargets
{
    /// <summary>
    /// 가용성 목표 (%)
    /// 예: 99.9 = 한 달에 43.2분 다운타임 허용
    /// </summary>
    public double? AvailabilityPercent { get; set; } = 99.9;

    /// <summary>
    /// P95 지연시간 목표 (밀리초)
    /// 95%의 요청이 이 시간 이내에 완료되어야 함
    /// </summary>
    public double? LatencyP95Milliseconds { get; set; } = 500;

    /// <summary>
    /// P99 지연시간 목표 (밀리초)
    /// 99%의 요청이 이 시간 이내에 완료되어야 함
    /// </summary>
    public double? LatencyP99Milliseconds { get; set; } = 1000;

    /// <summary>
    /// 에러 버짓 윈도우 (일)
    /// 에러 버짓 계산 기간
    /// </summary>
    public int? ErrorBudgetWindowDays { get; set; } = 30;

    /// <summary>
    /// 에러 버짓 계산: 허용 가능한 실패율 (%)
    /// 예: 99.9% 가용성 → 0.1% 에러 허용
    /// </summary>
    public double GetErrorBudgetPercent()
    {
        return 100.0 - (AvailabilityPercent ?? 99.9);
    }
}

/// <summary>
/// CQRS 타입별 기본 SLO 설정
/// </summary>
public sealed class CqrsSloDefaults
{
    /// <summary>
    /// Command (쓰기 작업) 기본 SLO
    /// - 높은 신뢰성 필요 (99.9%)
    /// - 상대적으로 긴 응답시간 허용 (500ms)
    /// </summary>
    public SloTargets Command { get; set; } = new()
    {
        AvailabilityPercent = 99.9,
        LatencyP95Milliseconds = 500,
        LatencyP99Milliseconds = 1000,
        ErrorBudgetWindowDays = 30
    };

    /// <summary>
    /// Query (읽기 작업) 기본 SLO
    /// - 재시도 가능하므로 상대적으로 낮은 가용성 (99.5%)
    /// - 빠른 응답시간 필요 (200ms)
    /// </summary>
    public SloTargets Query { get; set; } = new()
    {
        AvailabilityPercent = 99.5,
        LatencyP95Milliseconds = 200,
        LatencyP99Milliseconds = 500,
        ErrorBudgetWindowDays = 30
    };
}
