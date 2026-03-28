namespace MyApp.Application.Ports;

public interface IUnitOfWorkPort
{
    Task<IUnitOfWorkTransaction> BeginAsync(CancellationToken ct = default);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
