using System.Runtime.CompilerServices;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
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
            var projected = GetProjectedItems(spec).ToList();
            var totalCount = projected.Count;
            ApplySortInPlace(projected, sort);
            var items = projected.Skip(page.Skip).Take(page.PageSize).ToList();

            return Fin.Succ(new PagedResult<TDto>(
                items, totalCount, page.Page, page.PageSize));
        });
    }

    public virtual FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
        Specification<TEntity> spec, CursorPageRequest cursor, SortExpression sort)
    {
        return IO.lift(() =>
        {
            var projected = GetProjectedItems(spec).ToList();
            ApplySortInPlace(projected, sort);

            var fieldName = sort.IsEmpty ? DefaultSortField : sort.Fields[0].FieldName;
            var selector = SortSelector(fieldName);

            IEnumerable<TDto> filtered = projected;
            if (cursor.After is not null)
            {
                var afterIndex = projected.FindLastIndex(item =>
                    string.Compare(selector(item)?.ToString(), cursor.After, StringComparison.Ordinal) <= 0);
                filtered = afterIndex >= 0 ? projected.Skip(afterIndex + 1) : projected;
            }
            else if (cursor.Before is not null)
            {
                var beforeIndex = projected.FindIndex(item =>
                    string.Compare(selector(item)?.ToString(), cursor.Before, StringComparison.Ordinal) >= 0);
                filtered = beforeIndex > 0 ? projected.Take(beforeIndex) : [];
            }

            var items = filtered.Take(cursor.PageSize + 1).ToList();
            var hasMore = items.Count > cursor.PageSize;
            if (hasMore) items.RemoveAt(items.Count - 1);

            string? nextCursor = hasMore && items.Count > 0 ? selector(items[^1])?.ToString() : null;
            string? prevCursor = items.Count > 0 ? selector(items[0])?.ToString() : null;

            return Fin.Succ(new CursorPagedResult<TDto>(items, nextCursor, prevCursor, hasMore));
        });
    }

    public virtual async IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec,
        SortExpression sort,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var projected = GetProjectedItems(spec).ToList();
        ApplySortInPlace(projected, sort);

        foreach (var item in projected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Read-side 존재 확인. GetProjectedItems를 통해 Specification 적용 후 Any().
    /// </summary>
    public virtual FinT<IO, bool> Exists(Specification<TEntity> spec)
    {
        return IO.lift(() => Fin.Succ(GetProjectedItems(spec).Any()));
    }

    /// <summary>
    /// Read-side 건수. GetProjectedItems를 통해 Specification 적용 후 Count().
    /// </summary>
    public virtual FinT<IO, int> Count(Specification<TEntity> spec)
    {
        return IO.lift(() => Fin.Succ(GetProjectedItems(spec).Count()));
    }

    private void ApplySortInPlace(List<TDto> items, SortExpression sort)
    {
        var sortField = sort.IsEmpty
            ? new SortField(DefaultSortField, SortDirection.Ascending)
            : sort.Fields[0];

        Comparison<TDto> comparison = BuildComparison(sort.IsEmpty
            ? [sortField]
            : sort.Fields);
        items.Sort(comparison);
    }

    private Comparison<TDto> BuildComparison(IEnumerable<SortField> fields)
    {
        return (x, y) =>
        {
            foreach (var field in fields)
            {
                var selector = SortSelector(field.FieldName);
                var cmp = Comparer<object>.Default.Compare(selector(x), selector(y));
                if (cmp != 0)
                    return field.Direction == SortDirection.Descending ? -cmp : cmp;
            }
            return 0;
        };
    }
}
