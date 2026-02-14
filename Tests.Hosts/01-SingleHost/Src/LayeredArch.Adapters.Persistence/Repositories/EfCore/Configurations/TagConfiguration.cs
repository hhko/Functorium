using LayeredArch.Domain.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LayeredArch.Adapters.Persistence.Repositories.EfCore.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(new TagIdConverter())
            .HasMaxLength(26);
        builder.Property(t => t.Id)
            .Metadata.SetValueComparer(new TagIdComparer());

        builder.Property(t => t.Name)
            .HasConversion(
                v => (string)v,
                s => TagName.CreateFromValidated(s))
            .HasMaxLength(TagName.MaxLength)
            .IsRequired();
    }
}
