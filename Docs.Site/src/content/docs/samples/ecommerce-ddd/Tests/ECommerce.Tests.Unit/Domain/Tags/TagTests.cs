using ECommerce.Domain.AggregateRoots.Tags;

namespace ECommerce.Tests.Unit.Domain.Tags;

public class TagTests
{
    [Fact]
    public void Create_ShouldReturnTag_WithNewId()
    {
        // Arrange
        var name = TagName.Create("electronics").ThrowIfFail();

        // Act
        var actual = Tag.Create(name);

        // Assert
        actual.ShouldNotBeNull();
        actual.Id.ShouldNotBe(default);
        actual.Name.ShouldBe(name);
    }

    [Fact]
    public void Create_ShouldPublishCreatedEvent()
    {
        // Arrange
        var name = TagName.Create("electronics").ThrowIfFail();

        // Act
        var actual = Tag.Create(name);

        // Assert
        var createdEvent = actual.DomainEvents.OfType<Tag.CreatedEvent>().ShouldHaveSingleItem();
        createdEvent.TagId.ShouldBe(actual.Id);
        createdEvent.TagName.ShouldBe(name);
    }

    [Fact]
    public void CreateFromValidated_ShouldReturnTag_WithGivenIdAndName()
    {
        // Arrange
        var id = TagId.New();
        var name = TagName.Create("sale").ThrowIfFail();
        var createdAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var actual = Tag.CreateFromValidated(id, name, createdAt, Option<DateTime>.None);

        // Assert
        actual.Id.ShouldBe(id);
        actual.Name.ShouldBe(name);
        actual.CreatedAt.ShouldBe(createdAt);
        actual.UpdatedAt.IsNone.ShouldBeTrue();
        actual.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Rename_ShouldChangeName()
    {
        // Arrange
        var sut = Tag.Create(TagName.Create("old-name").ThrowIfFail());
        var newName = TagName.Create("new-name").ThrowIfFail();

        // Act
        sut.Rename(newName);

        // Assert
        sut.Name.ShouldBe(newName);
    }

    [Fact]
    public void Rename_ShouldPublishRenamedEvent()
    {
        // Arrange
        var oldName = TagName.Create("old-name").ThrowIfFail();
        var newName = TagName.Create("new-name").ThrowIfFail();
        var sut = Tag.Create(oldName);

        // Act
        sut.Rename(newName);

        // Assert
        var renamedEvent = sut.DomainEvents.OfType<Tag.RenamedEvent>().ShouldHaveSingleItem();
        renamedEvent.TagId.ShouldBe(sut.Id);
        renamedEvent.OldName.ShouldBe(oldName);
        renamedEvent.NewName.ShouldBe(newName);
    }

    [Fact]
    public void Rename_ShouldUpdateTimestamp()
    {
        // Arrange
        var sut = Tag.Create(TagName.Create("old-name").ThrowIfFail());
        var newName = TagName.Create("new-name").ThrowIfFail();

        // Act
        sut.Rename(newName);

        // Assert
        sut.UpdatedAt.IsSome.ShouldBeTrue();
    }
}
