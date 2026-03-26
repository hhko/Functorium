using AiGovernance.Adapters.Persistence.Deployments.Repositories;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Specifications;
using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Domain.AggregateRoots.Deployments;

namespace AiGovernance.Adapters.Persistence.Deployments.Queries;

/// <summary>
/// InMemory 기반 배포 읽기 전용 어댑터.
/// DeploymentRepositoryInMemory의 정적 저장소에서 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class DeploymentQueryInMemory
    : InMemoryQueryBase<ModelDeployment, DeploymentListDto>, IDeploymentQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "EndpointUrl";

    protected override IEnumerable<DeploymentListDto> GetProjectedItems(Specification<ModelDeployment> spec)
    {
        return DeploymentRepositoryInMemory.Deployments.Values
            .Where(d => spec.IsSatisfiedBy(d))
            .Select(d => new DeploymentListDto(
                d.Id.ToString(),
                d.ModelId.ToString(),
                d.EndpointUrl,
                d.Status,
                d.Environment));
    }

    protected override Func<DeploymentListDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "EndpointUrl" => d => d.EndpointUrl,
        "Status" => d => d.Status,
        "Environment" => d => d.Environment,
        _ => d => d.EndpointUrl
    };
}
