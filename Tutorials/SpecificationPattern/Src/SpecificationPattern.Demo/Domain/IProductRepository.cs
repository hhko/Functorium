using Functorium.Domains.Specifications;

namespace SpecificationPattern.Demo.Domain;

/// <summary>
/// Product 조회를 위한 Repository 포트.
/// Repository는 WHERE(어디서)를, Specification은 WHAT(무엇을) 담당합니다.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Specification 조건을 만족하는 모든 Product를 반환합니다.
    /// </summary>
    IEnumerable<Product> FindAll(Specification<Product> spec);

    /// <summary>
    /// Specification 조건을 만족하는 Product가 존재하는지 확인합니다.
    /// </summary>
    bool Exists(Specification<Product> spec);
}
