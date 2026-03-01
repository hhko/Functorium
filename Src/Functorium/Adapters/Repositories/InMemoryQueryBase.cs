using System.Runtime.CompilerServices;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using static LanguageExt.Prelude;

namespace Functorium.Adapters.Repositories;

/// <summary>
/// InMemory 기반 QueryAdapter의 공통 인프라.
/// DapperQueryBase의 InMemory 대응 베이스 클래스입니다.
/// 서브클래스는 데이터 소스 접근(GetProjectedItems)과 정렬 키(SortSelector)만 담당합니다.
/// </summary>
public abstract class InMemoryQueryBase<TEntity, TDto>
{
    /// <summary>기본 정렬 필드명</summary>
    protected abstract string DefaultSortField { get; }

    /// <summary>필터링 + DTO 프로젝션 (JOIN 로직 포함)</summary>
    protected abstract IEnumerable<TDto> GetProjectedItems(Specification<TEntity> spec);

    /// <summary>정렬 키 셀렉터 (필드명 → 셀렉터 함수)</summary>
    protected abstract Func<TDto, object> SortSelector(string fieldName);

    public virtual FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec, PageRequest page, SortExpression sort)
    {
        return IO.lift(() =>
        {
            var projected = toSeq(GetProjectedItems(spec));
            var sorted = ApplySort(projected, sort);
            var totalCount = sorted.Count;
            var items = sorted
                .Skip(page.Skip)
                .Take(page.PageSize)
                .ToSeq();

            return Fin.Succ(new PagedResult<TDto>(
                items, totalCount, page.Page, page.PageSize));
        });
    }

    public virtual async IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec,
        SortExpression sort,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var projected = toSeq(GetProjectedItems(spec));
        var sorted = ApplySort(projected, sort);

        foreach (var item in sorted)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask;
    }

    private Seq<TDto> ApplySort(Seq<TDto> items, SortExpression sort)
    {
        if (sort.IsEmpty)
            return toSeq(items.OrderBy(SortSelector(DefaultSortField)));

        IOrderedEnumerable<TDto>? ordered = null;

        foreach (var field in sort.Fields)
        {
            var selector = SortSelector(field.FieldName);
            var isDesc = field.Direction == SortDirection.Descending;
            ordered = (ordered, isDesc) switch
            {
                (null, false) => items.OrderBy(selector),
                (null, true) => items.OrderByDescending(selector),
                (_, false) => ordered!.ThenBy(selector),
                _ => ordered!.ThenByDescending(selector),
            };
        }

        return ordered is not null ? toSeq(ordered) : items;
    }
}
