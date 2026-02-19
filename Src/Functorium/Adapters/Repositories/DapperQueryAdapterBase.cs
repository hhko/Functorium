using System.Data;
using Dapper;
using Functorium.Applications.Queries;
using Functorium.Domains.Specifications;
using static LanguageExt.Prelude;

namespace Functorium.Adapters.Repositories;

/// <summary>
/// Dapper 기반 QueryAdapter의 공통 인프라.
/// 서브클래스는 SQL 선언(SelectSql, CountSql)과 WHERE 빌드만 담당합니다.
/// </summary>
public abstract class DapperQueryAdapterBase<TEntity, TDto>
{
    private readonly IDbConnection _connection;

    protected abstract string SelectSql { get; }
    protected abstract string CountSql { get; }
    protected abstract string DefaultOrderBy { get; }
    protected abstract Dictionary<string, string> AllowedSortColumns { get; }
    protected abstract (string Where, DynamicParameters Params) BuildWhereClause(Specification<TEntity> spec);

    protected DapperQueryAdapterBase(IDbConnection connection) => _connection = connection;

    public virtual FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec, PageRequest page, SortExpression sort)
    {
        return IO.liftAsync(async () =>
        {
            var (where, parameters) = BuildWhereClause(spec);
            var orderBy = BuildOrderByClause(sort);

            var totalCount = await _connection.ExecuteScalarAsync<int>(
                $"{CountSql} {where}", parameters);

            parameters.Add("PageSize", page.PageSize);
            parameters.Add("Skip", page.Skip);
            var items = await _connection.QueryAsync<TDto>(
                $"{SelectSql} {where} {orderBy} LIMIT @PageSize OFFSET @Skip", parameters);

            return Fin.Succ(new PagedResult<TDto>(
                toSeq(items), totalCount, page.Page, page.PageSize));
        });
    }

    private string BuildOrderByClause(SortExpression sort)
    {
        if (sort.IsEmpty)
            return $"ORDER BY {DefaultOrderBy}";

        var defaultColumn = DefaultOrderBy.Split(' ')[0];
        var clauses = new List<string>();
        foreach (var field in sort.Fields)
        {
            var column = AllowedSortColumns.GetValueOrDefault(field.FieldName, defaultColumn);
            var direction = field.Direction == SortDirection.Descending ? "DESC" : "ASC";
            clauses.Add($"{column} {direction}");
        }

        return $"ORDER BY {string.Join(", ", clauses)}";
    }

    protected static DynamicParameters Params(params (string Name, object Value)[] values)
    {
        var p = new DynamicParameters();
        foreach (var (name, value) in values)
            p.Add(name, value);
        return p;
    }
}
