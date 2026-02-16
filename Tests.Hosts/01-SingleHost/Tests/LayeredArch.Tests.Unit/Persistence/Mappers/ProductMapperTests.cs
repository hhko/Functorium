using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.Entities;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Persistence.Mappers;

public class ProductMapperTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveAllFields()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Test Product").ThrowIfFail(),
            ProductDescription.Create("Test Description").ThrowIfFail(),
            Money.Create(99.99m).ThrowIfFail(),
            Quantity.Create(42).ThrowIfFail());

        // Act
        var actual = product.ToModel().ToDomain();

        // Assert
        actual.Id.ToString().ShouldBe(product.Id.ToString());
        ((string)actual.Name).ShouldBe(product.Name);
        ((string)actual.Description).ShouldBe(product.Description);
        ((decimal)actual.Price).ShouldBe(product.Price);
        ((int)actual.StockQuantity).ShouldBe(product.StockQuantity);
        actual.CreatedAt.ShouldBe(product.CreatedAt);
        actual.UpdatedAt.ShouldBe(product.UpdatedAt);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTags()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Tagged Product").ThrowIfFail(),
            ProductDescription.Create("With tags").ThrowIfFail(),
            Money.Create(50m).ThrowIfFail(),
            Quantity.Create(10).ThrowIfFail());

        product.AddTag(Tag.Create(TagName.Create("tag-a").ThrowIfFail()));
        product.AddTag(Tag.Create(TagName.Create("tag-b").ThrowIfFail()));
        product.ClearDomainEvents();

        // Act
        var actual = product.ToModel().ToDomain();

        // Assert
        actual.Tags.Count.ShouldBe(2);
        actual.Tags.Select(t => (string)t.Name).ShouldBe(
            product.Tags.Select(t => (string)t.Name), ignoreOrder: true);
    }

    [Fact]
    public void RoundTrip_ShouldClearDomainEvents()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Event Product").ThrowIfFail(),
            ProductDescription.Create("Desc").ThrowIfFail(),
            Money.Create(10m).ThrowIfFail(),
            Quantity.Create(1).ThrowIfFail());

        product.AddTag(Tag.Create(TagName.Create("tag").ThrowIfFail()));

        // Act
        var actual = product.ToModel().ToDomain();

        // Assert - 복원 과정에서 발행된 이벤트는 제거되어야 함
        actual.DomainEvents.ShouldBeEmpty();
    }
}
