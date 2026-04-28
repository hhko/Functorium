namespace InheritanceAndInterface.Domains;

public interface IAuditable
{
    DateTime CreatedAt { get; }
    DateTime? ModifiedAt { get; }
}
