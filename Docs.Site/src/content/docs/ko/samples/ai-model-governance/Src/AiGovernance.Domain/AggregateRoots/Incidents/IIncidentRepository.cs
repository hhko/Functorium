using Functorium.Domains.Repositories;

namespace AiGovernance.Domain.AggregateRoots.Incidents;

/// <summary>
/// 인시던트 리포지토리 인터페이스 (Command 전용)
/// </summary>
public interface IIncidentRepository : IRepository<ModelIncident, ModelIncidentId>
{
    /// <summary>
    /// Specification 기반 조회.
    /// </summary>
    FinT<IO, Seq<ModelIncident>> Find(Specification<ModelIncident> spec);
}
