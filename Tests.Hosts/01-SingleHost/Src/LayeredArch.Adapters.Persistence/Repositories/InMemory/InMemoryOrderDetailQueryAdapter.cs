using Functorium.Adapters.Errors;
using Functorium.Adapters.SourceGenerators;
using LayeredArch.Application.Usecases.Orders.Dtos;
using LayeredArch.Application.Usecases.Orders.Ports;
using LayeredArch.Domain.AggregateRoots.Orders;
using static Functorium.Adapters.Errors.AdapterErrorType;

namespace LayeredArch.Adapters.Persistence.Repositories.InMemory;

/// <summary>
/// InMemory 기반 Order 단건 조회 읽기 전용 어댑터.
/// InMemoryOrderRepository의 정적 저장소에서 데이터를 가져온 후 DTO로 프로젝션합니다.
/// </summary>
[GeneratePipeline]
public class InMemoryOrderDetailQueryAdapter : IOrderDetailQueryAdapter
{
    public string RequestCategory => "QueryAdapter";

    public virtual FinT<IO, OrderDetailDto> GetById(OrderId id)
    {
        return IO.lift(() =>
        {
            if (InMemoryOrderRepository.Orders.TryGetValue(id, out var order))
            {
                return Fin.Succ(new OrderDetailDto(
                    order.Id.ToString(),
                    order.ProductId.ToString(),
                    order.Quantity,
                    order.UnitPrice,
                    order.TotalAmount,
                    order.ShippingAddress,
                    order.CreatedAt));
            }

            return AdapterError.For<InMemoryOrderDetailQueryAdapter>(
                new NotFound(),
                id.ToString(),
                $"주문 ID '{id}'을(를) 찾을 수 없습니다");
        });
    }
}
