using ECommerce.Domain.AggregateRoots.Products;
using ECommerce.Domain.AggregateRoots.Tags;
using ECommerce.Domain.SharedModels.ValueObjects;

namespace ECommerce.Tests.Unit.Domain.Products;

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
        var actual = sut.Update(newName, newDescription, newPrice);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        sut.DomainEvents.ShouldContain(e => e is Product.UpdatedEvent);
        ((string)sut.Name).ShouldBe("Updated Name");
    }

    [Fact]
    public void AssignTag_ShouldAddTagIdAndPublishEvent()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.ClearDomainEvents();
        var tagId = TagId.New();

        // Act
        sut.AssignTag(tagId);

        // Assert
        sut.TagIds.ShouldContain(tagId);
        var assignedEvent = sut.DomainEvents.OfType<Product.TagAssignedEvent>().ShouldHaveSingleItem();
        assignedEvent.ProductId.ShouldBe(sut.Id);
        assignedEvent.TagId.ShouldBe(tagId);
    }

    [Fact]
    public void AssignTag_ShouldNotDuplicate_WhenSameTagAssignedTwice()
    {
        // Arrange
        var sut = CreateSampleProduct();
        var tagId = TagId.New();
        sut.AssignTag(tagId);

        // Act
        sut.AssignTag(tagId);

        // Assert
        sut.TagIds.Count.ShouldBe(1);
    }

    [Fact]
    public void CreateFromValidated_ShouldRestoreWithoutDomainEvent()
    {
        // Arrange
        var id = ProductId.New();
        var name = ProductName.Create("Restored Product").ThrowIfFail();
        var description = ProductDescription.Create("Restored Desc").ThrowIfFail();
        var price = Money.Create(500m).ThrowIfFail();
        var tagIds = new[] { TagId.New(), TagId.New() };
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var deletedAt = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var deletedBy = "admin@test.com";

        // Act
        var sut = Product.CreateFromValidated(
            id, name, description, price, tagIds, createdAt,
            Some(updatedAt), Some(deletedAt), Some(deletedBy));

        // Assert
        sut.Id.ShouldBe(id);
        ((string)sut.Name).ShouldBe("Restored Product");
        ((decimal)sut.Price).ShouldBe(500m);
        sut.TagIds.Count.ShouldBe(2);
        sut.CreatedAt.ShouldBe(createdAt);
        sut.UpdatedAt.ShouldBe(Some(updatedAt));
        sut.DeletedAt.ShouldBe(Some(deletedAt));
        sut.DeletedBy.ShouldBe(Some(deletedBy));
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UnassignTag_ShouldRemoveTagIdAndPublishEvent()
    {
        // Arrange
        var sut = CreateSampleProduct();
        var tagId = TagId.New();
        sut.AssignTag(tagId);
        sut.ClearDomainEvents();

        // Act
        sut.UnassignTag(tagId);

        // Assert
        sut.TagIds.ShouldBeEmpty();
        var unassignedEvent = sut.DomainEvents.OfType<Product.TagUnassignedEvent>().ShouldHaveSingleItem();
        unassignedEvent.ProductId.ShouldBe(sut.Id);
        unassignedEvent.TagId.ShouldBe(tagId);
    }

    [Fact]
    public void Delete_ShouldSetDeletedAtAndDeletedBy()
    {
        // Arrange
        var sut = CreateSampleProduct();

        // Act
        sut.Delete("admin@test.com");

        // Assert
        sut.DeletedAt.IsSome.ShouldBeTrue();
        sut.DeletedBy.ShouldBe(Some("admin@test.com"));
    }

    [Fact]
    public void Delete_ShouldPublishDeletedEvent()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.ClearDomainEvents();

        // Act
        sut.Delete("admin@test.com");

        // Assert
        var deletedEvent = sut.DomainEvents.OfType<Product.DeletedEvent>().ShouldHaveSingleItem();
        deletedEvent.ProductId.ShouldBe(sut.Id);
        deletedEvent.DeletedBy.ShouldBe("admin@test.com");
    }

    [Fact]
    public void Delete_ShouldBeIdempotent_WhenAlreadyDeleted()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.Delete("admin@test.com");
        sut.ClearDomainEvents();

        // Act
        sut.Delete("other@test.com");

        // Assert
        sut.DomainEvents.ShouldBeEmpty();
        sut.DeletedBy.ShouldBe(Some("admin@test.com"));
    }

    [Fact]
    public void Restore_ShouldClearDeletedAtAndDeletedBy()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.Delete("admin@test.com");

        // Act
        sut.Restore();

        // Assert
        sut.DeletedAt.IsNone.ShouldBeTrue();
        sut.DeletedBy.IsNone.ShouldBeTrue();
    }

    [Fact]
    public void Restore_ShouldPublishRestoredEvent()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.Delete("admin@test.com");
        sut.ClearDomainEvents();

        // Act
        sut.Restore();

        // Assert
        var restoredEvent = sut.DomainEvents.OfType<Product.RestoredEvent>().ShouldHaveSingleItem();
        restoredEvent.ProductId.ShouldBe(sut.Id);
    }

    [Fact]
    public void Restore_ShouldBeIdempotent_WhenNotDeleted()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.ClearDomainEvents();

        // Act
        sut.Restore();

        // Assert
        sut.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Update_ReturnsFail_WhenProductIsDeleted()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.Delete("admin@test.com");

        var newName = ProductName.Create("New Name").ThrowIfFail();
        var newDescription = ProductDescription.Create("New Desc").ThrowIfFail();
        var newPrice = Money.Create(200m).ThrowIfFail();

        // Act
        var actual = sut.Update(newName, newDescription, newPrice);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Restore_ShouldAllowUpdate_AfterDeleteAndRestore()
    {
        // Arrange
        var sut = CreateSampleProduct();
        sut.Delete("admin@test.com");
        sut.Restore();

        var newName = ProductName.Create("Restored Name").ThrowIfFail();
        var newDescription = ProductDescription.Create("Restored Desc").ThrowIfFail();
        var newPrice = Money.Create(300m).ThrowIfFail();

        // Act
        var actual = sut.Update(newName, newDescription, newPrice);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        ((string)sut.Name).ShouldBe("Restored Name");
    }
}
