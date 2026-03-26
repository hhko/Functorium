using Functorium.Applications.Queries;
using AiGovernance.Domain.AggregateRoots.Incidents;

namespace AiGovernance.Application.Usecases.Incidents.Ports;

/// <summary>
/// 인시던트 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IIncidentQuery : IQueryPort<ModelIncident, IncidentListDto> { }

public sealed record IncidentListDto(
    string Id,
    string DeploymentId,
    string Severity,
    string Status,
    DateTimeOffset ReportedAt);
