using DDDContact;

namespace DDDContact.Tests.Unit;

/// <summary>
/// PersonalName 복합 VO 테스트
///
/// 테스트 목적:
/// 1. LINQ 모나딕 합성 Create 성공/실패
/// 2. CreateFromValidated 직접 생성
/// 3. MiddleInitial 지원
/// </summary>
[Trait("Part4-Conclusion", "04-DDDContact")]
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
}
