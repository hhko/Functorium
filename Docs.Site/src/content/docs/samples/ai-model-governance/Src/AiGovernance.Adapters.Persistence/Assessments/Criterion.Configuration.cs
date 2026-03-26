using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiGovernance.Adapters.Persistence.Assessments;

public class CriterionConfiguration : IEntityTypeConfiguration<CriterionModel>
{
    public void Configure(EntityTypeBuilder<CriterionModel> builder)
    {
        builder.ToTable("Criteria");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasMaxLength(26);

        builder.Property(c => c.AssessmentId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(c => c.Result)
            .HasMaxLength(20);

        builder.Property(c => c.Notes)
            .HasMaxLength(2000);

        builder.Property(c => c.EvaluatedAt);

        builder.HasOne<AssessmentModel>()
            .WithMany(a => a.Criteria)
            .HasForeignKey(c => c.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
