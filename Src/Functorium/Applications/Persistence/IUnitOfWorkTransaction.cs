namespace Functorium.Applications.Persistence;

/// <summary>
/// 명시적 트랜잭션 스코프.
/// Dispose 시 미커밋 트랜잭션은 자동 롤백됩니다.
/// </summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
