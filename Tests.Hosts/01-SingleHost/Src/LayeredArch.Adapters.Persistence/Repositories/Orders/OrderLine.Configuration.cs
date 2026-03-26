using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.Orders;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLineModel>
{
    public void Configure(EntityTypeBuilder<OrderLineModel> builder)
    {
        builder.ToTable("OrderLines");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasMaxLength(26);

        builder.Property(l => l.OrderId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(l => l.ProductId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(l => l.UnitPrice)
            .HasPrecision(18, 4);

        builder.Property(l => l.LineTotal)
            .HasPrecision(18, 4);

        builder.HasOne<OrderModel>()
            .WithMany(o => o.OrderLines)
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
