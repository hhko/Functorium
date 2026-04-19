using Functorium.Domains.Repositories;

namespace AiGovernance.Domain.AggregateRoots.Deployments;

/// <summary>
/// 배포 리포지토리 인터페이스 (Command 전용)
/// </summary>
public interface IDeploymentRepository : IRepository<ModelDeployment, ModelDeploymentId>
{
    /// <summary>
    /// Specification 기반 조회.
    /// </summary>
    FinT<IO, Seq<ModelDeployment>> Find(Specification<ModelDeployment> spec);
}
