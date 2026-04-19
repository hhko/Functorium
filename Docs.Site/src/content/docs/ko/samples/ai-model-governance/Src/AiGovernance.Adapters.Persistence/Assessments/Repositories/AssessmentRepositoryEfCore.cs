using AiGovernance.Domain.AggregateRoots.Assessments;
using AiGovernance.Domain.AggregateRoots.Assessments.ValueObjects;
using AiGovernance.Domain.AggregateRoots.Deployments;
using AiGovernance.Domain.AggregateRoots.Models;
using Functorium.Adapters.Repositories;
using Functorium.Adapters.SourceGenerators;
using Functorium.Applications.Events;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace AiGovernance.Adapters.Persistence.Assessments.Repositories;

/// <summary>
/// EF Core 기반 컴플라이언스 평가 리포지토리 구현
/// </summary>
[GenerateObservablePort]
public class AssessmentRepositoryEfCore
    : EfCoreRepositoryBase<ComplianceAssessment, ComplianceAssessmentId, AssessmentModel>, IAssessmentRepository
{
    private readonly GovernanceDbContext _dbContext;

    public AssessmentRepositoryEfCore(GovernanceDbContext dbContext, IDomainEventCollector eventCollector)
        : base(eventCollector,
               q => q.Include(a => a.Criteria),
               new PropertyMap<ComplianceAssessment, AssessmentModel>()
                   .Map(a => a.ModelId.ToString(), e => e.ModelId)
                   .Map(a => a.DeploymentId.ToString(), e => e.DeploymentId)
                   .Map(a => (string)a.Status, e => e.Status)
                   .Map(a => a.Id.ToString(), e => e.Id))
        => _dbContext = dbContext;

    // ─── 필수 선언 ───────────────────────────────────

    protected override DbContext DbContext => _dbContext;
    protected override DbSet<AssessmentModel> DbSet => _dbContext.Assessments;

    protected override void BuildSetters(UpdateSettersBuilder<AssessmentModel> setters, AssessmentModel model) { }

    protected override ComplianceAssessment ToDomain(AssessmentModel model)
    {
        var criteria = model.Criteria.Select(c =>
            AssessmentCriterion.CreateFromValidated(
                AssessmentCriterionId.Create(c.Id),
                c.Name,
                c.Description,
                c.Result is not null ? CriterionResult.CreateFromValidated(c.Result) : Option<CriterionResult>.None,
                Optional(c.Notes),
                Optional(c.EvaluatedAt)));

        return ComplianceAssessment.CreateFromValidated(
            ComplianceAssessmentId.Create(model.Id),
            AIModelId.Create(model.ModelId),
            ModelDeploymentId.Create(model.DeploymentId),
            model.OverallScore.HasValue
                ? AssessmentScore.CreateFromValidated(model.OverallScore.Value)
                : Option<AssessmentScore>.None,
            AssessmentStatus.CreateFromValidated(model.Status),
            criteria,
            model.AssessedAt,
            model.CreatedAt,
            Optional(model.UpdatedAt));
    }

    protected override AssessmentModel ToModel(ComplianceAssessment aggregate)
    {
        var assessmentId = aggregate.Id.ToString();
        return new()
        {
            Id = assessmentId,
            ModelId = aggregate.ModelId.ToString(),
            DeploymentId = aggregate.DeploymentId.ToString(),
            OverallScore = aggregate.OverallScore.Match(Some: s => (int?)s, None: () => null),
            Status = aggregate.Status,
            AssessedAt = aggregate.AssessedAt,
            CreatedAt = aggregate.CreatedAt,
            UpdatedAt = aggregate.UpdatedAt.ToNullable(),
            Criteria = aggregate.Criteria.Select(c => new CriterionModel
            {
                Id = c.Id.ToString(),
                AssessmentId = assessmentId,
                Name = c.Name,
                Description = c.Description,
                Result = c.Result.Match(Some: r => (string?)r, None: () => null),
                Notes = c.Notes.Match(Some: n => (string?)n, None: () => null),
                EvaluatedAt = c.EvaluatedAt.ToNullable()
            }).ToList()
        };
    }

    // ─── Update 오버라이드 (Criteria 포함) ───────────

    public override FinT<IO, ComplianceAssessment> Update(ComplianceAssessment aggregate)
    {
        return IO.liftAsync(async () =>
        {
            var id = aggregate.Id.ToString();
            var existing = await _dbContext.Assessments
                .Include(a => a.Criteria)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existing is null)
                return NotFoundError(aggregate.Id);

            var updated = ToModel(aggregate);

            // Assessment 본체 업데이트
            _dbContext.Entry(existing).CurrentValues.SetValues(updated);

            // Criteria 동기화: 기존 삭제 후 새로 추가
            _dbContext.Criteria.RemoveRange(existing.Criteria);
            foreach (var criterion in updated.Criteria)
            {
                _dbContext.Criteria.Add(criterion);
            }

            EventCollector.Track(aggregate);
            return Fin.Succ(aggregate);
        });
    }

    // ─── Assessment 고유 메서드 ──────────────────────

    public virtual FinT<IO, Seq<ComplianceAssessment>> Find(Specification<ComplianceAssessment> spec)
    {
        return IO.liftAsync(async () =>
        {
            return await BuildQuery(spec).Match<Task<Fin<Seq<ComplianceAssessment>>>>(
                Succ: async query =>
                {
                    var models = await query.ToListAsync();
                    return Fin.Succ(toSeq(models.Select(ToDomain)));
                },
                Fail: error => Task.FromResult(Fin.Fail<Seq<ComplianceAssessment>>(error)));
        });
    }
}
