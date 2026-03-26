using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.Products;

public class ProductTagConfiguration : IEntityTypeConfiguration<ProductTagModel>
{
    public void Configure(EntityTypeBuilder<ProductTagModel> builder)
    {
        builder.ToTable("ProductTags");
        builder.HasKey(pt => new { pt.ProductId, pt.TagId });

        builder.Property(pt => pt.ProductId)
            .HasMaxLength(26);

        builder.Property(pt => pt.TagId)
            .HasMaxLength(26);
    }
}
