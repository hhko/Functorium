using AiGovernance.Adapters.Persistence.Models.Repositories;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Domains.Specifications;
using AiGovernance.Application.Usecases.Models.Ports;
using AiGovernance.Domain.AggregateRoots.Models;

namespace AiGovernance.Adapters.Persistence.Models.Queries;

/// <summary>
/// InMemory 기반 AI 모델 읽기 전용 어댑터.
/// AIModelRepositoryInMemory의 정적 저장소에서 데이터를 가져온 후 정렬/페이지네이션/DTO 변환합니다.
/// </summary>
[GenerateObservablePort]
public class AIModelQueryInMemory
    : InMemoryQueryBase<AIModel, ModelListDto>, IAIModelQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string DefaultSortField => "Name";

    protected override IEnumerable<ModelListDto> GetProjectedItems(Specification<AIModel> spec)
    {
        return AIModelRepositoryInMemory.AIModels.Values
            .Where(m => m.DeletedAt.IsNone && spec.IsSatisfiedBy(m))
            .Select(m => new ModelListDto(
                m.Id.ToString(),
                m.Name,
                m.Version,
                m.RiskTier));
    }

    protected override Func<ModelListDto, object> SortSelector(string fieldName) => fieldName switch
    {
        "Name" => m => m.Name,
        "Version" => m => m.Version,
        "RiskTier" => m => m.RiskTier,
        _ => m => m.Name
    };
}
