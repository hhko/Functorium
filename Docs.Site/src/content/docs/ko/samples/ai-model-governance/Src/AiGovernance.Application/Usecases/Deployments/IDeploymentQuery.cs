using Functorium.Applications.Queries;
using AiGovernance.Domain.AggregateRoots.Deployments;

namespace AiGovernance.Application.Usecases.Deployments.Ports;

/// <summary>
/// 배포 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IDeploymentQuery : IQueryPort<ModelDeployment, DeploymentListDto> { }

public sealed record DeploymentListDto(
    string Id,
    string ModelId,
    string EndpointUrl,
    string Status,
    string Environment);
