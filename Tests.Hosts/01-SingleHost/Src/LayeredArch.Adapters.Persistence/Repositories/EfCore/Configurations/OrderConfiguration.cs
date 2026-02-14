using LayeredArch.Domain.AggregateRoots.Orders;
using LayeredArch.Domain.AggregateRoots.Orders.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(new OrderIdConverter())
            .HasMaxLength(26);
        builder.Property(o => o.Id)
            .Metadata.SetValueComparer(new OrderIdComparer());

        // 교차 Aggregate 참조 (navigation 없음)
        builder.Property(o => o.ProductId)
            .HasConversion(new ProductIdConverter())
            .HasMaxLength(26)
            .IsRequired();
        builder.Property(o => o.ProductId)
            .Metadata.SetValueComparer(new ProductIdComparer());

        builder.Property(o => o.Quantity)
            .HasConversion(
                v => (int)v,
                i => Quantity.CreateFromValidated(i));

        builder.Property(o => o.UnitPrice)
            .HasConversion(
                v => (decimal)v,
                d => Money.CreateFromValidated(d))
            .HasPrecision(18, 4);

        builder.Property(o => o.TotalAmount)
            .HasConversion(
                v => (decimal)v,
                d => Money.CreateFromValidated(d))
            .HasPrecision(18, 4);

        builder.Property(o => o.ShippingAddress)
            .HasConversion(
                v => (string)v,
                s => ShippingAddress.CreateFromValidated(s))
            .HasMaxLength(ShippingAddress.MaxLength)
            .IsRequired();

        builder.Property(o => o.CreatedAt);
        builder.Property(o => o.UpdatedAt);
    }
}
