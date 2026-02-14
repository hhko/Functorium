using LayeredArch.Domain.AggregateRoots.Customers;
using LayeredArch.Domain.AggregateRoots.Customers.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasConversion(new CustomerIdConverter())
            .HasMaxLength(26);
        builder.Property(c => c.Id)
            .Metadata.SetValueComparer(new CustomerIdComparer());

        builder.Property(c => c.Name)
            .HasConversion(
                v => (string)v,
                s => CustomerName.CreateFromValidated(s))
            .HasMaxLength(CustomerName.MaxLength)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasConversion(
                v => (string)v,
                s => Email.CreateFromValidated(s))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(c => c.CreditLimit)
            .HasConversion(
                v => (decimal)v,
                d => Money.CreateFromValidated(d))
            .HasPrecision(18, 4);

        builder.Property(c => c.CreatedAt);
        builder.Property(c => c.UpdatedAt);
    }
}
