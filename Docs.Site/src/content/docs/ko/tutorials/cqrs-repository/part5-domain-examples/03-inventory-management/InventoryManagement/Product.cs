using Functorium.Domains.Entities;
using Functorium.Domains.Events;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace InventoryManagement;

/// <summary>
/// 재고 상품 Aggregate Root.
/// ISoftDeletable을 구현하여 논리 삭제를 지원합니다.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>, ISoftDeletable
{
    // ─── Domain Events ──────────────────────────────────
    public sealed record ProductCreatedEvent(ProductId ProductId, string Name) : DomainEvent;
    public sealed record ProductDeletedEvent(ProductId ProductId) : DomainEvent;
    public sealed record ProductRestoredEvent(ProductId ProductId) : DomainEvent;
    public sealed record StockAdjustedEvent(ProductId ProductId, int OldStock, int NewStock) : DomainEvent;

    // ─── Properties ─────────────────────────────────────
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public Option<DateTime> DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt.IsSome;

    private Product(ProductId id, string name, decimal price, int stock)
    {
        Id = id;
        Name = name;
        Price = price;
        Stock = stock;
        DeletedAt = None;
    }

    // ─── Factory ────────────────────────────────────────

    public static Fin<Product> Create(string name, decimal price, int stock = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.New("Name is required.");

        if (price < 0)
            return Error.New("Price cannot be negative.");

        if (stock < 0)
            return Error.New("Stock cannot be negative.");

        var product = new Product(ProductId.New(), name, price, stock);
        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name));
        return Fin.Succ(product);
    }

    // ─── Commands ───────────────────────────────────────

    public Fin<Unit> AdjustStock(int newStock)
    {
        if (newStock < 0)
            return Error.New("Stock cannot be negative.");

        var oldStock = Stock;
        Stock = newStock;
        AddDomainEvent(new StockAdjustedEvent(Id, oldStock, newStock));
        return unit;
    }

    public Fin<Unit> Delete()
    {
        if (IsDeleted)
            return Error.New("Product is already deleted.");

        DeletedAt = DateTime.UtcNow;
        AddDomainEvent(new ProductDeletedEvent(Id));
        return unit;
    }

    public Fin<Unit> Restore()
    {
        if (!IsDeleted)
            return Error.New("Product is not deleted.");

        DeletedAt = None;
        AddDomainEvent(new ProductRestoredEvent(Id));
        return unit;
    }
}
