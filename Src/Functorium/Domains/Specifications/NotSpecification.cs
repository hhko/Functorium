namespace Functorium.Domains.Specifications;

/// <summary>
/// Specification의 NOT 부정.
/// </summary>
public sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public Specification<T> Inner { get; } = inner;
    public override bool IsSatisfiedBy(T entity) => !Inner.IsSatisfiedBy(entity);
}
