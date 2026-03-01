using Functorium.Domains.Specifications;
using TestingStrategies;
using TestingStrategies.Specifications;

namespace TestingStrategies.Tests.Unit;

public class MockProductRepository : IProductRepository
{
    public Specification<Product>? LastSpec { get; private set; }
    private readonly List<Product> _products;

    public MockProductRepository(IEnumerable<Product> products)
        => _products = products.ToList();

    public IEnumerable<Product> FindAll(Specification<Product> spec)
    {
        LastSpec = spec;
        return _products.Where(spec.IsSatisfiedBy);
    }

    public bool Exists(Specification<Product> spec)
    {
        LastSpec = spec;
        return _products.Any(spec.IsSatisfiedBy);
    }
}

[Trait("Part4-TestingStrategies", "Level3")]
public class Level3_UsecaseTests
{
    private static readonly List<Product> SampleProducts =
    [
        new("Laptop", 1500, 10, "Electronics"),
        new("Mouse", 25, 50, "Electronics"),
        new("Desk", 300, 0, "Furniture"),
    ];

    [Fact]
    public void Exists_ShouldReturnTrue_WhenProductMatchesSpec()
    {
        // Arrange
        var repository = new MockProductRepository(SampleProducts);
        var spec = new ProductNameUniqueSpec("Laptop");

        // Act
        var exists = repository.Exists(spec);

        // Assert
        exists.ShouldBeTrue();
        repository.LastSpec.ShouldNotBeNull();
    }

    [Fact]
    public void Exists_ShouldReturnFalse_WhenNoProductMatchesSpec()
    {
        // Arrange
        var repository = new MockProductRepository(SampleProducts);
        var spec = new ProductNameUniqueSpec("Tablet");

        // Act
        var exists = repository.Exists(spec);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public void FindAll_ShouldReturnFilteredProducts_WhenSpecApplied()
    {
        // Arrange
        var repository = new MockProductRepository(SampleProducts);
        var spec = new ProductCategorySpec("Electronics") & new ProductInStockSpec();

        // Act
        var results = repository.FindAll(spec).ToList();

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldAllBe(p => p.Category == "Electronics" && p.Stock > 0);
        repository.LastSpec.ShouldNotBeNull();
    }

    [Fact]
    public void FindAll_ShouldReturnAll_WhenAllSpecUsed()
    {
        // Arrange
        var repository = new MockProductRepository(SampleProducts);
        var spec = Specification<Product>.All;

        // Act
        var results = repository.FindAll(spec).ToList();

        // Assert
        results.Count.ShouldBe(SampleProducts.Count);
    }
}
