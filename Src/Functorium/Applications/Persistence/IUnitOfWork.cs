using Functorium.Domains.Observabilities;

namespace Functorium.Applications.Persistence;

public interface IUnitOfWork : IPort
{
    FinT<IO, Unit> SaveChanges(CancellationToken cancellationToken = default);
}
