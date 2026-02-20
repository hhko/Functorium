using LayeredArch.Adapters.Persistence.Repositories.EfCore.Mappers;
using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedModels.Entities;
using LayeredArch.Domain.SharedModels.Entities.ValueObjects;
using LayeredArch.Domain.SharedModels.ValueObjects;

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
            Money.Create(99.99m).ThrowIfFail());

        // Act
        var actual = product.ToModel().ToDomain();

        // Assert
        actual.Id.ToString().ShouldBe(product.Id.ToString());
        ((string)actual.Name).ShouldBe(product.Name);
        ((string)actual.Description).ShouldBe(product.Description);
        ((decimal)actual.Price).ShouldBe(product.Price);
        actual.CreatedAt.ShouldBe(product.CreatedAt);
        actual.UpdatedAt.ShouldBe(product.UpdatedAt);
    }

    [Fact]
    public void RoundTrip_ShouldPreserveTagIds()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Tagged Product").ThrowIfFail(),
            ProductDescription.Create("With tags").ThrowIfFail(),
            Money.Create(50m).ThrowIfFail());

        var tagId1 = TagId.New();
        var tagId2 = TagId.New();
        product.AssignTag(tagId1);
        product.AssignTag(tagId2);
        product.ClearDomainEvents();

        // Act
        var actual = product.ToModel().ToDomain();

        // Assert
        actual.TagIds.Count.ShouldBe(2);
        actual.TagIds.Select(t => t.ToString()).ShouldBe(
            product.TagIds.Select(t => t.ToString()), ignoreOrder: true);
    }

    [Fact]
    public void RoundTrip_ShouldNotProduceDomainEvents()
    {
        // Arrange
        var product = Product.Create(
            ProductName.Create("Event Product").ThrowIfFail(),
            ProductDescription.Create("Desc").ThrowIfFail(),
            Money.Create(10m).ThrowIfFail());

        product.AssignTag(TagId.New());

        // Act
        var actual = product.ToModel().ToDomain();

        // Assert - 복원 과정에서 이벤트가 발생하지 않아야 함
        actual.DomainEvents.ShouldBeEmpty();
    }
}
