using LayeredArch.Domain.AggregateRoots.Products;
using LayeredArch.Domain.SharedKernel.Entities;
using LayeredArch.Domain.SharedKernel.Events;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.Products;

public class ProductTests
{
    private static Product CreateSampleProduct(
        string name = "Test Product",
        string description = "Test Description",
        decimal price = 100m)
    {
        return Product.Create(
            ProductName.Create(name).ThrowIfFail(),
            ProductDescription.Create(description).ThrowIfFail(),
            Money.Create(price).ThrowIfFail());
    }

    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Act
        var sut = CreateSampleProduct();

        // Assert
        sut.Id.ShouldNotBe(default);
        sut.DomainEvents.ShouldContain(e => e is Product.CreatedEvent);
    }

    [Fact]
    public void Create_ShouldSetProperties()
    {
        // Act
        var sut = CreateSampleProduct(name: "Laptop", description: "Good laptop", price: 1500m);

        // Assert
        ((string)sut.Name).ShouldBe("Laptop");
        ((string)sut.Description).ShouldBe("Good laptop");
        ((decimal)sut.Price).ShouldBe(1500m);
    }

    [Fact]
    public void Update_ShouldPublishUpdatedEvent()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.ClearDomainEvents();

        var newName = ProductName.Create("Updated Name").ThrowIfFail();
        var newDescription = ProductDescription.Create("Updated Desc").ThrowIfFail();
        var newPrice = Money.Create(200m).ThrowIfFail();

        // Act
        sut.Update(newName, newDescription, newPrice);

        // Assert
        sut.DomainEvents.ShouldContain(e => e is Product.UpdatedEvent);
        ((string)sut.Name).ShouldBe("Updated Name");
    }

    [Fact]
    public void AddTag_ShouldAddTagAndPublishEvent()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.ClearDomainEvents();
        var tagName = TagName.Create("sale").ThrowIfFail();
        var tag = Tag.Create(tagName);

        // Act
        sut.AddTag(tag);

        // Assert
        sut.Tags.ShouldContain(t => t.Id == tag.Id);
        sut.DomainEvents.ShouldContain(e => e is TagAssignedEvent);
    }

    [Fact]
    public void AddTag_ShouldNotDuplicate_WhenSameTagAddedTwice()
    {
        // Arrange
        var sut = CreateSampleProduct();
        var tagName = TagName.Create("sale").ThrowIfFail();
        var tag = Tag.Create(tagName);
        sut.AddTag(tag);

        // Act
        sut.AddTag(tag);

        // Assert
        sut.Tags.Count.ShouldBe(1);
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = ProductId.New();
        var name = ProductName.Create("Restored Product").ThrowIfFail();
        var description = ProductDescription.Create("Restored Desc").ThrowIfFail();
        var price = Money.Create(500m).ThrowIfFail();
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var sut = Product.CreateFromValidated(id, name, description, price, createdAt, updatedAt);

        // Assert
        sut.Id.ShouldBe(id);
        ((string)sut.Name).ShouldBe("Restored Product");
        ((decimal)sut.Price).ShouldBe(500m);
        sut.CreatedAt.ShouldBe(createdAt);
        sut.UpdatedAt.ShouldBe(updatedAt);
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveTag_ShouldRemoveTagAndPublishEvent()
    {
        // Arrange
        var sut = CreateSampleProduct();
        var tagName = TagName.Create("sale").ThrowIfFail();
        var tag = Tag.Create(tagName);
        sut.AddTag(tag);
        sut.ClearDomainEvents();

        // Act
        sut.RemoveTag(tag.Id);

        // Assert
        sut.Tags.ShouldBeEmpty();
        sut.DomainEvents.ShouldContain(e => e is TagRemovedEvent);
    }
}
