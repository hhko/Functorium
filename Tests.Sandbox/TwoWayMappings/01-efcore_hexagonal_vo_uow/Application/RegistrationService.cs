using MyApp.Application.Ports;
using MyApp.Domain;

namespace MyApp.Application;

public sealed class RegistrationService
{
    private readonly IPersistencePort _persistence;
    private readonly IUnitOfWorkPort _uow;

    public RegistrationService(IPersistencePort persistence, IUnitOfWorkPort uow)
    {
        _persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<User> RegisterAsync(string email, string displayName, CancellationToken ct = default)
    {
        var emailVo = Email.Create(email);
        var user = new User(Guid.NewGuid(), emailVo, displayName);

        await using var tx = await _uow.BeginAsync(ct);

        try
        {
            // Insert-only. Duplicate is handled by DB unique constraint + exception mapping.
            await _persistence.InsertAsync(user, ct);

            await tx.CommitAsync(ct);
            return user;
        }
        catch (UserAlreadyExistsException)
        {
            await tx.RollbackAsync(ct);
            throw;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
