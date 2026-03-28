namespace DomainLayerRules.Domains;

public abstract class Specification<T>
{
    public abstract bool IsSatisfiedBy(T entity);
}
