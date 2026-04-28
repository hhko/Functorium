using AiGovernance.Adapters.Persistence.Models.Repositories;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using AiGovernance.Application.Usecases.Models.Ports;
using AiGovernance.Domain.AggregateRoots.Models;
using static Functorium.Adapters.Errors.AdapterErrorKind;

namespace AiGovernance.Adapters.Persistence.Models.Queries;

/// <summary>
/// InMemory 기반 AI 모델 단건 조회 읽기 전용 어댑터.
/// AIModelRepositoryInMemory의 정적 저장소에서 데이터를 가져온 후 DTO로 프로젝션합니다.
/// </summary>
[GenerateObservablePort]
public class AIModelDetailQueryInMemory : IModelDetailQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, ModelDetailDto> GetById(AIModelId id)
    {
        return IO.lift(() =>
        {
            if (AIModelRepositoryInMemory.AIModels.TryGetValue(id, out var model) && model.DeletedAt.IsNone)
            {
                return Fin.Succ(new ModelDetailDto(
                    model.Id.ToString(),
                    model.Name,
                    model.Version,
                    model.Purpose,
                    model.RiskTier,
                    model.CreatedAt));
            }

            return AdapterError.For<AIModelDetailQueryInMemory>(
                new NotFound(),
                id.ToString(),
                $"AI 모델 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }
}
