using LayeredArch.Adapters.Persistence.Repositories.Customers;
using LayeredArch.Adapters.Persistence.Repositories.Inventories;
using LayeredArch.Adapters.Persistence.Repositories.Orders;
using LayeredArch.Adapters.Persistence.Repositories.Products;
using LayeredArch.Adapters.Persistence.Repositories.Tags;
using Microsoft.EntityFrameworkCore;

namespace LayeredArch.Adapters.Persistence.Repositories;

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
