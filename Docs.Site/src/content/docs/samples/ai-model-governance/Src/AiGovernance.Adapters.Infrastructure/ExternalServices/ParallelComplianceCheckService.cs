using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Abstractions.Observabilities;
using AiGovernance.Domain.AggregateRoots.Deployments;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace AiGovernance.Adapters.Infrastructure.ExternalServices;

/// <summary>
/// 병렬 컴플라이언스 체크 결과 레코드
/// </summary>
public sealed record ComplianceCriterionCheckResult(
    string CriterionName,
    bool Passed,
    string Details,
    DateTimeOffset CheckedAt);

/// <summary>
/// 병렬 컴플라이언스 체크 결과 집합 레코드
/// </summary>
public sealed record ComplianceCheckReport(
    ModelDeploymentId DeploymentId,
    Seq<ComplianceCriterionCheckResult> Results,
    bool AllPassed,
    DateTimeOffset ReportedAt);

/// <summary>
/// 병렬 컴플라이언스 체크 외부 서비스 Port.
/// Infrastructure Adapter에서 구현합니다.
/// </summary>
public interface IParallelComplianceCheckService : IObservablePort
{
    FinT<IO, ComplianceCheckReport> RunComplianceChecks(ModelDeploymentId deploymentId);
}

/// <summary>
/// 병렬 컴플라이언스 체크 외부 서비스 구현.
/// IO.Fork + awaitAll 패턴을 보여줍니다:
///   각 기준별 체크를 Fork → awaitAll로 병렬 수집 → 결과 집계
/// </summary>
[GenerateObservablePort]
public class ParallelComplianceCheckService : IParallelComplianceCheckService
{
    #region Error Types

    public sealed record ComplianceCheckFailed : AdapterErrorType.Custom;

    #endregion

    private static readonly Random _random = new();

    /// <summary>
    /// 관찰 가능성 로그를 위한 요청 카테고리
    /// </summary>
    public string RequestCategory => "ExternalService";

    /// <summary>
    /// 병렬로 실행할 컴플라이언스 체크 기준 목록
    /// </summary>
    private static readonly Seq<string> CriterionNames = Seq(
        "DataGovernance",
        "SecurityReview",
        "BiasAssessment",
        "TransparencyAudit",
        "HumanOversight");

    public virtual FinT<IO, ComplianceCheckReport> RunComplianceChecks(ModelDeploymentId deploymentId)
    {
        // 각 기준별 IO 체크를 생성하고 Fork로 병렬 실행
        var forks = CriterionNames.Map(name => CheckSingleCriterion(name).Fork());

        // awaitAll(Seq<IO<ForkIO<A>>>) 오버로드로 모든 Fork 결과를 수집
        var io = awaitAll(forks)
            .Map(results =>
            {
                var allPassed = results.ForAll(r => r.Passed);
                return new ComplianceCheckReport(
                    DeploymentId: deploymentId,
                    Results: results,
                    AllPassed: allPassed,
                    ReportedAt: DateTimeOffset.UtcNow);
            })
            .Catch(
                e => e.IsExceptional,
                e => IO.fail<ComplianceCheckReport>(
                    AdapterError.FromException<ParallelComplianceCheckService>(
                        new ComplianceCheckFailed(),
                        e.ToException())));

        return new FinT<IO, ComplianceCheckReport>(io.Map(Fin.Succ));
    }

    /// <summary>
    /// 개별 기준 체크를 수행하는 IO 연산.
    /// 네트워크 지연과 간헐적 실패를 시뮬레이션합니다.
    /// </summary>
    private static IO<ComplianceCriterionCheckResult> CheckSingleCriterion(string criterionName)
    {
        return IO.liftAsync<ComplianceCriterionCheckResult>(async env =>
        {
            // 각 기준별 독립적인 네트워크 지연 시뮬레이션 (100~500ms)
            await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(100, 500)), env.Token);

            // 간헐적 실패 시뮬레이션 (3% 확률)
            if (_random.Next(100) < 3)
                throw new InvalidOperationException(
                    $"Compliance check service unavailable for criterion: {criterionName}");

            var passed = _random.Next(100) < 90; // 90% 확률로 통과
            return new ComplianceCriterionCheckResult(
                CriterionName: criterionName,
                Passed: passed,
                Details: passed
                    ? $"{criterionName}: All requirements met"
                    : $"{criterionName}: Remediation required",
                CheckedAt: DateTimeOffset.UtcNow);
        });
    }
}
