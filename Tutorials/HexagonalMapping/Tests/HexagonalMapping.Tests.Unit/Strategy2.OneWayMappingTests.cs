using HexagonalMapping.Strategy2.OneWayMapping.Adapter.Out.Persistence;
using HexagonalMapping.Strategy2.OneWayMapping.Model;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HexagonalMapping.Tests.Unit;

/// <summary>
/// One-Way Mapping 전략 테스트
///
/// 핵심 검증 포인트:
/// - Domain과 Adapter 모두 IProductModel 구현
/// - Repository가 IProductModel 반환 (ProductEntity 직접 반환)
/// - Domain → Adapter 방향만 변환 필요
/// </summary>
public class Strategy2_OneWayMappingTests
{
    [Fact]
    public void Product_ShouldImplementIProductModel()
    {
        // Arrange & Act
        Product product = Product.Create("Test Product", 29.99m, "USD");

        // Assert
        product.ShouldBeAssignableTo<IProductModel>();
    }

    [Fact]
    public void ProductEntity_ShouldImplementIProductModel()
    {
        // Arrange & Act
        ProductEntity entity = new()
        {
            Id = Guid.NewGuid(),
            Name = "Test Entity",
            Price = 49.99m,
            Currency = "EUR"
        };

        // Assert
        entity.ShouldBeAssignableTo<IProductModel>();
    }

    [Fact]
    public void Product_FromModel_ShouldCreateFromInterface()
    {
        // Arrange - ProductEntity도 IProductModel을 구현
        IProductModel model = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = "From Entity",
            Price = 99.99m,
            Currency = "GBP"
        };

        // Act - 비즈니스 로직이 필요할 때만 변환
        Product product = Product.FromModel(model);

        // Assert
        product.Id.ShouldBe(model.Id);
        product.Name.ShouldBe(model.Name);
        product.Price.ShouldBe(model.Price);
        product.Currency.ShouldBe(model.Currency);
    }

    [Fact]
    public void ProductEntity_FromModel_ShouldCreateFromInterface()
    {
        // Arrange - Product도 IProductModel을 구현
        IProductModel model = Product.Create("From Domain", 149.99m, "USD");

        // Act - Domain → Adapter 변환 (One-Way의 유일한 변환 방향)
        ProductEntity entity = ProductEntity.FromModel(model);

        // Assert
        entity.Id.ShouldBe(model.Id);
        entity.Name.ShouldBe(model.Name);
        entity.Price.ShouldBe(model.Price);
        entity.Currency.ShouldBe(model.Currency);
    }

    [Fact]
    public async Task ProductRepository_ShouldAcceptIProductModel()
    {
        // Arrange
        DbContextOptions<ProductDbContext> options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using ProductDbContext context = new(options);
        ProductRepository repository = new(context);

        // Product가 IProductModel을 구현하므로 직접 전달 가능
        Product product = Product.Create("Interface Test", 79.99m, "USD");

        // Act
        await repository.AddAsync(product); // IProductModel로 전달
        IProductModel? retrieved = await repository.GetByIdAsync(product.Id);

        // Assert - Repository는 IProductModel 반환 (ProductEntity 직접 반환)
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(product.Id);
        retrieved.Name.ShouldBe(product.Name);
    }

    [Fact]
    public async Task ProductRepository_GetAll_ShouldReturnIProductModel()
    {
        // Arrange
        DbContextOptions<ProductDbContext> options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using ProductDbContext context = new(options);
        ProductRepository repository = new(context);

        await repository.AddAsync(Product.Create("Product 1", 10m, "USD"));
        await repository.AddAsync(Product.Create("Product 2", 20m, "USD"));

        // Act
        IReadOnlyList<IProductModel> products = await repository.GetAllAsync();

        // Assert - One-Way: Repository는 IProductModel 반환
        products.Count.ShouldBe(2);

        // One-Way의 특징: 실제로는 ProductEntity가 반환됨
        // (ProductEntity가 IProductModel을 구현하므로 직접 반환 가능)
        products.ShouldAllBe(p => p is IProductModel);
    }

    [Fact]
    public async Task OneWayMapping_AdapterToCore_ShouldReturnEntityDirectly()
    {
        // Arrange
        DbContextOptions<ProductDbContext> options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using ProductDbContext context = new(options);
        ProductRepository repository = new(context);

        Product original = Product.Create("One-Way Test", 59.99m, "USD");
        await repository.AddAsync(original);

        // Act - Repository가 IProductModel 반환
        IProductModel? retrieved = await repository.GetByIdAsync(original.Id);

        // Assert
        retrieved.ShouldNotBeNull();

        // One-Way Mapping의 핵심:
        // 반환된 객체는 실제로 ProductEntity (변환 없이 직접 반환)
        retrieved.ShouldBeOfType<ProductEntity>();

        // 비즈니스 로직이 필요하면 Product로 변환
        Product domainProduct = Product.FromModel(retrieved);
        domainProduct.ShouldBeOfType<Product>();
        domainProduct.FormattedPrice.ShouldNotBeNullOrEmpty();
    }
}
