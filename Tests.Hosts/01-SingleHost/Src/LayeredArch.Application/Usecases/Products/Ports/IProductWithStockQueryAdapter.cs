using Functorium.Applications.Queries;
using LayeredArch.Application.Usecases.Products.Dtos;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Ports;

/// <summary>
/// Product + Inventory JOIN 읽기 전용 어댑터 포트.
/// 상품과 재고 정보를 결합하여 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IProductWithStockQueryAdapter : IQueryAdapter<Product, ProductWithStockDto> { }
