using Functorium.Applications.Queries;
using LayeredArch.Domain.AggregateRoots.Products;

namespace LayeredArch.Application.Usecases.Products.Ports;

/// <summary>
/// Product 단건 조회용 읽기 전용 어댑터 포트.
/// Aggregate 재구성 없이 DB에서 DTO로 직접 프로젝션합니다.
/// </summary>
public interface IProductDetailQuery : IQueryAdapter
{
    FinT<IO, ProductDetailDto> GetById(ProductId id);
}

public sealed record ProductDetailDto(
    string ProductId,
    string Name,
    string Description,
    decimal Price,
    DateTime CreatedAt,
    Option<DateTime> UpdatedAt);
