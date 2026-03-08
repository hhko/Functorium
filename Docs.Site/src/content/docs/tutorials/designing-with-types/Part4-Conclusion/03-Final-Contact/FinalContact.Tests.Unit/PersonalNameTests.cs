using FinalContact;

namespace FinalContact.Tests.Unit;

/// <summary>
/// PersonalName 복합 값 객체 테스트
///
/// 테스트 목적:
/// 1. 유효한 이름으로 생성 성공
/// 2. MiddleInitial null 허용
/// 3. 빈 이름으로 생성 실패
/// </summary>
[Trait("Part4-Conclusion", "03-FinalContact")]
public class PersonalNameTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsValid()
    {
        // Act
        var actual = PersonalName.Create("HyungHo", "Ko", "J");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsSuccess_WhenMiddleInitialIsNull()
    {
        // Act
        var actual = PersonalName.Create("HyungHo", "Ko");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenFirstNameIsEmpty()
    {
        // Act
        var actual = PersonalName.Create("", "Ko");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenLastNameIsEmpty()
    {
        // Act
        var actual = PersonalName.Create("HyungHo", "");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
