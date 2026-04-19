using Functorium.Domains.Repositories;

namespace AiGovernance.Domain.AggregateRoots.Models;

/// <summary>
/// AI 모델 리포지토리 인터페이스 (Command 전용)
/// </summary>
public interface IAIModelRepository : IRepository<AIModel, AIModelId>
{
    /// <summary>
    /// 삭제된 모델을 포함하여 ID로 조회합니다.
    /// </summary>
    FinT<IO, AIModel> GetByIdIncludingDeleted(AIModelId id);
}
