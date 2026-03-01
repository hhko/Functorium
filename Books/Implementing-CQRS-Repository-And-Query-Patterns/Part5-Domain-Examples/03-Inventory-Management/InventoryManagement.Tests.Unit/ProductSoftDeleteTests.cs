using InventoryManagement;

namespace InventoryManagement.Tests.Unit;

public sealed class ProductSoftDeleteTests
{
    // ─── Create ─────────────────────────────────────────

    [Fact]
    public void Create_ValidInput_ReturnsSucc()
    {
        var result = Product.Create("노트북", 1_500_000m, 10);

        result.IsSucc.ShouldBeTrue();
        var product = result.ThrowIfFail();
        product.Name.ShouldBe("노트북");
        product.Price.ShouldBe(1_500_000m);
        product.Stock.ShouldBe(10);
        product.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Create_EmptyName_ReturnsFail()
    {
        Product.Create("", 100m).IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_NegativePrice_ReturnsFail()
    {
        Product.Create("노트북", -1m).IsFail.ShouldBeTrue();
    }

    // ─── Soft Delete ────────────────────────────────────

    [Fact]
    public void Delete_ActiveProduct_SetsIsDeleted()
    {
        var product = Product.Create("노트북", 1_500_000m).ThrowIfFail();

        var result = product.Delete();

        result.IsSucc.ShouldBeTrue();
        product.IsDeleted.ShouldBeTrue();
        product.DeletedAt.IsSome.ShouldBeTrue();
    }

    [Fact]
    public void Delete_AlreadyDeleted_ReturnsFail()
    {
        var product = Product.Create("노트북", 1_500_000m).ThrowIfFail();
        product.Delete();

        var result = product.Delete();

        result.IsFail.ShouldBeTrue();
    }

    // ─── Restore ────────────────────────────────────────

    [Fact]
    public void Restore_DeletedProduct_ClearsDeletedAt()
    {
        var product = Product.Create("노트북", 1_500_000m).ThrowIfFail();
        product.Delete();

        var result = product.Restore();

        result.IsSucc.ShouldBeTrue();
        product.IsDeleted.ShouldBeFalse();
        product.DeletedAt.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Restore_ActiveProduct_ReturnsFail()
    {
        var product = Product.Create("노트북", 1_500_000m).ThrowIfFail();

        var result = product.Restore();

        result.IsFail.ShouldBeTrue();
    }

    // ─── AdjustStock ────────────────────────────────────

    [Fact]
    public void AdjustStock_ValidAmount_ReturnsSucc()
    {
        var product = Product.Create("노트북", 1_500_000m, 10).ThrowIfFail();

        var result = product.AdjustStock(20);

        result.IsSucc.ShouldBeTrue();
        product.Stock.ShouldBe(20);
    }

    [Fact]
    public void AdjustStock_NegativeAmount_ReturnsFail()
    {
        var product = Product.Create("노트북", 1_500_000m, 10).ThrowIfFail();

        var result = product.AdjustStock(-1);

        result.IsFail.ShouldBeTrue();
    }

    // ─── ActiveProductSpec ──────────────────────────────

    [Fact]
    public void ActiveProductSpec_ActiveProduct_ReturnsTrue()
    {
        var product = Product.Create("노트북", 1_500_000m).ThrowIfFail();
        var spec = new ActiveProductSpec();

        spec.IsSatisfiedBy(product).ShouldBeTrue();
    }

    [Fact]
    public void ActiveProductSpec_DeletedProduct_ReturnsFalse()
    {
        var product = Product.Create("노트북", 1_500_000m).ThrowIfFail();
        product.Delete();
        var spec = new ActiveProductSpec();

        spec.IsSatisfiedBy(product).ShouldBeFalse();
    }

    // ─── Domain Events ──────────────────────────────────

    [Fact]
    public void DeleteAndRestore_CollectsEvents()
    {
        var product = Product.Create("노트북", 1_500_000m).ThrowIfFail();
        product.Delete();
        product.Restore();

        product.DomainEvents.Count.ShouldBe(3);
        product.DomainEvents[0].ShouldBeOfType<Product.ProductCreatedEvent>();
        product.DomainEvents[1].ShouldBeOfType<Product.ProductDeletedEvent>();
        product.DomainEvents[2].ShouldBeOfType<Product.ProductRestoredEvent>();
    }
}
