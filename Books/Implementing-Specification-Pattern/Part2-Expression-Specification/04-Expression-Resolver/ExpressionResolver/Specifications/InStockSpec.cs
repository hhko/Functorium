using Functorium.Domains.Specifications;

namespace ExpressionResolver.Specifications;

/// <summary>
/// Non-expression Specification (fallback 데모용).
/// SpecificationExpressionResolver.TryResolve에서 null을 반환합니다.
/// </summary>
public sealed class InStockSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => entity.Stock > 0;
}
