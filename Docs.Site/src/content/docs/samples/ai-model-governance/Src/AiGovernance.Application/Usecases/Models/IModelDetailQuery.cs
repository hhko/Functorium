using Functorium.Applications.Queries;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Application.Usecases.Models.Ports;

/// <summary>
/// AI 모델 단건 조회용 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IModelDetailQuery : IQueryPort
{
    FinT<IO, ModelDetailDto> GetById(AIModelId id);
}

public sealed record ModelDetailDto(
    string Id,
    string Name,
    string Version,
    string Purpose,
    string RiskTier,
    DateTimeOffset CreatedAt);
