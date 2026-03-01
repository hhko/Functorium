using Shouldly;

namespace InterfaceSegregationAndVariance.Tests.Unit;

public class InterfaceSegregationTests
{
    [Fact]
    public void Create_ReturnsContainer_WhenValueProvided()
    {
        // Act
        var actual = Container.Create("Hello");

        // Assert
        actual.Value.ShouldBe("Hello");
        actual.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CreateEmpty_ReturnsInvalidContainer_WhenCalled()
    {
        // Act
        var actual = Container.CreateEmpty();

        // Assert
        actual.Value.ShouldBe(string.Empty);
        actual.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateViaConstraint_ReturnsInstance_WhenStaticAbstractUsed()
    {
        // Act
        var actual = CreateDefault<Container>();

        // Assert
        actual.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ReadableAssign_Succeeds_WhenContainerAssignedToIReadable()
    {
        // Arrange
        var container = Container.Create("Test");

        // Act
        IReadable<string> readable = container;

        // Assert
        readable.Value.ShouldBe("Test");
        readable.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void MutableContainer_ReadsAndWrites_WhenUsedAsReadWrite()
    {
        // Arrange
        var sut = new MutableContainer<string>();

        // Act
        sut.Write("Hello");

        // Assert
        sut.Value.ShouldBe("Hello");
        sut.IsValid.ShouldBeTrue();
    }

    private static T CreateDefault<T>() where T : IFactory<T> => T.CreateEmpty();
}
