using DDDContactExt;

namespace DDDContactExt.Tests.Unit;

/// <summary>
/// PersonalName 복합 VO 테스트 (향상: null 입력)
/// </summary>
[Trait("Part4-Conclusion", "05-DDDContactExt")]
public class PersonalNameTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = PersonalName.Create("HyungHo", "Ko");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsSuccess_WithMiddleInitial()
    {
        // Act
        var actual = PersonalName.Create("HyungHo", "Ko", "J");

        // Assert
        var name = actual.ThrowIfFail();
        name.MiddleInitial.ShouldBe("J");
    }

    [Fact]
    public void Create_ReturnsFail_WhenFirstNameNull()
    {
        // Act
        var actual = PersonalName.Create(null, "Ko");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenLastNameNull()
    {
        // Act
        var actual = PersonalName.Create("HyungHo", null);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenFirstNameEmpty()
    {
        // Act
        var actual = PersonalName.Create("", "Ko");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenLastNameEmpty()
    {
        // Act
        var actual = PersonalName.Create("HyungHo", "");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_CreatesDirectly()
    {
        // Arrange
        var firstName = String50.CreateFromValidated("HyungHo");
        var lastName = String50.CreateFromValidated("Ko");

        // Act
        var actual = PersonalName.CreateFromValidated(firstName, lastName, "J");

        // Assert
        actual.FirstName.ShouldBe(firstName);
        actual.LastName.ShouldBe(lastName);
        actual.MiddleInitial.ShouldBe("J");
    }

    #region ValueObject Equality

    [Fact]
    public void Equals_ReturnsTrue_WhenSameValues()
    {
        // Arrange
        var name1 = PersonalName.Create("John", "Doe", "A").ThrowIfFail();
        var name2 = PersonalName.Create("John", "Doe", "A").ThrowIfFail();

        // Assert
        name1.ShouldBe(name2);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDifferentFirstName()
    {
        // Arrange
        var name1 = PersonalName.Create("John", "Doe").ThrowIfFail();
        var name2 = PersonalName.Create("Jane", "Doe").ThrowIfFail();

        // Assert
        name1.ShouldNotBe(name2);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenMiddleInitialDiffers()
    {
        // Arrange
        var name1 = PersonalName.Create("John", "Doe").ThrowIfFail();
        var name2 = PersonalName.Create("John", "Doe", "A").ThrowIfFail();

        // Assert
        name1.ShouldNotBe(name2);
    }

    [Fact]
    public void GetHashCode_ReturnsSame_WhenEqual()
    {
        // Arrange
        var name1 = PersonalName.Create("John", "Doe", "A").ThrowIfFail();
        var name2 = PersonalName.Create("John", "Doe", "A").ThrowIfFail();

        // Assert
        name1.GetHashCode().ShouldBe(name2.GetHashCode());
    }

    #endregion
}
