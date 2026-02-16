using LayeredArch.Adapters.Persistence.Repositories.EfCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<CustomerModel>
{
    public void Configure(EntityTypeBuilder<CustomerModel> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasMaxLength(26);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.CreditLimit)
            .HasPrecision(18, 4);

        builder.Property(c => c.CreatedAt);
        builder.Property(c => c.UpdatedAt);
    }
}
