using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AiGovernance.Adapters.Persistence.Deployments;

public class DeploymentConfiguration : IEntityTypeConfiguration<DeploymentModel>
{
    public void Configure(EntityTypeBuilder<DeploymentModel> builder)
    {
        builder.ToTable("Deployments");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasMaxLength(26);

        builder.Property(d => d.ModelId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(d => d.EndpointUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(d => d.Environment)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.DriftThreshold)
            .HasPrecision(5, 4);

        builder.Property(d => d.LastHealthCheckAt);
        builder.Property(d => d.DeployedAt);
        builder.Property(d => d.CreatedAt);
        builder.Property(d => d.UpdatedAt);

        builder.HasIndex(d => d.ModelId);
        builder.HasIndex(d => d.Status);
    }
}
