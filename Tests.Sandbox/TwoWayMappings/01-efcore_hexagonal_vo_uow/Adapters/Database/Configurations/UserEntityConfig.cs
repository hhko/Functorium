using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MyApp.Adapters.Database.Configurations;

public sealed class UserEntityConfig : IEntityTypeConfiguration<UserJpaEntity>
{
    public void Configure(EntityTypeBuilder<UserJpaEntity> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.NormalizedEmail).IsRequired().HasMaxLength(320);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);

        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
    }
}
