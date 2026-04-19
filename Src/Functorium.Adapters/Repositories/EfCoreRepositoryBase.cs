using System.Linq.Expressions;
using System.Reflection;
using Functorium.Adapters.Errors;
using Functorium.Applications.Events;
using Functorium.Domains.Entities;
using Functorium.Abstractions.Observabilities;
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
    where TModel : class, IHasStringId
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
    /// BuildQuery/Exists/Count/DeleteBy 사용 시 필수입니다.
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

    /// <summary>SQL IN 절 파라미터 한계 방지를 위한 배치 크기 (기본값: 500).</summary>
    protected virtual int IdBatchSize => 500;

    // ─── 서브클래스 필수 구현 ────────────────────────────

    /// <summary>EF Core DbContext. CreateRange 청크 처리의 SaveChangesAsync/ChangeTracker.Clear에서 사용됩니다.</summary>
    protected abstract DbContext DbContext { get; }

    /// <summary>엔티티 모델의 DbSet</summary>
    protected abstract DbSet<TModel> DbSet { get; }

    /// <summary>Model → Domain 매핑</summary>
    protected abstract TAggregate ToDomain(TModel model);

    /// <summary>Domain → Model 매핑</summary>
    protected abstract TModel ToModel(TAggregate aggregate);

    /// <summary>
    /// ExecuteUpdateAsync용 SetProperty 빌더.
    /// [GenerateSetters] Source Generator가 TModel.ApplySetters()를 생성하므로
    /// 서브클래스에서 단순 위임으로 구현합니다.
    /// <example>
    /// protected override void BuildSetters(UpdateSettersBuilder&lt;CustomerModel&gt; s, CustomerModel model)
    ///     => CustomerModel.ApplySetters(s, model);
    /// </example>
    /// </summary>
    protected abstract void BuildSetters(
        Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<TModel> setters,
        TModel model);

    // ─── 정적 캐싱: Expression 공유 구성요소 ─────────

    private static readonly MethodInfo ListContainsMethod =
        typeof(System.Collections.Generic.List<string>).GetMethod("Contains", [typeof(string)])!;
    private static readonly ParameterExpression ModelParam =
        Expression.Parameter(typeof(TModel), "m");
    private static readonly MemberExpression IdProperty =
        Expression.Property(ModelParam, nameof(IHasStringId.Id));

    /// <summary>
    /// 단일 ID 매칭 Expression. IHasStringId 기반 기본 구현을 제공합니다.
    /// Expression을 수동 빌드하여 TModel의 구체 프로퍼티를 참조합니다 (EF Core 호환).
    /// </summary>
    protected virtual Expression<Func<TModel, bool>> ByIdPredicate(TId id)
    {
        var body = Expression.Equal(IdProperty, Expression.Constant(id.ToString()));
        return Expression.Lambda<Func<TModel, bool>>(body, ModelParam);
    }

    /// <summary>
    /// 복수 ID 매칭 Expression. IHasStringId 기반 기본 구현을 제공합니다.
    /// Expression을 수동 빌드하여 TModel의 구체 프로퍼티를 참조합니다 (EF Core 호환).
    /// </summary>
    protected virtual Expression<Func<TModel, bool>> ByIdsPredicate(IReadOnlyList<TId> ids)
    {
        var ss = ids.Select(id => id.ToString()).ToList();
        var body = Expression.Call(Expression.Constant(ss), ListContainsMethod, IdProperty);
        return Expression.Lambda<Func<TModel, bool>>(body, ModelParam);
    }

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
    protected Fin<IQueryable<TModel>> BuildQuery(Specification<TAggregate> spec)
    {
        if (PropertyMap is null)
        {
            return NotConfiguredError(
                $"PropertyMap is required for Specification operations (Exists/Count/DeleteBy/BuildQuery). Provide it via the {GetType().Name} constructor.");
        }

        var expression = SpecificationExpressionResolver.TryResolve(spec);
        if (expression is null)
        {
            return NotSupportedError(spec.GetType().Name,
                $"No Expression is defined for Specification '{spec.GetType().Name}'.");
        }

        return Fin.Succ(_applyIncludes(DbSet.AsNoTracking()).Where(PropertyMap.Translate(expression)));
    }

    /// <summary>
    /// Specification 기반 존재 여부 확인. PropertyMap 필수.
    /// </summary>
    public virtual FinT<IO, bool> Exists(Specification<TAggregate> spec)
    {
        return IO.liftAsync(async () =>
        {
            return await BuildQuery(spec).Match<Task<Fin<bool>>>(
                Succ: async query => Fin.Succ(await query.AnyAsync()),
                Fail: error => Task.FromResult(Fin.Fail<bool>(error)));
        });
    }

    /// <summary>
    /// Specification 기반 건수 확인. PropertyMap 필수. (1 SQL COUNT)
    /// </summary>
    public virtual FinT<IO, int> Count(Specification<TAggregate> spec)
    {
        return IO.liftAsync(async () =>
        {
            return await BuildQuery(spec).Match<Task<Fin<int>>>(
                Succ: async query => Fin.Succ(await query.CountAsync()),
                Fail: error => Task.FromResult(Fin.Fail<int>(error)));
        });
    }

    /// <summary>
    /// Specification 기반 조건부 대량 삭제. PropertyMap 필수. (1 SQL DELETE)
    /// Change Tracker를 우회하여 직접 SQL DELETE를 실행합니다.
    /// </summary>
    public virtual FinT<IO, int> DeleteBy(Specification<TAggregate> spec)
    {
        return IO.liftAsync(async () =>
        {
            return await BuildQuery(spec).Match<Task<Fin<int>>>(
                Succ: async query => Fin.Succ(await query.ExecuteDeleteAsync()),
                Fail: error => Task.FromResult(Fin.Fail<int>(error)));
        });
    }

    /// <summary>
    /// Specification 기반 조건부 대량 업데이트. PropertyMap 필수. (1 SQL UPDATE)
    /// Change Tracker를 우회하여 직접 SQL UPDATE를 실행합니다.
    /// 서브클래스에서 도메인 특화 메서드로 래핑하여 사용합니다.
    /// </summary>
    protected FinT<IO, int> UpdateBy(
        Specification<TAggregate> spec,
        Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<TModel>> setters)
    {
        return IO.liftAsync(async () =>
        {
            return await BuildQuery(spec).Match<Task<Fin<int>>>(
                Succ: async query => Fin.Succ(await query.ExecuteUpdateAsync(setters)),
                Fail: error => Task.FromResult(Fin.Fail<int>(error)));
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
        return IO.liftAsync(async () =>
        {
            var model = ToModel(aggregate);
            int affected = await DbSet
                .Where(ByIdPredicate(aggregate.Id))
                .ExecuteUpdateAsync(s => BuildSetters(s, model));

            if (affected == 0)
                return NotFoundError(aggregate.Id);

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

    /// <remarks>
    /// IdBatchSize를 초과하는 대량 데이터는 청크 단위로 SaveChangesAsync + ChangeTracker.Clear를 수행합니다.
    /// 이 경로는 UsecaseTransactionPipeline의 트랜잭션 컨텍스트 내에서 사용해야 합니다.
    /// 트랜잭션 없이 호출 시 부분 삽입 후 롤백이 불가능합니다.
    /// </remarks>
    public virtual FinT<IO, int> CreateRange(IReadOnlyList<TAggregate> aggregates)
    {
        if (aggregates.Count <= IdBatchSize)
        {
            return IO.lift(() =>
            {
                if (aggregates.Count == 0)
                    return Fin.Succ(0);

                DbSet.AddRange(aggregates.Select(ToModel));
                EventCollector.TrackRange(aggregates);
                return Fin.Succ(aggregates.Count);
            });
        }

        return IO.liftAsync(async () =>
        {
            foreach (var chunk in aggregates.Chunk(IdBatchSize))
            {
                DbSet.AddRange(chunk.Select(ToModel));
                await DbContext.SaveChangesAsync();
                DbContext.ChangeTracker.Clear();
            }

            EventCollector.TrackRange(aggregates);
            return Fin.Succ(aggregates.Count);
        });
    }

    public virtual FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids)
    {
        return IO.liftAsync(async () =>
        {
            if (ids.Count == 0)
                return Fin.Succ(LanguageExt.Seq<TAggregate>.Empty);

            var distinctIds = ids.Distinct().ToList();

            List<TModel> models;
            if (distinctIds.Count <= IdBatchSize)
            {
                models = await ReadQuery()
                    .Where(ByIdsPredicate(distinctIds))
                    .ToListAsync();
            }
            else
            {
                models = new List<TModel>(distinctIds.Count);
                foreach (var batch in distinctIds.Chunk(IdBatchSize))
                {
                    var batchResult = await ReadQuery()
                        .Where(ByIdsPredicate(batch))
                        .ToListAsync();
                    models.AddRange(batchResult);
                }
            }

            var aggregates = toSeq(models.Select(ToDomain));

            if (aggregates.Count != distinctIds.Count)
            {
                return PartialNotFoundError(distinctIds, aggregates);
            }

            return Fin.Succ(aggregates);
        });
    }

    public virtual FinT<IO, int> UpdateRange(IReadOnlyList<TAggregate> aggregates)
    {
        return IO.liftAsync(async () =>
        {
            if (aggregates.Count == 0)
                return Fin.Succ(0);

            int totalAffected = 0;
            foreach (var aggregate in aggregates)
            {
                var model = ToModel(aggregate);
                totalAffected += await DbSet
                    .Where(ByIdPredicate(aggregate.Id))
                    .ExecuteUpdateAsync(s => BuildSetters(s, model));
            }

            EventCollector.TrackRange(aggregates);
            return Fin.Succ(totalAffected);
        });
    }

    public virtual FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids)
    {
        return IO.liftAsync(async () =>
        {
            if (ids.Count == 0)
                return Fin.Succ(0);

            var distinctIds = ids.Distinct().ToList();
            int totalAffected;

            if (distinctIds.Count <= IdBatchSize)
            {
                totalAffected = await DbSet.Where(ByIdsPredicate(distinctIds))
                    .ExecuteDeleteAsync();
            }
            else
            {
                totalAffected = 0;
                foreach (var batch in distinctIds.Chunk(IdBatchSize))
                {
                    totalAffected += await DbSet.Where(ByIdsPredicate(batch))
                        .ExecuteDeleteAsync();
                }
            }

            return Fin.Succ(totalAffected);
        });
    }

    // ─── 에러 헬퍼 ───────────────────────────────────

    /// <summary>
    /// NotFound 에러 생성. GetType()을 사용하여 실제 서브클래스 이름이 에러 코드에 포함됩니다.
    /// </summary>
    protected Error NotFoundError(TId id) =>
        AdapterError.For(GetType(),
            new NotFound(), id.ToString()!,
            $"NotFound: No record found for ID '{id}'");

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

    /// <summary>
    /// NotConfigured 에러 생성. 필수 설정이 누락되었을 때 사용합니다.
    /// </summary>
    protected Error NotConfiguredError(string message) =>
        AdapterError.For(GetType(),
            new NotConfigured(), GetType().Name, message);

    /// <summary>
    /// NotSupported 에러 생성. 지원되지 않는 연산 요청 시 사용합니다.
    /// </summary>
    protected Error NotSupportedError(string currentValue, string message) =>
        AdapterError.For(GetType(),
            new NotSupported(), currentValue, message);

    // ─── 내부 헬퍼 ───────────────────────────────────

    private static string FormatIds(IEnumerable<string> ids, int maxDisplay = 3)
    {
        var list = ids.ToList();
        if (list.Count <= maxDisplay)
            return string.Join(", ", list);

        return string.Join(", ", list.Take(maxDisplay)) + $" ... (+{list.Count - maxDisplay} more)";
    }
}
