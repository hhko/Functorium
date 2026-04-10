using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Deployments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Models;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AiGovernance.Adapters.Persistence.Deployments.Repositories;

/// <summary>
/// EF Core 기반 배포 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class DeploymentRepositoryEfCore
    : EfCoreRepositoryBase<ModelDeployment, ModelDeploymentId, DeploymentModel>, IDeploymentRepository
{
    private readonly GovernanceDbContext _dbContext;

    public DeploymentRepositoryEfCore(GovernanceDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               propertyMap: new PropertyMap<ModelDeployment, DeploymentModel>()
                   .Map(d => d.ModelId.ToString(), e => e.ModelId)
                   .Map(d => (string)d.Status, e => e.Status)
                   .Map(d => d.Id.ToString(), e => e.Id))
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<DeploymentModel> DbSet => _dbContext.Deployments;

    protected override ModelDeployment ToDomain(DeploymentModel model) =>
        ModelDeployment.CreateFromValidated(
            ModelDeploymentId.Create(model.Id),
            AIModelId.Create(model.ModelId),
            EndpointUrl.CreateFromValidated(model.EndpointUrl),
            DeploymentEnvironment.CreateFromValidated(model.Environment),
            DeploymentStatus.CreateFromValidated(model.Status),
            DriftThreshold.CreateFromValidated(model.DriftThreshold),
            Optional(model.LastHealthCheckAt),
            model.DeployedAt,
            model.CreatedAt,
            Optional(model.UpdatedAt));

    protected override DeploymentModel ToModel(ModelDeployment aggregate) => new()
    {
        Id = aggregate.Id.ToString(),
        ModelId = aggregate.ModelId.ToString(),
        EndpointUrl = aggregate.EndpointUrl,
        Environment = aggregate.Environment,
        Status = aggregate.Status,
        DriftThreshold = aggregate.DriftThreshold,
        LastHealthCheckAt = aggregate.LastHealthCheckAt.ToNullable(),
        DeployedAt = aggregate.DeployedAt,
        CreatedAt = aggregate.CreatedAt,
        UpdatedAt = aggregate.UpdatedAt.ToNullable()
    };

    // ─── Deployment 고유 메서드 ──────────────────────

    public virtual FinT<IO, bool> Exists(Specification<ModelDeployment> spec)
        => ExistsBySpec(spec);

    public virtual FinT<IO, Seq<ModelDeployment>> Find(Specification<ModelDeployment> spec)
    {
        return IO.liftAsync(async () =>
        {
            return await BuildQuery(spec).Match<Task<Fin<Seq<ModelDeployment>>>>(
                Succ: async query =>
                {
                    var models = await query.ToListAsync();
                    return Fin.Succ(toSeq(models.Select(ToDomain)));
                },
                Fail: error => Task.FromResult(Fin.Fail<Seq<ModelDeployment>>(error)));
        });
    }
}
