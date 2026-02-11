using LayeredArch.Domain.SharedKernel.Entities;
using LayeredArch.Domain.SharedKernel.ValueObjects;

namespace LayeredArch.Tests.Unit.Domain.SharedKernel;

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
    public void CreateFromValidated_ShouldReturnTag_WithGivenIdAndName()
    {
        // Arrange
        var id = TagId.New();
        var name = TagName.Create("sale").ThrowIfFail();

        // Act
        var actual = Tag.CreateFromValidated(id, name);

        // Assert
        actual.Id.ShouldBe(id);
        actual.Name.ShouldBe(name);
    }
}
