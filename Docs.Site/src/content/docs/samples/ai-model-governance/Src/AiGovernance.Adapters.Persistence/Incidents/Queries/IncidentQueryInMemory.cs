using AiGovernance.Adapters.Persistence.Incidents.Repositories;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Specifications;
using AiGovernance.Application.Usecases.Incidents.Ports;
using AiGovernance.Domain.AggregateRoots.Incidents;

namespace AiGovernance.Adapters.Persistence.Incidents.Queries;

/// <summary>
/// InMemory 기반 인시던트 읽기 전용 어댑터.
/// IncidentRepositoryInMemory의 정적 저장소에서 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class IncidentQueryInMemory
    : InMemoryQueryBase<ModelIncident, IncidentListDto>, IIncidentQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "ReportedAt";

    protected override IEnumerable<IncidentListDto> GetProjectedItems(Specification<ModelIncident> spec)
    {
        return IncidentRepositoryInMemory.Incidents.Values
            .Where(i => spec.IsSatisfiedBy(i))
            .Select(i => new IncidentListDto(
                i.Id.ToString(),
                i.DeploymentId.ToString(),
                i.Severity,
                i.Status,
                i.ReportedAt));
    }

    protected override Func<IncidentListDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Severity" => i => i.Severity,
        "Status" => i => i.Status,
        "ReportedAt" => i => i.ReportedAt,
        _ => i => i.ReportedAt
    };
}
