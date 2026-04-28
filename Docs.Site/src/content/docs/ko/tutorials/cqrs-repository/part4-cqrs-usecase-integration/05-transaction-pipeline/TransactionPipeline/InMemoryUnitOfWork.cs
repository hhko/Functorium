using Functorium.Applications.Persistence;
using LanguageExt;

namespace TransactionPipeline;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private bool _saved;
    public bool WasSaved => _saved;

    public string RequestCategory => "UnitOfWork";

    public FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
    {
        return IO.lift(() =>
        {
            _saved = true;
            return Fin.Succ(Unit.Default);
        });
    }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IUnitOfWorkTransaction>(new InMemoryTransaction());
    }

    public void Reset() => _saved = false;
}

public sealed class InMemoryTransaction : IUnitOfWorkTransaction
{
    public bool WasCommitted { get; private set; }
    public bool WasDisposed { get; private set; }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        WasCommitted = true;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        WasDisposed = true;
        return ValueTask.CompletedTask;
    }
}
