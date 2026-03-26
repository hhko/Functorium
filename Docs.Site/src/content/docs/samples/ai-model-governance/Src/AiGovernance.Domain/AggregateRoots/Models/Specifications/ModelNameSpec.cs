using System.Linq.Expressions;
using Functorium.Domains.Specifications;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;

namespace AiGovernance.Domain.AggregateRoots.Models.Specifications;

/// <summary>
/// 모델명 검색 Specification.
/// </summary>
public sealed class ModelNameSpec : ExpressionSpecification<AIModel>
{
    public ModelName Name { get; }

    public ModelNameSpec(ModelName name) => Name = name;

    public override Expression<Func<AIModel, bool>> ToExpression()
    {
        string nameStr = Name;
        return model => (string)model.Name == nameStr;
    }
}
