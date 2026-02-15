namespace Functorium.Domains.Specifications;

/// <summary>
/// 두 Specification의 AND 조합.
/// </summary>
internal sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T entity) => left.IsSatisfiedBy(entity) && right.IsSatisfiedBy(entity);
}
