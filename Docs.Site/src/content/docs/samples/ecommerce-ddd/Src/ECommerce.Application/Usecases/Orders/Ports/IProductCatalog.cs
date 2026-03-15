using Functorium.Domains.Observabilities;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Application.Usecases.Orders.Ports;

/// <summary>
/// 교차 Aggregate 상품 카탈로그 Port
/// Order 등 다른 Aggregate에서 상품 검증용으로 사용.
/// 배치 조회 전용 — 단일 WHERE IN 쿼리로 N+1 라운드트립을 방지합니다.
/// </summary>
public interface IProductCatalog : IObservablePort
{
    /// <summary>
    /// 복수 상품의 가격을 일괄 조회합니다.
    /// 존재하지 않는 상품 ID는 결과에 포함되지 않으므로, 반환 건수로 존재 여부를 검증할 수 있습니다.
    /// </summary>
    FinT<IO, Seq<(ProductId Id, Money Price)>> GetPricesForProducts(IReadOnlyList<ProductId> productIds);
}
