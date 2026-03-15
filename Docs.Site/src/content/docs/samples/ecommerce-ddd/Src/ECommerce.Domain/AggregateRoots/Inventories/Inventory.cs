using Functorium.Domains.Entities;
using Functorium.Domains.Errors;
using ECommerce.Domain.AggregateRoots.Products;
using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace ECommerce.Domain.AggregateRoots.Inventories;

/// <summary>
/// 재고 도메인 모델 (Aggregate Root)
/// Product에서 분리된 재고 관리 전용 Aggregate.
/// 고빈도 변경(주문마다 DeductStock)에 대한 낙관적 동시성 제어를 지원합니다.
/// InventoryId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class Inventory : AggregateRoot<InventoryId>, IAuditable, IConcurrencyAware
{
    #region Error Types

    public sealed record InsufficientStock : DomainErrorType.Custom;

    #endregion

    #region Domain Events

    /// <summary>
    /// 재고 생성 이벤트
    /// </summary>
    public sealed record CreatedEvent(InventoryId InventoryId, ProductId ProductId, Quantity StockQuantity) : DomainEvent;

    /// <summary>
    /// 재고 차감 이벤트
    /// </summary>
    public sealed record StockDeductedEvent(InventoryId InventoryId, ProductId ProductId, Quantity Quantity) : DomainEvent;

    /// <summary>
    /// 재고 추가 이벤트
    /// </summary>
    public sealed record StockAddedEvent(InventoryId InventoryId, ProductId ProductId, Quantity Quantity) : DomainEvent;

    #endregion

    // 참조 ID (Product Aggregate와의 연결)
    public ProductId ProductId { get; private set; }

    // Value Object 속성
    public Quantity StockQuantity { get; private set; }

    // 낙관적 동시성 제어
    public byte[] RowVersion { get; private set; } = [];

    // Audit 속성
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // 내부 생성자: 이미 검증된 VO를 받음
    private Inventory(
        InventoryId id,
        ProductId productId,
        Quantity stockQuantity)
        : base(id)
    {
        ProductId = productId;
        StockQuantity = stockQuantity;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 이미 검증된 Value Object를 직접 받음
    /// Application Layer에서 VO 생성 후 호출
    /// </summary>
    public static Inventory Create(
        ProductId productId,
        Quantity stockQuantity)
    {
        var inventory = new Inventory(InventoryId.New(), productId, stockQuantity);
        inventory.AddDomainEvent(new CreatedEvent(inventory.Id, productId, stockQuantity));
        return inventory;
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Inventory CreateFromValidated(
        InventoryId id,
        ProductId productId,
        Quantity stockQuantity,
        byte[] rowVersion,
        DateTime createdAt,
        Option<DateTime> updatedAt)
    {
        return new Inventory(id, productId, stockQuantity)
        {
            RowVersion = rowVersion,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    /// <summary>
    /// 재고를 차감합니다.
    /// </summary>
    public Fin<Unit> DeductStock(Quantity quantity)
    {
        if (quantity > StockQuantity)
            return DomainError.For<Inventory, int>(
                new InsufficientStock(),
                currentValue: StockQuantity,
                message: $"Insufficient stock. Current: {StockQuantity}, Requested: {quantity}");

        StockQuantity = StockQuantity.Subtract(quantity);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new StockDeductedEvent(Id, ProductId, quantity));
        return unit;
    }

    /// <summary>
    /// 재고를 추가합니다.
    /// </summary>
    public Inventory AddStock(Quantity quantity)
    {
        StockQuantity = StockQuantity.Add(quantity);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new StockAddedEvent(Id, ProductId, quantity));
        return this;
    }
}
