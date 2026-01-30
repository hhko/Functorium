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
        // Provider-strong detection: SQLite error codes
        // - SQLITE_CONSTRAINT (19)
        // - SQLITE_CONSTRAINT_UNIQUE (2067) / SQLITE_CONSTRAINT_PRIMARYKEY (1555)
        // See: https://sqlite.org/rescode.html
        if (ex.InnerException is Microsoft.Data.Sqlite.SqliteException se)
        {
            const int SQLITE_CONSTRAINT = 19;
            const int SQLITE_CONSTRAINT_UNIQUE = 2067;
            const int SQLITE_CONSTRAINT_PRIMARYKEY = 1555;

            return se.SqliteErrorCode == SQLITE_CONSTRAINT
                && (se.SqliteExtendedErrorCode == SQLITE_CONSTRAINT_UNIQUE
                    || se.SqliteExtendedErrorCode == SQLITE_CONSTRAINT_PRIMARYKEY);
        }

        // Fallback (should be rare): message pattern
        var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
        return msg.Contains("unique constraint failed");
    }
}
