using LanguageExt;

namespace FunctoriumFramework.Tests.Unit;

/// <summary>
/// Age 값 객체 테스트 (ComparableSimpleValueObject 기반)
///
/// 테스트 목적:
/// 1. 값 객체 생성 검증 (Create 메서드)
/// 2. 동등성 비교 검증 (Equals, GetHashCode)
/// 3. 비교 가능성 검증 (CompareTo, 비교 연산자)
/// 4. 정렬 검증 (Array.Sort)
/// </summary>
[Trait("Part4-Functorium-Framework", "AgeTests")]
public class AgeTests
{
    #region 생성 테스트

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    public void Create_ReturnsSuccess_WhenAgeIsValid(int ageValue)
    {
        // Act
        var actual = Age.Create(ageValue);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: age => ((int)age).ShouldBe(ageValue),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(int.MinValue)]
    public void Create_ReturnsFail_WhenAgeIsNegative(int ageValue)
    {
        // Act
        var actual = Age.Create(ageValue);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Age cannot be negative")
        );
    }

    [Theory]
    [InlineData(151)]
    [InlineData(200)]
    [InlineData(int.MaxValue)]
    public void Create_ReturnsFail_WhenAgeExceedsMaximum(int ageValue)
    {
        // Act
        var actual = Age.Create(ageValue);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("Age cannot exceed 150 years")
        );
    }

    [Fact]
    public void CreateFromValidated_CreatesAge_WithoutValidation()
    {
        // Arrange
        var ageValue = 25;

        // Act
        var actual = Age.CreateFromValidated(ageValue);

        // Assert
        ((int)actual).ShouldBe(ageValue);
    }

    #endregion

    #region 비교 가능성 테스트

    [Fact]
    public void CompareTo_ReturnsNegative_WhenAgeIsLess()
    {
        // Arrange
        var age20 = Age.CreateFromValidated(20);
        var age30 = Age.CreateFromValidated(30);

        // Act & Assert
        age20.CompareTo(age30).ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_ReturnsPositive_WhenAgeIsGreater()
    {
        // Arrange
        var age30 = Age.CreateFromValidated(30);
        var age20 = Age.CreateFromValidated(20);

        // Act & Assert
        age30.CompareTo(age20).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_ReturnsZero_WhenAgesAreEqual()
    {
        // Arrange
        var age1 = Age.CreateFromValidated(25);
        var age2 = Age.CreateFromValidated(25);

        // Act & Assert
        age1.CompareTo(age2).ShouldBe(0);
    }

    [Fact]
    public void LessThanOperator_ReturnsTrue_WhenLeftIsLess()
    {
        // Arrange
        var age20 = Age.CreateFromValidated(20);
        var age30 = Age.CreateFromValidated(30);

        // Act & Assert
        (age20 < age30).ShouldBeTrue();
    }

    [Fact]
    public void GreaterThanOperator_ReturnsTrue_WhenLeftIsGreater()
    {
        // Arrange
        var age30 = Age.CreateFromValidated(30);
        var age20 = Age.CreateFromValidated(20);

        // Act & Assert
        (age30 > age20).ShouldBeTrue();
    }

    #endregion

    #region 정렬 테스트

    [Fact]
    public void Sort_OrdersAgesAscending_WhenArrayIsSorted()
    {
        // Arrange
        var ages = new[]
        {
            Age.CreateFromValidated(30),
            Age.CreateFromValidated(20),
            Age.CreateFromValidated(25)
        };

        // Act
        Array.Sort(ages);

        // Assert
        ((int)ages[0]).ShouldBe(20);
        ((int)ages[1]).ShouldBe(25);
        ((int)ages[2]).ShouldBe(30);
    }

    #endregion

    #region 암시적 변환 테스트

    [Fact]
    public void ImplicitConversion_ReturnsValue_WhenConvertedToInt()
    {
        // Arrange
        var age = Age.CreateFromValidated(25);

        // Act
        int value = age;

        // Assert
        value.ShouldBe(25);
    }

    #endregion
}
