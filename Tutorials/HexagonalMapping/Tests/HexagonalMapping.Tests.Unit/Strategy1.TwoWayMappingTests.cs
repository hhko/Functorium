using HexagonalMapping.Domain.Model;
using HexagonalMapping.Strategy1.TwoWayMapping.Adapter.In.Rest;
using HexagonalMapping.Strategy1.TwoWayMapping.Adapter.Out.Persistence;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HexagonalMapping.Tests.Unit;

/// <summary>
/// Two-Way Mapping 전략 테스트
/// </summary>
public class Strategy1_TwoWayMappingTests
{
    [Fact]
    public void ProductMapper_ToDomain_ShouldConvertEntityToDomain()
    {
        // Arrange
        var entity = new ProductEntity
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Price = 29.99m,
            Currency = "USD"
        };

        // Act
        var domain = ProductMapper.ToDomain(entity);

        // Assert
        domain.Id.Value.ShouldBe(entity.Id);
        domain.Name.ShouldBe(entity.Name);
        domain.Price.Amount.ShouldBe(entity.Price);
        domain.Price.Currency.ShouldBe(entity.Currency);
    }

    [Fact]
    public void ProductMapper_ToEntity_ShouldConvertDomainToEntity()
    {
        // Arrange
        var domain = Product.Create("Test Product", 29.99m, "EUR");

        // Act
        var entity = ProductMapper.ToEntity(domain);

        // Assert
        entity.Id.ShouldBe(domain.Id.Value);
        entity.Name.ShouldBe(domain.Name);
        entity.Price.ShouldBe(domain.Price.Amount);
        entity.Currency.ShouldBe(domain.Price.Currency);
    }

    [Fact]
    public void ProductDtoMapper_ToDto_ShouldConvertDomainToDto()
    {
        // Arrange
        var domain = Product.Create("Test Product", 49.99m, "USD");

        // Act
        var dto = ProductDtoMapper.ToDto(domain);

        // Assert
        dto.Id.ShouldBe(domain.Id.Value);
        dto.Name.ShouldBe(domain.Name);
        dto.Price.ShouldBe(domain.Price.Amount);
        dto.Currency.ShouldBe(domain.Price.Currency);
        dto.FormattedPrice.ShouldBe("49.99 USD");
    }

    [Fact]
    public void ProductDtoMapper_ToDomain_ShouldConvertRequestToDomain()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "New Product",
            Price = 99.99m,
            Currency = "GBP"
        };

        // Act
        var domain = ProductDtoMapper.ToDomain(request);

        // Assert
        domain.Name.ShouldBe(request.Name);
        domain.Price.Amount.ShouldBe(request.Price);
        domain.Price.Currency.ShouldBe("GBP");
    }

    [Fact]
    public async Task ProductRepository_ShouldPersistAndRetrieveProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ProductDbContext(options);
        var repository = new ProductRepository(context);
        var product = Product.Create("Integration Test Product", 199.99m, "USD");

        // Act
        await repository.AddAsync(product);
        var retrieved = await repository.GetByIdAsync(product.Id);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(product.Id);
        retrieved.Name.ShouldBe(product.Name);
        retrieved.Price.Amount.ShouldBe(product.Price.Amount);
        retrieved.Price.Currency.ShouldBe(product.Price.Currency);
    }

    [Fact]
    public async Task ProductRepository_ShouldUpdateProduct()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new ProductDbContext(options);
        var repository = new ProductRepository(context);
        var product = Product.Create("Original Name", 100m, "USD");
        await repository.AddAsync(product);

        // Act
        product.Rename("Updated Name");
        product.UpdatePrice(150m, "EUR");
        await repository.UpdateAsync(product);
        var updated = await repository.GetByIdAsync(product.Id);

        // Assert
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Updated Name");
        updated.Price.Amount.ShouldBe(150m);
        updated.Price.Currency.ShouldBe("EUR");
    }
}
