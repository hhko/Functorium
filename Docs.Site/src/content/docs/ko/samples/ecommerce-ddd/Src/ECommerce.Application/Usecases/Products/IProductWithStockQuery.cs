using Functorium.Applications.Queries;
using ECommerce.Domain.AggregateRoots.Products;

namespace ECommerce.Application.Usecases.Products.Ports;

/// <summary>
/// Product + Inventory JOIN 읽기 전용 어댑터 포트.
/// 상품과 재고 정보를 결합하여 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IProductWithStockQuery : IQueryPort<Product, ProductWithStockDto> { }

public sealed record ProductWithStockDto(
    string ProductId,
    string Name,
    decimal Price,
    int StockQuantity);
