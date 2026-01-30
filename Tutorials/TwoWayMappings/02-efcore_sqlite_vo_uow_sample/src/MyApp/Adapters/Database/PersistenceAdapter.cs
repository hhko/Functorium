using Microsoft.EntityFrameworkCore;
using MyApp.Application;
using MyApp.Application.Ports;
using MyApp.Domain;

namespace MyApp.Adapters.Database;

public sealed class PersistenceAdapter : IPersistencePort
{
    private readonly AppDbContext _db;

    public PersistenceAdapter(AppDbContext db) => _db = db;

    public async Task InsertAsync(User user, CancellationToken ct = default)
    {
        // Insert-only: no pre-check. Rely on UNIQUE constraint of NormalizedEmail.
        var entity = ToEntity(user);
        await _db.Users.AddAsync(entity, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsSqliteUniqueViolation(ex))
        {
            throw new UserAlreadyExistsException("User already exists.", ex);
        }
    }

    public async Task<User?> FindByEmailAsync(Email email, CancellationToken ct = default)
    {
        var entity = await _db.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == email.Normalized, ct);

        return entity is null ? null : ToDomain(entity);
    }

    private static UserJpaEntity ToEntity(User user) => new()
    {
        Id = user.Id,
        Email = user.Email.Value,
        NormalizedEmail = user.Email.Normalized,
        DisplayName = user.DisplayName
    };

    private static User ToDomain(UserJpaEntity entity)
        => new(entity.Id, Email.Create(entity.Email), entity.DisplayName);

    private static bool IsSqliteUniqueViolation(DbUpdateException ex)
    {
        // SQLite typically reports: "SQLite Error 19: 'UNIQUE constraint failed: Users.NormalizedEmail'."
        var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
        return msg.Contains("sqlite error 19") || msg.Contains("unique constraint failed");
    }
}
