using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiGovernance.Adapters.Persistence.Assessments;

public class AssessmentConfiguration : IEntityTypeConfiguration<AssessmentModel>
{
    public void Configure(EntityTypeBuilder<AssessmentModel> builder)
    {
        builder.ToTable("Assessments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasMaxLength(26);

        builder.Property(a => a.ModelId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(a => a.DeploymentId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(a => a.OverallScore);

        builder.Property(a => a.Status)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(a => a.AssessedAt);
        builder.Property(a => a.CreatedAt);
        builder.Property(a => a.UpdatedAt);

        builder.HasMany(a => a.Criteria)
            .WithOne()
            .HasForeignKey(c => c.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.ModelId);
        builder.HasIndex(a => a.DeploymentId);
        builder.HasIndex(a => a.Status);
    }
}
