using Functorium.Applications.Persistence;
using LanguageExt;
using static LanguageExt.Prelude;

namespace UnitOfWork;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly List<Action> _pendingActions = [];
    private bool _saved;

    public string RequestCategory => "UnitOfWork";

    public bool IsSaved => _saved;

    public void AddPendingAction(Action action) => _pendingActions.Add(action);

    public FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
    {
        return IO.lift(() =>
        {
            foreach (var action in _pendingActions)
                action();

            _pendingActions.Clear();
            _saved = true;
            return Fin.Succ(unit);
        });
    }

    public Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IUnitOfWorkTransaction>(new InMemoryTransaction());
    }

    private sealed class InMemoryTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
