namespace DomainLayerRules.Domains;

public abstract class AggregateRoot<TId> : Entity<TId> where TId : struct
{
    protected AggregateRoot() { }
    protected AggregateRoot(TId id) : base(id) { }
}
