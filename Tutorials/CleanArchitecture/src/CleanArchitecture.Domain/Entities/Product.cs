using CleanArchitecture.Domain.ValueObjects;

using Functorium.Domains.Entities;
using Functorium.Domains.Errors;
using Functorium.Domains.SourceGenerators;
using Functorium.Domains.ValueObjects.Validations;

using LanguageExt;
using LanguageExt.Common;

using static Functorium.Domains.Errors.DomainErrorType;
using static LanguageExt.Prelude;

namespace CleanArchitecture.Domain.Entities;

[GenerateEntityId]
public class Product : Entity<ProductId>
{
    public string Name { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int StockQuantity { get; private set; }
    public bool IsActive { get; private set; }

    private Product() { }

    private Product(ProductId id, string name, string sku, Money price) : base(id)
    {
        Name = name;
        Sku = sku;
        Price = price;
        StockQuantity = 0;
        IsActive = true;
    }

    public static Fin<Product> Create(string name, string sku, Money price) =>
        Validate(name, sku).ToFin()
            .Map(v => new Product(ProductId.New(), v.Name, v.Sku, price));

    public static Product CreateFromValidated(ProductId id, string name, string sku, Money price, int stockQuantity, bool isActive) =>
        new(id, name, sku, price)
        {
            StockQuantity = stockQuantity,
            IsActive = isActive
        };

    private static Validation<Error, (string Name, string Sku)> Validate(string name, string sku)
    {
        var nameValidation = ValidateName(name);
        var skuValidation = ValidateSku(sku);

        return (nameValidation, skuValidation).Apply((n, s) => (Name: n, Sku: s));
    }

    private static Validation<Error, string> ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return DomainError.For<Product>(new Empty(), name ?? "", "Product name is required");
        if (name.Length > 200)
            return DomainError.For<Product>(new TooLong(200), name, "Product name cannot exceed 200 characters");
        return name;
    }

    private static Validation<Error, string> ValidateSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return DomainError.For<Product>(new Empty(), sku ?? "", "SKU is required");
        return sku.ToUpperInvariant();
    }

    public Fin<Unit> AddStock(int quantity)
    {
        if (quantity <= 0)
            return DomainError.For<Product, int>(new NotPositive(), quantity, "Quantity must be positive");
        StockQuantity += quantity;
        return unit;
    }

    public Fin<Unit> RemoveStock(int quantity)
    {
        if (quantity <= 0)
            return DomainError.For<Product, int>(new NotPositive(), quantity, "Quantity must be positive");
        if (StockQuantity < quantity)
            return DomainError.For<Product, int>(new BelowMinimum(quantity.ToString()), StockQuantity, $"Insufficient stock. Available: {StockQuantity}");
        StockQuantity -= quantity;
        return unit;
    }

    public Fin<Unit> UpdatePrice(Money newPrice)
    {
        if (newPrice.Amount <= 0)
            return DomainError.For<Product, decimal>(new NotPositive(), newPrice.Amount, "Price must be greater than zero");
        Price = newPrice;
        return unit;
    }

    public Fin<Unit> Deactivate()
    {
        if (!IsActive)
            return DomainError.For<Product>(new Custom("AlreadyInactive"), IsActive.ToString(), "Product is already inactive");
        IsActive = false;
        return unit;
    }
}
