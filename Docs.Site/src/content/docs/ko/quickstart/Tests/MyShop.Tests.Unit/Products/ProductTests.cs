using MyShop.Domain.AggregateRoots.Products;
using MyShop.Domain.AggregateRoots.Products.ValueObjects;
using Shouldly;

namespace MyShop.Tests.Unit.Products;

public class ProductTests
{
    [Fact]
    public void Create_ShouldRaise_CreatedEvent()
    {
        // Arrange
        var name = ProductName.Create("노트북").ThrowIfFail();
        var price = Money.Create(1500000m).ThrowIfFail();

        // Act
        var product = Product.Create(name, price);

        // Assert
        product.DomainEvents.ShouldContain(e => e is Product.CreatedEvent);
    }

    [Fact]
    public void Create_CreatedEvent_ShouldContainCorrectData()
    {
        // Arrange
        var name = ProductName.Create("노트북").ThrowIfFail();
        var price = Money.Create(1500000m).ThrowIfFail();

        // Act
        var product = Product.Create(name, price);

        // Assert
        var createdEvent = product.DomainEvents
            .OfType<Product.CreatedEvent>()
            .ShouldHaveSingleItem();

        createdEvent.ProductId.ShouldBe(product.Id);
        createdEvent.Name.ShouldBe(name);
        createdEvent.Price.ShouldBe(price);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Arrange
        var name = ProductName.Create("노트북").ThrowIfFail();
        var price = Money.Create(1500000m).ThrowIfFail();

        // Act
        var product = Product.Create(name, price);

        // Assert
        product.Name.ShouldBe(name);
        product.Price.ShouldBe(price);
        product.CreatedAt.ShouldNotBe(default);
    }
}
