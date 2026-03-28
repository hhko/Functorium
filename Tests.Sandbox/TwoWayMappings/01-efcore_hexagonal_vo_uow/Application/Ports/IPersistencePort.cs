using MyApp.Domain;

namespace MyApp.Application.Ports;

public interface IPersistencePort
{
    Task InsertAsync(User user, CancellationToken ct = default);
    Task<User?> FindByEmailAsync(Email email, CancellationToken ct = default);
}
