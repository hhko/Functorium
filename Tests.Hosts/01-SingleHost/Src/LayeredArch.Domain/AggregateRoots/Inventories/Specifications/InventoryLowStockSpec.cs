using System.Linq.Expressions;
using Functorium.Domains.Specifications;

namespace LayeredArch.Domain.AggregateRoots.Inventories.Specifications;

/// <summary>
/// 재고 부족 Specification.
/// 재고가 Threshold 미만인 항목을 만족합니다.
/// Expression 기반으로 EF Core 자동 SQL 번역을 지원합니다.
/// </summary>
public sealed class InventoryLowStockSpec : ExpressionSpecification<Inventory>
{
    public Quantity Threshold { get; }

    public InventoryLowStockSpec(Quantity threshold)
    {
        Threshold = threshold;
    }

    public override Expression<Func<Inventory, bool>> ToExpression()
    {
        int threshold = Threshold;
        return inventory => (int)inventory.StockQuantity < threshold;
    }
}
