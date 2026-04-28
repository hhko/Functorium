using Functorium.Domains.Entities;
using LanguageExt;

using static LanguageExt.Prelude;

namespace EntityInterfaces;

/// <summary>
/// IAuditable과 ISoftDeletableWithUser를 구현하는 상품 Entity.
/// 생성/수정 시각 추적과 소프트 삭제를 지원합니다.
/// </summary>
public sealed class Product : Entity<ProductId>, IAuditable, ISoftDeletableWithUser
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    // IAuditable
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    // ISoftDeletableWithUser
    public Option<DateTime> DeletedAt { get; private set; }
    public Option<string> DeletedBy { get; private set; }
    public bool IsDeleted => DeletedAt.IsSome;

    private Product(ProductId id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = None;
        DeletedAt = None;
        DeletedBy = None;
    }

    public static Product Create(string name, decimal price)
    {
        return new Product(ProductId.New(), name, price);
    }

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(string deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        DeletedAt = None;
        DeletedBy = None;
        UpdatedAt = DateTime.UtcNow;
    }
}
