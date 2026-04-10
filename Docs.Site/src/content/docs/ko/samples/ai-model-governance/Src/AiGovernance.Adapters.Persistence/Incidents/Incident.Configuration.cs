using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiGovernance.Adapters.Persistence.Incidents;

public class IncidentConfiguration : IEntityTypeConfiguration<IncidentModel>
{
    public void Configure(EntityTypeBuilder<IncidentModel> builder)
    {
        builder.ToTable("Incidents");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasMaxLength(26);

        builder.Property(i => i.DeploymentId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(i => i.ModelId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(i => i.Severity)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(i => i.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(i => i.ResolutionNote)
            .HasMaxLength(2000);

        builder.Property(i => i.ReportedAt);
        builder.Property(i => i.ResolvedAt);
        builder.Property(i => i.CreatedAt);
        builder.Property(i => i.UpdatedAt);

        builder.HasIndex(i => i.ModelId);
        builder.HasIndex(i => i.DeploymentId);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.Severity);
    }
}
