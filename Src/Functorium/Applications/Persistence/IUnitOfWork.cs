using Functorium.Applications.Observabilities;

namespace Functorium.Applications.Persistence;

public interface IUnitOfWork : IAdapter
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
}
