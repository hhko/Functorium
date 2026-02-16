namespace Functorium.Domains.Specifications;

/// <summary>
/// 두 Specification의 AND 조합.
/// </summary>
public sealed class AndSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public Specification<T> Left { get; } = left;
    public Specification<T> Right { get; } = right;
    public override bool IsSatisfiedBy(T entity) => Left.IsSatisfiedBy(entity) && Right.IsSatisfiedBy(entity);
}
