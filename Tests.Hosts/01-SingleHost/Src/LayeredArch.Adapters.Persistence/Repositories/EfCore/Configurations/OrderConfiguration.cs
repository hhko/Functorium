using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<OrderModel>
{
    public void Configure(EntityTypeBuilder<OrderModel> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasMaxLength(26);

        builder.Property(o => o.CustomerId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 4);

        builder.Property(o => o.ShippingAddress)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.CreatedAt);
        builder.Property(o => o.UpdatedAt);

        builder.HasMany(o => o.OrderLines)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
