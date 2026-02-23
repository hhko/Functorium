using Functorium.Domains.Observabilities;

namespace Functorium.Applications.Persistence;

public interface IUnitOfWork : IObservablePort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
}
