using System.Linq.Expressions;
using Functorium.Adapters.Errors;
using Functorium.Applications.Events;
using Functorium.Domains.Entities;
using Functorium.Domains.Observabilities;
using Functorium.Domains.Repositories;
using Microsoft.EntityFrameworkCore;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace Functorium.Adapters.Repositories;

/// <summary>
/// EF Core Repository의 공통 베이스 클래스.
/// ApplyIncludes()에서 선언한 Include가 ReadQuery()를 통해 모든 읽기 쿼리에 자동 적용되어
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
    protected EfCoreRepositoryBase(IDomainEventCollector eventCollector)
        => EventCollector = eventCollector;

    /// <summary>도메인 이벤트 수집기. 서브클래스에서 override 메서드 내 이벤트 추적에 사용합니다.</summary>
    protected IDomainEventCollector EventCollector { get; }

    // ─── 서브클래스 필수 구현 ────────────────────────────

    /// <summary>엔티티 모델의 DbSet</summary>
    protected abstract DbSet<TModel> DbSet { get; }

    /// <summary>
    /// Navigation Property Include 선언 (N+1 방지의 핵심).
    /// 여기서 선언한 Include가 모든 읽기 쿼리에 자동 적용됩니다.
    /// Navigation Property가 없으면 query를 그대로 반환합니다.
    /// </summary>
    protected abstract IQueryable<TModel> ApplyIncludes(IQueryable<TModel> query);

    /// <summary>Model → Domain 매핑</summary>
    protected abstract TAggregate ToDomain(TModel model);

    /// <summary>Domain → Model 매핑</summary>
    protected abstract TModel ToModel(TAggregate aggregate);

    /// <summary>단일 ID 매칭 Expression</summary>
    protected abstract Expression<Func<TModel, bool>> ByIdPredicate(TId id);

    /// <summary>복수 ID 매칭 Expression</summary>
    protected abstract Expression<Func<TModel, bool>> ByIdsPredicate(IReadOnlyList<TId> ids);

    // ─── 중앙화된 쿼리 인프라 ─────────────────────────

    public abstract string RequestCategory { get; }

    /// <summary>
    /// Include가 자동 적용된 읽기 전용 쿼리.
    /// 모든 읽기 메서드는 이 메서드를 사용하므로 N+1이 구조적으로 불가능합니다.
    /// </summary>
    protected IQueryable<TModel> ReadQuery()
        => ApplyIncludes(DbSet.AsNoTracking());

    /// <summary>
    /// Include가 자동 적용된 읽기 전용 쿼리 (글로벌 필터 무시).
    /// Soft Delete된 엔티티 조회 등 IgnoreQueryFilters가 필요한 경우 사용합니다.
    /// </summary>
    protected IQueryable<TModel> ReadQueryIgnoringFilters()
        => ApplyIncludes(DbSet.IgnoreQueryFilters().AsNoTracking());

    // ─── IRepository 구현 ─────────────────────────────

    public virtual FinT<IO, TAggregate> Create(TAggregate aggregate)
    {
        return IO.liftAsync(async () =>
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

    public virtual FinT<IO, Unit> Delete(TId id)
    {
        return IO.liftAsync(async () =>
        {
            var model = await DbSet.FindAsync(id.ToString());
            if (model is null)
            {
                return NotFoundError(id);
            }

            DbSet.Remove(model);
            return Fin.Succ(unit);
        });
    }

    public virtual FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates)
    {
        return IO.liftAsync(async () =>
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
            var models = await ReadQuery()
                .Where(ByIdsPredicate(ids))
                .ToListAsync();
            return Fin.Succ(toSeq(models.Select(ToDomain)));
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

    public virtual FinT<IO, Unit> DeleteRange(IReadOnlyList<TId> ids)
    {
        return IO.liftAsync(async () =>
        {
            await DbSet.Where(ByIdsPredicate(ids))
                .ExecuteDeleteAsync();
            return Fin.Succ(unit);
        });
    }

    // ─── 에러 헬퍼 ───────────────────────────────────

    /// <summary>
    /// NotFound 에러 생성. GetType()을 사용하여 실제 서브클래스 이름이 에러 코드에 포함됩니다.
    /// </summary>
    protected Error NotFoundError(TId id) =>
        AdapterError.For(GetType(),
            new NotFound(), id.ToString()!,
            $"ID '{id}'을(를) 찾을 수 없습니다");
}
