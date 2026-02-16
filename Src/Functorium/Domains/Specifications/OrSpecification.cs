namespace Functorium.Domains.Specifications;

/// <summary>
/// 두 Specification의 OR 조합.
/// </summary>
public sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
{
    public Specification<T> Left { get; } = left;
    public Specification<T> Right { get; } = right;
    public override bool IsSatisfiedBy(T entity) => Left.IsSatisfiedBy(entity) || Right.IsSatisfiedBy(entity);
}
