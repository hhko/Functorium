using EcommerceFiltering.Domain.Specifications;
using EcommerceFiltering.Domain.ValueObjects;
using EcommerceFiltering.Infrastructure;

namespace EcommerceFiltering.Tests.Unit;

public class RepositoryTests
{
    private readonly InMemoryProductRepository _repository = new(SampleProducts.All);

    [Fact]
    public void FindAll_ShouldReturnMatchingProducts_WhenCategorySpecified()
    {
        // Arrange
        var spec = new ProductCategorySpec(new Category("전자기기"));

        // Act
        var results = _repository.FindAll(spec).ToList();

        // Assert
        results.Count.ShouldBe(4);
    }

    [Fact]
    public void FindAll_ShouldReturnEmpty_WhenNoProductsMatch()
    {
        // Arrange
        var spec = new ProductCategorySpec(new Category("존재하지않는카테고리"));

        // Act
        var results = _repository.FindAll(spec).ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void Exists_ShouldReturnTrue_WhenProductNameExists()
    {
        // Arrange
        var spec = new ProductNameUniqueSpec(new ProductName("맥북 프로 16인치"));

        // Act & Assert
        _repository.Exists(spec).ShouldBeTrue();
    }

    [Fact]
    public void Exists_ShouldReturnFalse_WhenProductNameDoesNotExist()
    {
        // Arrange
        var spec = new ProductNameUniqueSpec(new ProductName("갤럭시 탭"));

        // Act & Assert
        _repository.Exists(spec).ShouldBeFalse();
    }

    [Fact]
    public void FindAll_ShouldReturnFilteredProducts_WhenCompositeSpecUsed()
    {
        // Arrange: 재고 있음 AND 50만원 이하
        var spec = new ProductInStockSpec()
            & new ProductPriceRangeSpec(new Money(0m), new Money(500_000m));

        // Act
        var results = _repository.FindAll(spec).ToList();

        // Assert
        results.ShouldNotBeEmpty();
        results.ShouldAllBe(p => (int)p.Stock > 0 && (decimal)p.Price <= 500_000m);
    }
}
