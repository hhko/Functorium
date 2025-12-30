namespace Cqrs01.Demo;

/// <summary>
/// 사용자 Repository 인터페이스
/// </summary>
public interface IUserRepository
{
    Task<Fin<User>> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<Fin<User?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Fin<Seq<User>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Fin<bool>> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}
