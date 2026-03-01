using System.Data;
using Dapper;
using Functorium.Adapters.SourceGenerators;
using Functorium.Adapters.Repositories;
using Functorium.Domains.Specifications;
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;

namespace LayeredArch.Adapters.Persistence.Repositories.Dapper;

/// <summary>
/// Customer + Order LEFT JOIN + GROUP BY 집계 쿼리 어댑터.
/// 고객별 주문 요약 통계를 산출합니다.
/// </summary>
[GenerateObservablePort]
public class DapperCustomerOrderSummaryQuery
    : DapperQueryBase<Customer, CustomerOrderSummaryDto>, ICustomerOrderSummaryQuery
{
    public string RequestCategory => "QueryAdapter";

    protected override string SelectSql =>
        "SELECT c.Id AS CustomerId, c.Name AS CustomerName, " +
        "COUNT(o.Id) AS OrderCount, " +
        "COALESCE(SUM(o.TotalAmount), 0) AS TotalSpent, " +
        "MAX(o.CreatedAt) AS LastOrderDate " +
        "FROM Customers c LEFT JOIN Orders o ON o.CustomerId = c.Id " +
        "GROUP BY c.Id, c.Name";
    protected override string CountSql =>
        "SELECT COUNT(*) FROM Customers c";
    protected override string DefaultOrderBy => "CustomerName ASC";
    protected override Dictionary<string, string> AllowedSortColumns { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["CustomerName"] = "CustomerName",
            ["OrderCount"] = "OrderCount",
            ["TotalSpent"] = "TotalSpent",
            ["LastOrderDate"] = "LastOrderDate"
        };

    public DapperCustomerOrderSummaryQuery(IDbConnection connection) : base(connection) { }

    protected override (string, DynamicParameters) BuildWhereClause(Specification<Customer> spec)
        => spec switch
        {
            { IsAll: true } => ("", new DynamicParameters()),
            _ => throw new NotSupportedException(
                $"Specification '{spec.GetType().Name}'은 Dapper QueryAdapter에서 지원되지 않습니다.")
        };
}
