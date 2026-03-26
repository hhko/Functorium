using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

namespace AiGovernance.Domain.AggregateRoots.Models.Specifications;

/// <summary>
/// 모델 위험 등급 검색 Specification.
/// </summary>
public sealed class ModelRiskTierSpec : ExpressionSpecification<AIModel>
{
    public RiskTier Tier { get; }

    public ModelRiskTierSpec(RiskTier tier) => Tier = tier;

    public override Expression<Func<AIModel, bool>> ToExpression()
    {
        string tierStr = Tier;
        return model => (string)model.RiskTier == tierStr;
    }
}
