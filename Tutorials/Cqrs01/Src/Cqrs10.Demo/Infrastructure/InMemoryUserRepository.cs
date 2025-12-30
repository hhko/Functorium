using System.Collections.Concurrent;
using static LanguageExt.Prelude;

namespace Cqrs01.Demo.Infrastructure;

/// <summary>
/// 메모리 기반 사용자 Repository 구현
/// </summary>
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public Task<Fin<User>> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (_users.TryAdd(user.Id, user))
        {
            return Task.FromResult(Fin.Succ(user));
        }

        return Task.FromResult(Fin.Fail<User>(Error.New("Failed to create user")));
    }

    public Task<Fin<User?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _users.TryGetValue(id, out User? user);
        return Task.FromResult(Fin.Succ(user));
    }

    public Task<Fin<Seq<User>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        Seq<User> users = toSeq(_users.Values);
        return Task.FromResult(Fin.Succ(users));
    }

    public Task<Fin<bool>> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        bool exists = _users.Values.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(Fin.Succ(exists));
    }
}
