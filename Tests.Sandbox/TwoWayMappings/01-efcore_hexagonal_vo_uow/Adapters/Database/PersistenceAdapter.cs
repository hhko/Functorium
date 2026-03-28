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
        var entity = ToEntity(user);

        await _db.Users.AddAsync(entity, ct);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Map infrastructure exception -> application exception
            throw new UserAlreadyExistsException("User already exists.", ex);
        }
    }

    public async Task<User?> FindByEmailAsync(Email email, CancellationToken ct = default)
    {
        var normalized = email.Normalized;

        var entity = await _db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.NormalizedEmail == normalized, ct);

        return entity is null ? null : ToDomain(entity);
    }

    // --- Two-Way Mapping handled by PersistenceAdapter ---

    private static UserJpaEntity ToEntity(User user) => new()
    {
        Id = user.Id,
        Email = user.Email.Value,
        NormalizedEmail = user.Email.Normalized,
        DisplayName = user.DisplayName
    };

    private static User ToDomain(UserJpaEntity entity)
        => new(entity.Id, Email.Create(entity.Email), entity.DisplayName);

    /// <summary>
    /// Best-effort unique constraint detection across providers.
    /// Prefer provider-specific checks in real projects.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var msg = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();

        // Common patterns:
        // - SQL Server: "Cannot insert duplicate key row" / error numbers 2601, 2627 (not accessible here without provider types)
        // - PostgreSQL: "duplicate key value violates unique constraint"
        // - SQLite: "UNIQUE constraint failed"
        return msg.Contains("duplicate") ||
               msg.Contains("unique constraint") ||
               msg.Contains("unique index") ||
               msg.Contains("unique constraint failed") ||
               msg.Contains("cannot insert duplicate key");
    }
}
