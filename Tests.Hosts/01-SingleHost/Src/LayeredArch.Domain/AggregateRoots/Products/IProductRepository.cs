using Functorium.Domains.Repositories;

namespace LayeredArch.Domain.AggregateRoots.Products;

/// <summary>
/// 상품 리포지토리 인터페이스 (Command 전용)
/// </summary>
public interface IProductRepository : IRepository<Product, ProductId>
{
    /// <summary>
    /// Specification 기반 존재 여부 확인.
    /// </summary>
    FinT<IO, bool> Exists(Specification<Product> spec);
}
