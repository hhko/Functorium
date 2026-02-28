using System.Linq.Expressions;
using Functorium.Adapters.Errors;
using Functorium.Applications.Events;
using Functorium.Domains.Entities;
using Functorium.Domains.Observabilities;
using Functorium.Domains.Repositories;
using Functorium.Domains.Specifications;
using Functorium.Domains.Specifications.Expressions;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace Functorium.Adapters.Repositories;

/// <summary>
/// EF Core Repository의 공통 베이스 클래스.
/// 생성자의 applyIncludes에서 선언한 Include가 ReadQuery()를 통해 모든 읽기 쿼리에 자동 적용되어
/// N+1 문제를 구조적으로 방지합니다.
/// </summary>
/// <typeparam name="TAggregate">Aggregate Root 타입</typeparam>
/// <typeparam name="TId">EntityId 구현 타입</typeparam>
/// <typeparam name="TModel">EF Core 엔티티 모델 타입</typeparam>
public abstract class EfCoreRepositoryBase<TAggregate, TId, TModel>
    : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
    where TModel : class
{
    private readonly Func<IQueryable<TModel>, IQueryable<TModel>> _applyIncludes;

    /// <param name="eventCollector">도메인 이벤트 수집기</param>
    /// <param name="applyIncludes">
    /// Navigation Property Include 선언 (N+1 방지의 핵심).
    /// ReadQuery()를 통해 모든 읽기 쿼리에 자동 적용됩니다.
    /// Navigation Property가 없으면 null(기본값)을 사용합니다.
    /// </param>
    /// <param name="propertyMap">
    /// Specification → Model Expression 변환을 위한 프로퍼티 매핑.
    /// BuildQuery/ExistsBySpec 사용 시 필수입니다.
    /// </param>
    protected EfCoreRepositoryBase(
        IDomainEventCollector eventCollector,
        Func<IQueryable<TModel>, IQueryable<TModel>>? applyIncludes = null,
        PropertyMap<TAggregate, TModel>? propertyMap = null)
    {
        EventCollector = eventCollector;
        _applyIncludes = applyIncludes ?? (static q => q);
        PropertyMap = propertyMap;
    }

    /// <summary>도메인 이벤트 수집기. 서브클래스에서 override 메서드 내 이벤트 추적에 사용합니다.</summary>
    protected IDomainEventCollector EventCollector { get; }

    /// <summary>Specification → Model Expression 변환을 위한 프로퍼티 매핑.</summary>
    protected PropertyMap<TAggregate, TModel>? PropertyMap { get; }

    // ─── 서브클래스 필수 구현 ────────────────────────────

    /// <summary>엔티티 모델의 DbSet</summary>
    protected abstract DbSet<TModel> DbSet { get; }

    /// <summary>Model → Domain 매핑</summary>
    protected abstract TAggregate ToDomain(TModel model);

    /// <summary>Domain → Model 매핑</summary>
    protected abstract TModel ToModel(TAggregate aggregate);

    /// <summary>단일 ID 매칭 Expression</summary>
    protected abstract Expression<Func<TModel, bool>> ByIdPredicate(TId id);

    /// <summary>복수 ID 매칭 Expression</summary>
    protected abstract Expression<Func<TModel, bool>> ByIdsPredicate(IReadOnlyList<TId> ids);

    // ─── 중앙화된 쿼리 인프라 ─────────────────────────

    public virtual string RequestCategory => "Repository";

    /// <summary>
    /// Include가 자동 적용된 읽기 전용 쿼리.
    /// 모든 읽기 메서드는 이 메서드를 사용하므로 N+1이 구조적으로 불가능합니다.
    /// </summary>
    protected IQueryable<TModel> ReadQuery()
        => _applyIncludes(DbSet.AsNoTracking());

    /// <summary>
    /// Include가 자동 적용된 읽기 전용 쿼리 (글로벌 필터 무시).
    /// Soft Delete된 엔티티 조회 등 IgnoreQueryFilters가 필요한 경우 사용합니다.
    /// </summary>
    protected IQueryable<TModel> ReadQueryIgnoringFilters()
        => _applyIncludes(DbSet.IgnoreQueryFilters().AsNoTracking());

    /// <summary>
    /// Specification → Model Expression 쿼리 빌더. PropertyMap 필수.
    /// </summary>
    protected IQueryable<TModel> BuildQuery(Specification<TAggregate> spec)
    {
        if (PropertyMap is null)
            throw new InvalidOperationException(
                $"{GetType().Name}: BuildQuery를 사용하려면 생성자에서 PropertyMap을 제공해야 합니다.");

        var expression = SpecificationExpressionResolver.TryResolve(spec)
            ?? throw new NotSupportedException(
                $"Specification '{spec.GetType().Name}'에 대한 Expression이 정의되지 않았습니다.");

        return _applyIncludes(DbSet.AsNoTracking()).Where(PropertyMap.Translate(expression));
    }

    /// <summary>
    /// Specification 기반 존재 여부 확인. PropertyMap 필수.
    /// </summary>
    protected FinT<IO, bool> ExistsBySpec(Specification<TAggregate> spec)
    {
        return IO.liftAsync(async () =>
        {
            bool exists = await BuildQuery(spec).AnyAsync();
            return Fin.Succ(exists);
        });
    }

    // ─── IRepository 구현 ─────────────────────────────

    public virtual FinT<IO, TAggregate> Create(TAggregate aggregate)
    {
        return IO.lift(() =>
        {
            DbSet.Add(ToModel(aggregate));
            EventCollector.Track(aggregate);
            return Fin.Succ(aggregate);
        });
    }

    public virtual FinT<IO, TAggregate> GetById(TId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await ReadQuery()
                .FirstOrDefaultAsync(ByIdPredicate(id));

            if (model is not null)
            {
                return Fin.Succ(ToDomain(model));
            }

            return NotFoundError(id);
        });
    }

    public virtual FinT<IO, TAggregate> Update(TAggregate aggregate)
    {
        return IO.lift(() =>
        {
            DbSet.Update(ToModel(aggregate));
            EventCollector.Track(aggregate);
            return Fin.Succ(aggregate);
        });
    }

    public virtual FinT<IO, int> Delete(TId id)
    {
        return IO.liftAsync(async () =>
        {
            int affected = await DbSet.Where(ByIdPredicate(id))
                .ExecuteDeleteAsync();
            return Fin.Succ(affected);
        });
    }

    public virtual FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates)
    {
        return IO.lift(() =>
        {
            DbSet.AddRange(aggregates.Select(ToModel));
            EventCollector.TrackRange(aggregates);
            return Fin.Succ(toSeq(aggregates));
        });
    }

    public virtual FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids)
    {
        return IO.liftAsync(async () =>
        {
            var distinctIds = ids.Distinct().ToList();
            var models = await ReadQuery()
                .Where(ByIdsPredicate(distinctIds))
                .ToListAsync();
            var aggregates = toSeq(models.Select(ToDomain));

            if (aggregates.Count != distinctIds.Count)
            {
                return PartialNotFoundError(distinctIds, aggregates);
            }

            return Fin.Succ(aggregates);
        });
    }

    public virtual FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates)
    {
        return IO.lift(() =>
        {
            DbSet.UpdateRange(aggregates.Select(ToModel));
            EventCollector.TrackRange(aggregates);
            return Fin.Succ(toSeq(aggregates));
        });
    }

    public virtual FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids)
    {
        return IO.liftAsync(async () =>
        {
            int affected = await DbSet.Where(ByIdsPredicate(ids))
                .ExecuteDeleteAsync();
            return Fin.Succ(affected);
        });
    }

    // ─── 에러 헬퍼 ───────────────────────────────────

    /// <summary>
    /// NotFound 에러 생성. GetType()을 사용하여 실제 서브클래스 이름이 에러 코드에 포함됩니다.
    /// </summary>
    protected Error NotFoundError(TId id) =>
        AdapterError.For(GetType(),
            new NotFound(), id.ToString()!,
            $"No record found for ID '{id}'");

    /// <summary>
    /// PartialNotFound 에러 생성. 요청 건수와 결과 건수가 다를 때 사용합니다.
    /// </summary>
    protected Error PartialNotFoundError(IReadOnlyList<TId> requestedIds, Seq<TAggregate> foundAggregates)
    {
        var foundIds = foundAggregates.Select(a => a.Id.ToString()).ToHashSet();
        var missingIds = requestedIds.Where(id => !foundIds.Contains(id.ToString()!)).ToList();
        var missingIdsStr = FormatIds(missingIds.Select(id => id.ToString()!));

        return AdapterError.For(GetType(),
            new PartialNotFound(), missingIdsStr,
            $"Requested {requestedIds.Count} but found {foundAggregates.Count}. Missing IDs: {missingIdsStr}");
    }

    private static string FormatIds(IEnumerable<string> ids, int maxDisplay = 3)
    {
        var list = ids.ToList();
        if (list.Count <= maxDisplay)
            return string.Join(", ", list);

        return string.Join(", ", list.Take(maxDisplay)) + $" ... (+{list.Count - maxDisplay} more)";
    }
}
