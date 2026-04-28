namespace VisibilityAndModifiers.Domains;

public abstract class DomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
