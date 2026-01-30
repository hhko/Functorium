using Microsoft.EntityFrameworkCore;

namespace MyApp.Adapters.Database;

public sealed class AppDbContext : DbContext
{
    public DbSet<UserJpaEntity> Users => Set<UserJpaEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var user = modelBuilder.Entity<UserJpaEntity>();

        user.ToTable("Users");
        user.HasKey(x => x.Id);

        user.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(320);

        user.Property(x => x.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(320);

        user.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        // Unique index for stable duplicate detection
        user.HasIndex(x => x.NormalizedEmail).IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
