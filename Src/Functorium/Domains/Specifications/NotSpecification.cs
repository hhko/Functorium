namespace Functorium.Domains.Specifications;

/// <summary>
/// Specification의 NOT 부정.
/// </summary>
internal sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public override bool IsSatisfiedBy(T entity) => !inner.IsSatisfiedBy(entity);
}
