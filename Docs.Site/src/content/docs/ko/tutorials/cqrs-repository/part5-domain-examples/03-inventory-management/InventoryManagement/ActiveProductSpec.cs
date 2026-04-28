using Functorium.Domains.Specifications;

namespace InventoryManagement;

/// <summary>
/// 삭제되지 않은 활성 상품을 필터링하는 Specification.
/// </summary>
public sealed class ActiveProductSpec : Specification<Product>
{
    public override bool IsSatisfiedBy(Product entity) => !entity.IsDeleted;
}
