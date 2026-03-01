using System.Data;
using Dapper;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Adapters.Persistence.Repositories.Dapper;

/// <summary>
/// Customer → Order → OrderLine → Product 4-table JOIN 쿼리 어댑터.
/// 특정 고객의 모든 주문과 각 주문의 상품명을 Dapper로 조회합니다.
/// </summary>
[GenerateObservablePort]
public class DapperCustomerOrdersQuery : ICustomerOrdersQuery
{
    private const string CustomerSql =
        "SELECT Id AS CustomerId, Name AS CustomerName FROM Customers WHERE Id = @CustomerId";

    private const string OrderLinesSql =
        "SELECT o.Id AS OrderId, o.TotalAmount, o.Status, o.CreatedAt, " +
        "ol.ProductId, p.Name AS ProductName, ol.Quantity, ol.UnitPrice, ol.LineTotal " +
        "FROM Orders o " +
        "INNER JOIN OrderLines ol ON ol.OrderId = o.Id " +
        "INNER JOIN Products p ON p.Id = ol.ProductId " +
        "WHERE o.CustomerId = @CustomerId " +
        "ORDER BY o.CreatedAt DESC";

    private readonly IDbConnection _connection;

    public string RequestCategory => "QueryAdapter";

    public DapperCustomerOrdersQuery(IDbConnection connection) => _connection = connection;

    public virtual FinT<IO, CustomerOrdersDto> GetByCustomerId(CustomerId id)
    {
        return IO.liftAsync(async () =>
        {
            var customer = await _connection.QuerySingleOrDefaultAsync<CustomerRow>(
                CustomerSql, new { CustomerId = id.ToString() });

            if (customer is null)
                return AdapterError.For<DapperCustomerOrdersQuery>(
                    new NotFound(), id.ToString(),
                    $"고객 ID '{id}'을(를) 찾을 수 없습니다");

            var rows = (await _connection.QueryAsync<OrderLineRow>(
                OrderLinesSql, new { CustomerId = id.ToString() })).ToList();

            var orders = toSeq(rows
                .GroupBy(r => r.OrderId)
                .Select(g =>
                {
                    var first = g.First();
                    var lines = toSeq(g.Select(r => new CustomerOrderLineDto(
                        r.ProductId, r.ProductName, r.Quantity, r.UnitPrice, r.LineTotal)));
                    return new CustomerOrderDto(
                        first.OrderId, lines, first.TotalAmount, first.Status, first.CreatedAt);
                }));

            return Fin.Succ(new CustomerOrdersDto(
                customer.CustomerId, customer.CustomerName, orders));
        });
    }

    private sealed record CustomerRow(string CustomerId, string CustomerName);

    private sealed record OrderLineRow(
        string OrderId, decimal TotalAmount, string Status, DateTime CreatedAt,
        string ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
}
