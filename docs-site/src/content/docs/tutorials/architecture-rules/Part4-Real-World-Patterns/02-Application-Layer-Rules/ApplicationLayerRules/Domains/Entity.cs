namespace ApplicationLayerRules.Domains;

public abstract class Entity<TId> where TId : struct
{
    public TId Id { get; protected set; }
}
