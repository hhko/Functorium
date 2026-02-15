namespace Functorium.Domains.Specifications;

/// <summary>
/// 두 Specification의 OR 조합.
/// </summary>
internal sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public override bool IsSatisfiedBy(T entity) => left.IsSatisfiedBy(entity) || right.IsSatisfiedBy(entity);
}
