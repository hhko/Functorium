using Cqrs01.Demo.Domain.ValueObjects;

using Functorium.Domains.SourceGenerators;
using Functorium.Domains.Entities;

using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Cqrs01.Demo.Domain;

/// <summary>
/// 사용자 Entity
/// </summary>
[GenerateEntityId]
public sealed class User : Entity<UserId>
{
    public UserName Name { get; private set; } = null!;
    public UserEmail Email { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private User() { }

    private User(UserId id, UserName name, UserEmail email, DateTime createdAt) : base(id)
    {
        Name = name;
        Email = email;
        CreatedAt = createdAt;
    }

    public static Fin<User> Create(UserName name, UserEmail email, DateTime createdAt) =>
        Fin.Succ(new User(UserId.New(), name, email, createdAt));

    public static User CreateFromValidated(UserId id, UserName name, UserEmail email, DateTime createdAt) =>
        new(id, name, email, createdAt);
}
