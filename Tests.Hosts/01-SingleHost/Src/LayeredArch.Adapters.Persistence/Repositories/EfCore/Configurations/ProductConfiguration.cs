using LayeredArch.Domain.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasConversion(new ProductIdConverter())
            .HasMaxLength(26);
        builder.Property(p => p.Id)
            .Metadata.SetValueComparer(new ProductIdComparer());

        builder.Property(p => p.Name)
            .HasConversion(
                v => (string)v,
                s => ProductName.CreateFromValidated(s))
            .HasMaxLength(ProductName.MaxLength)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasConversion(
                v => (string)v,
                s => ProductDescription.CreateFromValidated(s))
            .HasMaxLength(ProductDescription.MaxLength)
            .IsRequired();

        builder.Property(p => p.Price)
            .HasConversion(
                v => (decimal)v,
                d => Money.CreateFromValidated(d))
            .HasPrecision(18, 4);

        builder.Property(p => p.StockQuantity)
            .HasConversion(
                v => (int)v,
                i => Quantity.CreateFromValidated(i));

        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);

        // Tags 컬렉션 (backing field)
        builder.HasMany(p => p.Tags)
            .WithOne()
            .HasForeignKey("ProductId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Tags)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
