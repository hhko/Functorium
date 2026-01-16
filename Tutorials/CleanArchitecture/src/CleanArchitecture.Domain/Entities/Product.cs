using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.ValueObjects;

namespace CleanArchitecture.Domain.Entities;

public class Product : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    protected Product() { }

    public static Product Create(string name, string sku, Money price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Product name is required");

        if (name.Length > 200)
            throw new DomainException("Product name cannot exceed 200 characters");

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU is required");

        return new Product
        {
            Name = name,
            Sku = sku.ToUpperInvariant(),
            Price = price,
            StockQuantity = 0,
            IsActive = true
        };
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        StockQuantity += quantity;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        if (StockQuantity < quantity)
            throw new DomainException($"Insufficient stock. Available: {StockQuantity}");

        StockQuantity -= quantity;
    }

    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            throw new DomainException("Price must be greater than zero");

        Price = newPrice;
    }

    public void Deactivate()
    {
        if (!IsActive)
            throw new DomainException("Product is already inactive");

        IsActive = false;
    }
}
