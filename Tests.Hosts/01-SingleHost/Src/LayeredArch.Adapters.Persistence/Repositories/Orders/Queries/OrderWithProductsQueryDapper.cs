using System.Data;
using Dapper;
using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Domain.AggregateRoots.Orders;
using static Functorium.Adapters.Errors.AdapterErrorType;
using static LanguageExt.Prelude;

namespace LayeredArch.Adapters.Persistence.Repositories.Orders.Queries;

/// <summary>
/// Order + OrderLine + Product 3-table JOIN 쿼리 어댑터.
/// Dapper multi-mapping으로 단건 주문 + 상품명 포함 주문 라인을 조회합니다.
/// </summary>
[GenerateObservablePort]
public class OrderWithProductsQueryDapper : IOrderWithProductsQuery
{
    private const string Sql =
        "SELECT o.Id AS OrderId, o.CustomerId, o.TotalAmount, o.Status, o.CreatedAt, " +
        "ol.ProductId, p.Name AS ProductName, ol.Quantity, ol.UnitPrice, ol.LineTotal " +
        "FROM Orders o " +
        "INNER JOIN OrderLines ol ON ol.OrderId = o.Id " +
        "INNER JOIN Products p ON p.Id = ol.ProductId " +
        "WHERE o.Id = @OrderId";

    private readonly IDbConnection _connection;

    public string RequestCategory => "QueryAdapter";

    public OrderWithProductsQueryDapper(IDbConnection connection) => _connection = connection;

    public virtual FinT<IO, OrderWithProductsDto> GetById(OrderId id)
    {
        return IO.liftAsync(async () =>
        {
            var rows = await _connection.QueryAsync<OrderWithProductsRow>(
                Sql, new { OrderId = id.ToString() });

            var rowList = rows.ToList();
            if (rowList.Count == 0)
                return AdapterError.For<OrderWithProductsQueryDapper>(
                    new NotFound(), id.ToString(),
                    $"주문 ID '{id}'을(를) 찾을 수 없습니다");

            var first = rowList[0];
            var orderLines = toSeq(rowList.Select(r => new OrderLineWithProductDto(
                r.ProductId, r.ProductName, r.Quantity, r.UnitPrice, r.LineTotal)));

            return Fin.Succ(new OrderWithProductsDto(
                first.OrderId, first.CustomerId, orderLines,
                first.TotalAmount, first.Status, first.CreatedAt));
        });
    }

    private sealed record OrderWithProductsRow(
        string OrderId, string CustomerId, decimal TotalAmount, string Status, DateTime CreatedAt,
        string ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
}
