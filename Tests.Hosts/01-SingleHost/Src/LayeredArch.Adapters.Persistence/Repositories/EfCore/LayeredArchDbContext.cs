using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using Microsoft.EntityFrameworkCore;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore;

public class LayeredArchDbContext : DbContext
{
    public DbSet<ProductModel> Products => Set<ProductModel>();
    public DbSet<InventoryModel> Inventories => Set<InventoryModel>();
    public DbSet<OrderModel> Orders => Set<OrderModel>();
    public DbSet<OrderLineModel> OrderLines => Set<OrderLineModel>();
    public DbSet<CustomerModel> Customers => Set<CustomerModel>();
    public DbSet<TagModel> Tags => Set<TagModel>();
    public DbSet<ProductTagModel> ProductTags => Set<ProductTagModel>();

    public LayeredArchDbContext(DbContextOptions<LayeredArchDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LayeredArchDbContext).Assembly);
    }
}
