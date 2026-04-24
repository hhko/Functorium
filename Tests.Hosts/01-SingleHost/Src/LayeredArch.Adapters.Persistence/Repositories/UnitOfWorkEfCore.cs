using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using static Functorium.Adapters.Errors.AdapterErrorKind;

namespace LayeredArch.Adapters.Persistence.Repositories;

/// <summary>
/// EF Core UnitOfWork - DbContext.SaveChangesAsync()를 호출하여 변경사항 커밋
/// </summary>
[GenerateObservablePort]
public class UnitOfWorkEfCore : IUnitOfWork
{
    #region Error Types

    public sealed record ConcurrencyConflict : AdapterErrorKind.Custom;
    public sealed record DatabaseUpdateFailed : AdapterErrorKind.Custom;

    #endregion

    private readonly LayeredArchDbContext _dbContext;

    public string RequestCategory => "UnitOfWork";

    public UnitOfWorkEfCore(LayeredArchDbContext dbContext) => _dbContext = dbContext;

    public virtual FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default)
    {
        return IO.liftAsync(async () =>
        {
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                return Fin.Succ(unit);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                return AdapterError.FromException<UnitOfWorkEfCore>(
                    new ConcurrencyConflict(), ex);
            }
            catch (DbUpdateException ex)
            {
                return AdapterError.FromException<UnitOfWorkEfCore>(
                    new DatabaseUpdateFailed(), ex);
            }
        });
    }

    public virtual async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfCoreTransaction(transaction);
    }

    private sealed class EfCoreTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
            => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync()
            => transaction.DisposeAsync();
    }
}
