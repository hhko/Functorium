using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Application.Usecases.Customers.Ports;
using LayeredArch.Domain.AggregateRoots.Customers;
using static Functorium.Adapters.Errors.AdapterErrorKind;
using static LanguageExt.Prelude;

using LayeredArch.Adapters.Persistence.Repositories.Customers.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Orders.Repositories;
using LayeredArch.Adapters.Persistence.Repositories.Products.Repositories;

namespace LayeredArch.Adapters.Persistence.Repositories.Customers.Queries;

/// <summary>
/// InMemory 기반 Customer → Order → OrderLine → Product 4-table JOIN 읽기 전용 어댑터.
/// 특정 고객의 모든 주문과 각 주문의 상품명을 포함합니다.
/// </summary>
[GenerateObservablePort]
public class CustomerOrdersQueryInMemory : ICustomerOrdersQuery
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, CustomerOrdersDto> GetByCustomerId(CustomerId id)
    {
        return IO.lift(() =>
        {
            if (!CustomerRepositoryInMemory.Customers.TryGetValue(id, out var customer))
            {
                return AdapterError.For<CustomerOrdersQueryInMemory>(
                    new NotFound(), id.ToString(),
                    $"고객 ID '{id}'을(를) 찾을 수 없습니다");
            }

            var productLookup = ProductRepositoryInMemory.Products;
            var orders = toSeq(OrderRepositoryInMemory.Orders.Values
                .Where(o => o.CustomerId.Equals(id))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o =>
                {
                    var orderLines = toSeq(o.OrderLines.Select(l =>
                    {
                        var productName = productLookup.TryGetValue(l.ProductId, out var product)
                            ? (string)product.Name : "Unknown";
                        return new CustomerOrderLineDto(
                            l.ProductId.ToString(), productName,
                            l.Quantity, l.UnitPrice, l.LineTotal);
                    }));

                    return new CustomerOrderDto(
                        o.Id.ToString(), orderLines,
                        o.TotalAmount, o.Status, o.CreatedAt);
                }));

            return Fin.Succ(new CustomerOrdersDto(
                customer.Id.ToString(), customer.Name, orders));
        });
    }
}
