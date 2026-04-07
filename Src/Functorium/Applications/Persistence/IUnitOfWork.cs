using Functorium.Abstractions.Observabilities;

namespace Functorium.Applications.Persistence;

public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);

    /// <summary>
    /// 명시적 트랜잭션을 시작합니다.
    /// ExecuteDeleteAsync/ExecuteUpdateAsync 등 즉시 실행 SQL과 SaveChanges를
    /// 동일 트랜잭션으로 묶어야 할 때 사용합니다.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
