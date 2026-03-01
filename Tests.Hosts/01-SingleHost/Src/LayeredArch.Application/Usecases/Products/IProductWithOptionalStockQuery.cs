using Functorium.Applications.Queries;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Ports;

/// <summary>
/// Product + Optional Inventory LEFT JOIN 읽기 전용 어댑터 포트.
/// 재고 없는 상품도 포함하여 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IProductWithOptionalStockQuery : IQueryPort<Product, ProductWithOptionalStockDto> { }

public sealed record ProductWithOptionalStockDto(
    string ProductId,
    string Name,
    decimal Price,
    int? StockQuantity);
