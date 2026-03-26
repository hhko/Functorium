using AiGovernance.Adapters.Persistence.Assessments;
using AiGovernance.Adapters.Persistence.Deployments;
using AiGovernance.Adapters.Persistence.Incidents;
using AiGovernance.Adapters.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace AiGovernance.Adapters.Persistence;

public class GovernanceDbContext : DbContext
{
    public DbSet<AIModelModel> AIModels => Set<AIModelModel>();
    public DbSet<DeploymentModel> Deployments => Set<DeploymentModel>();
    public DbSet<AssessmentModel> Assessments => Set<AssessmentModel>();
    public DbSet<CriterionModel> Criteria => Set<CriterionModel>();
    public DbSet<IncidentModel> Incidents => Set<IncidentModel>();

    public GovernanceDbContext(DbContextOptions<GovernanceDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GovernanceDbContext).Assembly);
    }
}
