using Functorium.Applications.Queries;

namespace PaginationAndSorting;

/// <summary>
/// PageRequest, CursorPageRequest, SortExpression, PagedResult, CursorPagedResult의 사용법을 보여줍니다.
/// </summary>
public static class PaginationDemo
{
    /// <summary>
    /// Offset 기반 PagedResult를 생성합니다.
    /// 전체 데이터에서 페이지에 해당하는 항목을 추출합니다.
    /// </summary>
    public static PagedResult<T> CreatePagedResult<T>(
        IReadOnlyList<T> allItems, PageRequest page)
    {
        var items = allItems
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToList();

        return new PagedResult<T>(items, allItems.Count, page.Page, page.PageSize);
    }

    /// <summary>
    /// Cursor 기반 CursorPagedResult를 생성합니다.
    /// cursorSelector로 커서 값을 추출하여 커서 이후 항목을 반환합니다.
    /// </summary>
    public static CursorPagedResult<T> CreateCursorPagedResult<T>(
        IReadOnlyList<T> allItems,
        CursorPageRequest cursor,
        Func<T, string> cursorSelector)
    {
        IEnumerable<T> filtered = allItems;

        if (cursor.After is not null)
        {
            var afterIndex = -1;
            for (int i = allItems.Count - 1; i >= 0; i--)
            {
                if (string.Compare(cursorSelector(allItems[i]), cursor.After, StringComparison.Ordinal) <= 0)
                {
                    afterIndex = i;
                    break;
                }
            }
            filtered = afterIndex >= 0 ? allItems.Skip(afterIndex + 1) : allItems;
        }

        var items = filtered.Take(cursor.PageSize + 1).ToList();
        var hasMore = items.Count > cursor.PageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        string? nextCursor = hasMore && items.Count > 0 ? cursorSelector(items[^1]) : null;
        string? prevCursor = items.Count > 0 ? cursorSelector(items[0]) : null;

        return new CursorPagedResult<T>(items, nextCursor, prevCursor, hasMore);
    }

    /// <summary>
    /// SortExpression을 사용하여 항목을 정렬합니다.
    /// </summary>
    public static List<T> ApplySort<T>(
        IEnumerable<T> items,
        SortExpression sort,
        Func<string, Func<T, object>> selectorFactory,
        string defaultField)
    {
        var list = items.ToList();
        if (list.Count == 0) return list;

        var field = sort.IsEmpty
            ? new SortField(defaultField, SortDirection.Ascending)
            : sort.Fields[0];

        var selector = selectorFactory(field.FieldName);
        IOrderedEnumerable<T> ordered = field.Direction == SortDirection.Descending
            ? list.OrderByDescending(selector)
            : list.OrderBy(selector);

        // 추가 정렬 필드 적용
        if (!sort.IsEmpty)
        {
            foreach (var additionalField in sort.Fields.Skip(1))
            {
                var additionalSelector = selectorFactory(additionalField.FieldName);
                ordered = additionalField.Direction == SortDirection.Descending
                    ? ordered.ThenByDescending(additionalSelector)
                    : ordered.ThenBy(additionalSelector);
            }
        }

        return ordered.ToList();
    }
}
