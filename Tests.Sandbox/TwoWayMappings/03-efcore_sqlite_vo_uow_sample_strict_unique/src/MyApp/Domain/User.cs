namespace MyApp.Domain;

public sealed class User
{
    public Guid Id { get; }
    public Email Email { get; }
    public string DisplayName { get; }

    public User(Guid id, Email email, string displayName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Id must not be empty.", nameof(id));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("DisplayName is required.", nameof(displayName));

        Id = id;
        DisplayName = displayName.Trim();
    }
}
