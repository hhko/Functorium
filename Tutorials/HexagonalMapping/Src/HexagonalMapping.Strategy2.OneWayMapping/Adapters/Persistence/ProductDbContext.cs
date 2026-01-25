using Microsoft.EntityFrameworkCore;

namespace HexagonalMapping.Strategy2.OneWayMapping.Adapters.Persistence;

public class ProductDbContext : DbContext
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
        });
    }
}
