using AiGovernance.Domain.AggregateRoots.Models;
using AiGovernance.Domain.AggregateRoots.Models.ValueObjects;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AiGovernance.Adapters.Persistence.Models.Repositories;

/// <summary>
/// EF Core 기반 AI 모델 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class AIModelRepositoryEfCore
    : EfCoreRepositoryBase<AIModel, AIModelId, AIModelModel>, IAIModelRepository
{
    private readonly GovernanceDbContext _dbContext;

    public AIModelRepositoryEfCore(GovernanceDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               propertyMap: new PropertyMap<AIModel, AIModelModel>()
                   .Map(m => (string)m.Name, e => e.Name)
                   .Map(m => (string)m.RiskTier, e => e.RiskTier)
                   .Map(m => m.Id.ToString(), e => e.Id))
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<AIModelModel> DbSet => _dbContext.AIModels;

    protected override AIModel ToDomain(AIModelModel model) =>
        AIModel.CreateFromValidated(
            AIModelId.Create(model.Id),
            ModelName.CreateFromValidated(model.Name),
            ModelVersion.CreateFromValidated(model.Version),
            ModelPurpose.CreateFromValidated(model.Purpose),
            RiskTier.CreateFromValidated(model.RiskTier),
            model.CreatedAt,
            Optional(model.UpdatedAt),
            Optional(model.DeletedAt),
            Optional(model.DeletedBy));

    protected override AIModelModel ToModel(AIModel aggregate) => new()
    {
        Id = aggregate.Id.ToString(),
        Name = aggregate.Name,
        Version = aggregate.Version,
        Purpose = aggregate.Purpose,
        RiskTier = aggregate.RiskTier,
        CreatedAt = aggregate.CreatedAt,
        UpdatedAt = aggregate.UpdatedAt.ToNullable(),
        DeletedAt = aggregate.DeletedAt.ToNullable(),
        DeletedBy = aggregate.DeletedBy.Match(Some: v => (string?)v, None: () => null)
    };

    // ─── Soft Delete 오버라이드 ──────────────────────

    public override FinT<IO, int> Delete(AIModelId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await ReadQueryIgnoringFilters()
                .FirstOrDefaultAsync(ByIdPredicate(id));

            if (model is null)
            {
                return NotFoundError(id);
            }

            var aiModel = ToDomain(model);
            aiModel.Archive("system");

            var updatedModel = ToModel(aiModel);
            DbSet.Attach(updatedModel);
            _dbContext.Entry(updatedModel).Property(p => p.DeletedAt).IsModified = true;
            _dbContext.Entry(updatedModel).Property(p => p.DeletedBy).IsModified = true;

            EventCollector.Track(aiModel);
            return Fin.Succ(1);
        });
    }

    // ─── AIModel 고유 메서드 ─────────────────────────

    public virtual FinT<IO, bool> Exists(Specification<AIModel> spec)
        => ExistsBySpec(spec);

    public virtual FinT<IO, AIModel> GetByIdIncludingDeleted(AIModelId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await ReadQueryIgnoringFilters()
                .FirstOrDefaultAsync(ByIdPredicate(id));

            if (model is not null)
            {
                return Fin.Succ(ToDomain(model));
            }

            return NotFoundError(id);
        });
    }
}
