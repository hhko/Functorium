using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Persistence;

namespace LayeredArch.Adapters.Persistence.Repositories;

/// <summary>
/// InMemory UnitOfWork - ConcurrentDictionary 기반이므로 SaveChanges 불필요 (no-op)
/// </summary>
[GenerateObservablePort]
public class UnitOfWorkInMemory : IUnitOfWork
{
    public string RequestCategory => "UnitOfWork";

    public virtual FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
    {
        return IO.lift(() => Fin.Succ(unit));
    }

    public virtual Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult<IUnitOfWorkTransaction>(NoOpTransaction.Instance);

    private sealed class NoOpTransaction : IUnitOfWorkTransaction
    {
        public static readonly NoOpTransaction Instance = new();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
