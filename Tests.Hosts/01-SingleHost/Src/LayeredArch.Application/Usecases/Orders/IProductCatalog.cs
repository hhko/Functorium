using Functorium.Domains.Observabilities;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Orders.Ports;

/// <summary>
/// 교차 Aggregate 상품 카탈로그 Port
/// Order 등 다른 Aggregate에서 상품 검증용으로 사용
/// </summary>
public interface IProductCatalog : IPort
{
    /// <summary>
    /// 상품 존재 여부 확인
    /// </summary>
    FinT<IO, bool> ExistsById(ProductId productId);

    /// <summary>
    /// 상품 가격 조회
    /// </summary>
    FinT<IO, Money> GetPrice(ProductId productId);
}
