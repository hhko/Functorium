using Functorium.Domains.Repositories;
using Functorium.Domains.Specifications;
using LanguageExt;

namespace RepositoryInterface;

/// <summary>
/// Product 전용 Repository 인터페이스.
/// IRepository의 8개 CRUD 메서드에 도메인 특화 메서드를 추가합니다.
/// </summary>
public interface IProductRepository : IRepository<Product, ProductId>
{
    /// <summary>
    /// Specification 조건을 만족하는 Product가 존재하는지 확인합니다.
    /// </summary>
    FinT<IO, bool> Exists(Specification<Product> spec);
}
