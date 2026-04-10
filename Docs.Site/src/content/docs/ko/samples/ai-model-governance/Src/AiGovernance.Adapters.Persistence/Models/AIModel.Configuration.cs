using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiGovernance.Adapters.Persistence.Models;

public class AIModelConfiguration : IEntityTypeConfiguration<AIModelModel>
{
    public void Configure(EntityTypeBuilder<AIModelModel> builder)
    {
        builder.ToTable("AIModels");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasMaxLength(26);

        builder.Property(m => m.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Version)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Purpose)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.RiskTier)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.CreatedAt);
        builder.Property(m => m.UpdatedAt);

        builder.Property(m => m.DeletedAt);
        builder.Property(m => m.DeletedBy).HasMaxLength(320);

        // Global Query Filter: 삭제된 모델 자동 제외
        builder.HasQueryFilter(m => m.DeletedAt == null);
    }
}
