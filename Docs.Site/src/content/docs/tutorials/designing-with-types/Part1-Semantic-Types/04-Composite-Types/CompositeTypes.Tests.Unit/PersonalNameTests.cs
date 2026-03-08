using CompositeTypes.ValueObjects;

namespace CompositeTypes.Tests.Unit;

/// <summary>
/// PersonalName 복합 값 객체 테스트
///
/// 테스트 목적:
/// 1. 유효한 이름으로 생성 성공
/// 2. 빈 이름으로 생성 실패 — 부분 유효성 불가
/// </summary>
[Trait("Part1-Semantic-Types", "04-CompositeTypes")]
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
