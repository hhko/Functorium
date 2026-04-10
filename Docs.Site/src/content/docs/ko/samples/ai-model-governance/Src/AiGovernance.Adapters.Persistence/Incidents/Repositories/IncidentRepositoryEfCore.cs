using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Incidents;
using AiGovernance.Domain.AggregateRoots.Incidents.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AiGovernance.Adapters.Persistence.Incidents.Repositories;

/// <summary>
/// EF Core 기반 인시던트 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class IncidentRepositoryEfCore
    : EfCoreRepositoryBase<ModelIncident, ModelIncidentId, IncidentModel>, IIncidentRepository
{
    private readonly GovernanceDbContext _dbContext;

    public IncidentRepositoryEfCore(GovernanceDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               propertyMap: new PropertyMap<ModelIncident, IncidentModel>()
                   .Map(i => i.ModelId.ToString(), e => e.ModelId)
                   .Map(i => i.DeploymentId.ToString(), e => e.DeploymentId)
                   .Map(i => (string)i.Status, e => e.Status)
                   .Map(i => (string)i.Severity, e => e.Severity)
                   .Map(i => i.Id.ToString(), e => e.Id))
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<IncidentModel> DbSet => _dbContext.Incidents;

    protected override ModelIncident ToDomain(IncidentModel model) =>
        ModelIncident.CreateFromValidated(
            ModelIncidentId.Create(model.Id),
            ModelDeploymentId.Create(model.DeploymentId),
            AIModelId.Create(model.ModelId),
            IncidentSeverity.CreateFromValidated(model.Severity),
            IncidentStatus.CreateFromValidated(model.Status),
            IncidentDescription.CreateFromValidated(model.Description),
            model.ResolutionNote is not null
                ? ResolutionNote.CreateFromValidated(model.ResolutionNote)
                : Option<ResolutionNote>.None,
            model.ReportedAt,
            Optional(model.ResolvedAt),
            model.CreatedAt,
            Optional(model.UpdatedAt));

    protected override IncidentModel ToModel(ModelIncident aggregate) => new()
    {
        Id = aggregate.Id.ToString(),
        DeploymentId = aggregate.DeploymentId.ToString(),
        ModelId = aggregate.ModelId.ToString(),
        Severity = aggregate.Severity,
        Status = aggregate.Status,
        Description = aggregate.Description,
        ResolutionNote = aggregate.ResolutionNote.Match(Some: n => (string?)n, None: () => null),
        ReportedAt = aggregate.ReportedAt,
        ResolvedAt = aggregate.ResolvedAt.ToNullable(),
        CreatedAt = aggregate.CreatedAt,
        UpdatedAt = aggregate.UpdatedAt.ToNullable()
    };

    // ─── Incident 고유 메서드 ────────────────────────

    public virtual FinT<IO, bool> Exists(Specification<ModelIncident> spec)
        => ExistsBySpec(spec);

    public virtual FinT<IO, Seq<ModelIncident>> Find(Specification<ModelIncident> spec)
    {
        return IO.liftAsync(async () =>
        {
            return await BuildQuery(spec).Match<Task<Fin<Seq<ModelIncident>>>>(
                Succ: async query =>
                {
                    var models = await query.ToListAsync();
                    return Fin.Succ(toSeq(models.Select(ToDomain)));
                },
                Fail: error => Task.FromResult(Fin.Fail<Seq<ModelIncident>>(error)));
        });
    }
}
