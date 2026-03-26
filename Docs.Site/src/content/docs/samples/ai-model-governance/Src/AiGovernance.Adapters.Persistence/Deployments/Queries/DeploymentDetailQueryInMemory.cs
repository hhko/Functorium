using AiGovernance.Adapters.Persistence.Deployments.Repositories;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using AiGovernance.Application.Usecases.Deployments.Ports;
using AiGovernance.Domain.AggregateRoots.Deployments;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace AiGovernance.Adapters.Persistence.Deployments.Queries;

/// <summary>
/// InMemory 기반 배포 단건 조회 읽기 전용 어댑터.
/// DeploymentRepositoryInMemory의 정적 저장소에서 데이터를 가져온 후 DTO로 프로젝션합니다.
/// </summary>
[GenerateObservablePort]
public class DeploymentDetailQueryInMemory : IDeploymentDetailQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, DeploymentDetailDto> GetById(ModelDeploymentId id)
    {
        return IO.lift(() =>
        {
            if (DeploymentRepositoryInMemory.Deployments.TryGetValue(id, out var deployment))
            {
                return Fin.Succ(new DeploymentDetailDto(
                    deployment.Id.ToString(),
                    deployment.ModelId.ToString(),
                    deployment.EndpointUrl,
                    deployment.Status,
                    deployment.Environment,
                    deployment.DriftThreshold));
            }

            return AdapterError.For<DeploymentDetailQueryInMemory>(
                new NotFound(),
                id.ToString(),
                $"배포 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }
}
