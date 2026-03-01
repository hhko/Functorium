using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Reflection;
using Dapper;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
namespace Functorium.Adapters.Repositories;

/// <summary>
/// Dapper 기반 QueryAdapter의 공통 인프라.
/// 서브클래스는 SQL 선언(SelectSql, CountSql)과 WHERE 빌드만 담당합니다.
/// </summary>
public abstract class DapperQueryBase<TEntity, TDto>
{
    private readonly IDbConnection _connection;
    private readonly DapperSpecTranslator<TEntity>? _translator;
    private readonly string _tableAlias;
    private string? _cachedDefaultColumn;

    protected abstract string SelectSql { get; }
    protected abstract string CountSql { get; }
    protected abstract string DefaultOrderBy { get; }
    protected abstract Dictionary<string, string> AllowedSortColumns { get; }

    /// <summary>
    /// Specification → SQL WHERE 절 변환.
    /// DapperSpecTranslator를 주입받은 경우 기본 구현이 제공됩니다.
    /// 그렇지 않으면 서브클래스에서 반드시 오버라이드해야 합니다.
    /// </summary>
    protected virtual (string Where, DynamicParameters Params) BuildWhereClause(Specification<TEntity> spec)
    {
        if (_translator is not null)
            return _translator.Translate(spec, _tableAlias);
        throw new NotSupportedException(
            "Override BuildWhereClause or provide a DapperSpecTranslator via constructor.");
    }

    /// <summary>DB 방언별 Offset 페이지네이션 절. 서브클래스에서 오버라이드하여 SQL Server 등 지원.</summary>
    protected virtual string PaginationClause => "LIMIT @PageSize OFFSET @Skip";

    /// <summary>DB 방언별 Keyset 페이지네이션 절. 서브클래스에서 오버라이드하여 SQL Server 등 지원.</summary>
    protected virtual string CursorPaginationClause => "LIMIT @PageSize";

    protected DapperQueryBase(IDbConnection connection)
    {
        _connection = connection;
        _tableAlias = "";
    }

    protected DapperQueryBase(
        IDbConnection connection, DapperSpecTranslator<TEntity> translator, string tableAlias = "")
        : this(connection)
    {
        _translator = translator;
        _tableAlias = tableAlias;
    }

    private string DefaultColumn => _cachedDefaultColumn ??=
        DefaultOrderBy[..DefaultOrderBy.IndexOf(' ')];

    public virtual FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec, PageRequest page, SortExpression sort)
    {
        return IO.liftAsync(async () =>
        {
            var (where, parameters) = BuildWhereClause(spec);
            var orderBy = BuildOrderByClause(sort);
            parameters.Add("PageSize", page.PageSize);
            parameters.Add("Skip", page.Skip);

            var sql = $"{CountSql} {where};\n{SelectSql} {where} {orderBy} {PaginationClause}";
            using var multi = await _connection.QueryMultipleAsync(sql, parameters);
            var totalCount = await multi.ReadSingleAsync<int>();
            var items = await multi.ReadAsync<TDto>();

            return Fin.Succ(new PagedResult<TDto>(
                items.ToList(), totalCount, page.Page, page.PageSize));
        });
    }

    public virtual FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
        Specification<TEntity> spec, CursorPageRequest cursor, SortExpression sort)
    {
        return IO.liftAsync(async () =>
        {
            var (where, parameters) = BuildWhereClause(spec);
            var sortColumn = ResolveSortColumn(sort);
            var orderBy = BuildOrderByClause(sort);

            if (cursor.After is not null)
            {
                where = string.IsNullOrEmpty(where)
                    ? $"WHERE {sortColumn} > @CursorValue"
                    : $"{where} AND {sortColumn} > @CursorValue";
                parameters.Add("CursorValue", cursor.After);
            }
            else if (cursor.Before is not null)
            {
                where = string.IsNullOrEmpty(where)
                    ? $"WHERE {sortColumn} < @CursorValue"
                    : $"{where} AND {sortColumn} < @CursorValue";
                parameters.Add("CursorValue", cursor.Before);
            }

            parameters.Add("PageSize", cursor.PageSize + 1);

            var sql = $"{SelectSql} {where} {orderBy} {CursorPaginationClause}";
            var items = (await _connection.QueryAsync<TDto>(sql, parameters)).ToList();

            var hasMore = items.Count > cursor.PageSize;
            if (hasMore) items.RemoveAt(items.Count - 1);

            var fieldName = ResolveSortFieldName(sort);
            string? nextCursor = hasMore && items.Count > 0 ? GetCursorValue(items[^1], fieldName) : null;
            string? prevCursor = items.Count > 0 ? GetCursorValue(items[0], fieldName) : null;

            return Fin.Succ(new CursorPagedResult<TDto>(items, nextCursor, prevCursor, hasMore));
        });
    }

    private string ResolveSortColumn(SortExpression sort)
    {
        if (sort.IsEmpty) return DefaultColumn;
        return AllowedSortColumns.GetValueOrDefault(sort.Fields[0].FieldName, DefaultColumn);
    }

    private string ResolveSortFieldName(SortExpression sort)
    {
        if (!sort.IsEmpty) return sort.Fields[0].FieldName;
        foreach (var kvp in AllowedSortColumns)
            if (kvp.Value == DefaultColumn) return kvp.Key;
        return DefaultColumn;
    }

    private static readonly ConcurrentDictionary<string, PropertyInfo?> CursorPropertyCache =
        new(StringComparer.OrdinalIgnoreCase);

    protected virtual string? GetCursorValue(TDto item, string fieldName)
    {
        if (item is null) return null;
        var prop = CursorPropertyCache.GetOrAdd(fieldName,
            name => typeof(TDto).GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
        return prop?.GetValue(item)?.ToString();
    }

    private string BuildOrderByClause(SortExpression sort)
    {
        if (sort.IsEmpty)
            return $"ORDER BY {DefaultOrderBy}";

        var clauses = new List<string>();
        foreach (var field in sort.Fields)
        {
            var column = AllowedSortColumns.GetValueOrDefault(field.FieldName, DefaultColumn);
            var direction = field.Direction == SortDirection.Descending ? "DESC" : "ASC";
            clauses.Add($"{column} {direction}");
        }

        return $"ORDER BY {string.Join(", ", clauses)}";
    }

    public virtual async IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec,
        SortExpression sort,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (where, parameters) = BuildWhereClause(spec);
        var orderBy = BuildOrderByClause(sort);

        var sql = $"{SelectSql} {where} {orderBy}";
        var dbConnection = _connection as DbConnection
            ?? throw new InvalidOperationException("Stream requires a DbConnection instance.");
        await foreach (var item in dbConnection.QueryUnbufferedAsync<TDto>(sql, parameters)
            .WithCancellation(cancellationToken))
        {
            yield return item;
        }
    }

    protected static DynamicParameters Params(params (string Name, object Value)[] values)
    {
        var p = new DynamicParameters();
        foreach (var (name, value) in values)
            p.Add(name, value);
        return p;
    }
}
