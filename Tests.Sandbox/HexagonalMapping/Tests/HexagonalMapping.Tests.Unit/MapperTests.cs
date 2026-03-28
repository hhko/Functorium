using HexagonalMapping.Domain.Model;
using Shouldly;
using Xunit;

namespace HexagonalMapping.Tests.Unit;

/// <summary>
/// Domain 엔티티 테스트
/// </summary>
public class DomainTests
{
    [Fact]
    public void Product_Create_ShouldCreateWithValidInput()
    {
        // Act
        var product = Product.Create("Test Product", 29.99m, "usd");

        // Assert
        product.Name.ShouldBe("Test Product");
        product.Price.Amount.ShouldBe(29.99m);
        product.Price.Currency.ShouldBe("USD"); // Uppercase 변환 확인
        product.Id.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Product_Create_ShouldThrowForEmptyName()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Product.Create("", 10m, "USD"));
        Should.Throw<ArgumentException>(() => Product.Create("   ", 10m, "USD"));
    }

    [Fact]
    public void Product_Reconstitute_ShouldRecreateFromPersistence()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var product = Product.Reconstitute(id, "Stored Product", 99.99m, "EUR");

        // Assert
        product.Id.Value.ShouldBe(id);
        product.Name.ShouldBe("Stored Product");
        product.Price.Amount.ShouldBe(99.99m);
        product.Price.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Product_UpdatePrice_ShouldUpdatePriceCorrectly()
    {
        // Arrange
        var product = Product.Create("Test", 10m, "USD");

        // Act
        product.UpdatePrice(20m, "EUR");

        // Assert
        product.Price.Amount.ShouldBe(20m);
        product.Price.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Product_Rename_ShouldUpdateNameCorrectly()
    {
        // Arrange
        var product = Product.Create("Original", 10m, "USD");

        // Act
        product.Rename("Updated");

        // Assert
        product.Name.ShouldBe("Updated");
    }

    [Fact]
    public void Money_Create_ShouldCreateWithValidInput()
    {
        // Act
        var money = Money.Create(100m, "eur");

        // Assert
        money.Amount.ShouldBe(100m);
        money.Currency.ShouldBe("EUR"); // Uppercase 변환 확인
    }

    [Fact]
    public void Money_Create_ShouldThrowForNegativeAmount()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() => Money.Create(-10m, "USD"));
    }

    [Fact]
    public void Money_Add_ShouldAddSameCurrency()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "USD");

        // Act
        var result = money1.Add(money2);

        // Assert
        result.Amount.ShouldBe(150m);
        result.Currency.ShouldBe("USD");
    }

    [Fact]
    public void Money_Add_ShouldThrowForDifferentCurrency()
    {
        // Arrange
        var money1 = Money.Create(100m, "USD");
        var money2 = Money.Create(50m, "EUR");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => money1.Add(money2));
    }

    [Fact]
    public void ProductId_New_ShouldCreateUniqueIds()
    {
        // Act
        var id1 = ProductId.New();
        var id2 = ProductId.New();

        // Assert
        id1.ShouldNotBe(id2);
        id1.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void ProductId_From_ShouldCreateFromGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var id = ProductId.From(guid);

        // Assert
        id.Value.ShouldBe(guid);
    }
}
