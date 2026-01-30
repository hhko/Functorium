using Cqrs01.Demo.Domain.ValueObjects;

using LanguageExt;

namespace Cqrs01.Demo.Domain;

/// <summary>
/// 사용자 Repository 인터페이스
/// </summary>
public interface IUserRepository
{
    Task<Fin<User>> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<Fin<User?>> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
    Task<Fin<Seq<User>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Fin<bool>> ExistsByEmailAsync(UserEmail email, CancellationToken cancellationToken = default);
}
