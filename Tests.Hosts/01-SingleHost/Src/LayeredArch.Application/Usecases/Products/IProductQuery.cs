using Functorium.Applications.Queries;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Ports;

/// <summary>
/// Product 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IProductQuery : IQueryPort<Product, ProductSummaryDto> { }

public sealed record ProductSummaryDto(
    string ProductId,
    string Name,
    decimal Price);