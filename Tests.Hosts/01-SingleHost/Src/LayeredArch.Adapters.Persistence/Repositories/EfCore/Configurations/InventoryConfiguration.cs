using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Configurations;

public class InventoryConfiguration : IEntityTypeConfiguration<InventoryModel>
{
    public void Configure(EntityTypeBuilder<InventoryModel> builder)
    {
        builder.ToTable("Inventories");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasMaxLength(26);

        builder.Property(i => i.ProductId)
            .HasMaxLength(26)
            .IsRequired();

        builder.HasIndex(i => i.ProductId)
            .IsUnique();

        builder.Property(i => i.RowVersion)
            .IsRowVersion();

        builder.Property(i => i.CreatedAt);
        builder.Property(i => i.UpdatedAt);
    }
}
