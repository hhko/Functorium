namespace TestingStrategies.Tests.Unit;

/// <summary>
/// 비교 가능성 테스트 패턴 검증
///
/// 테스트 목적:
/// 1. IComparable 구현 검증
/// 2. 비교 연산자 검증
/// 3. 정렬 동작 검증
/// </summary>
[Trait("Part4-Testing-Strategies", "ComparabilityPatternTests")]
public class ComparabilityPatternTests
{
    #region CompareTo 테스트

    [Fact]
    public void CompareTo_ReturnsNegative_WhenLeftIsLess()
    {
        // Arrange
        var age20 = Age.CreateFromValidated(20);
        var age30 = Age.CreateFromValidated(30);

        // Act
        var actual = age20.CompareTo(age30);

        // Assert
        actual.ShouldBeLessThan(0);
    }

    [Fact]
    public void CompareTo_ReturnsPositive_WhenLeftIsGreater()
    {
        // Arrange
        var age30 = Age.CreateFromValidated(30);
        var age20 = Age.CreateFromValidated(20);

        // Act
        var actual = age30.CompareTo(age20);

        // Assert
        actual.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_ReturnsZero_WhenValuesAreEqual()
    {
        // Arrange
        var age1 = Age.CreateFromValidated(25);
        var age2 = Age.CreateFromValidated(25);

        // Act
        var actual = age1.CompareTo(age2);

        // Assert
        actual.ShouldBe(0);
    }

    [Fact]
    public void CompareTo_ReturnsPositive_WhenComparingWithNull()
    {
        // Arrange
        var age = Age.CreateFromValidated(25);

        // Act
        var actual = age.CompareTo(null);

        // Assert
        actual.ShouldBeGreaterThan(0);
    }

    #endregion

    #region 비교 연산자 테스트

    [Fact]
    public void LessThan_ReturnsTrue_WhenLeftIsSmaller()
    {
        // Arrange
        var age20 = Age.CreateFromValidated(20);
        var age30 = Age.CreateFromValidated(30);

        // Act & Assert
        (age20 < age30).ShouldBeTrue();
    }

    [Fact]
    public void GreaterThan_ReturnsTrue_WhenLeftIsLarger()
    {
        // Arrange
        var age30 = Age.CreateFromValidated(30);
        var age20 = Age.CreateFromValidated(20);

        // Act & Assert
        (age30 > age20).ShouldBeTrue();
    }

    [Fact]
    public void LessThanOrEqual_ReturnsTrue_WhenValuesAreEqual()
    {
        // Arrange
        var age1 = Age.CreateFromValidated(25);
        var age2 = Age.CreateFromValidated(25);

        // Act & Assert
        (age1 <= age2).ShouldBeTrue();
    }

    [Fact]
    public void GreaterThanOrEqual_ReturnsTrue_WhenValuesAreEqual()
    {
        // Arrange
        var age1 = Age.CreateFromValidated(25);
        var age2 = Age.CreateFromValidated(25);

        // Act & Assert
        (age1 >= age2).ShouldBeTrue();
    }

    #endregion

    #region 정렬 테스트

    [Fact]
    public void Sort_OrdersValuesAscending_WhenArrayIsSorted()
    {
        // Arrange
        var ages = new[]
        {
            Age.CreateFromValidated(30),
            Age.CreateFromValidated(10),
            Age.CreateFromValidated(20),
            Age.CreateFromValidated(40)
        };

        // Act
        Array.Sort(ages);

        // Assert
        ages[0].Value.ShouldBe(10);
        ages[1].Value.ShouldBe(20);
        ages[2].Value.ShouldBe(30);
        ages[3].Value.ShouldBe(40);
    }

    [Fact]
    public void Sort_HandlesEqualValues_WhenArrayContainsDuplicates()
    {
        // Arrange
        var ages = new[]
        {
            Age.CreateFromValidated(25),
            Age.CreateFromValidated(25),
            Age.CreateFromValidated(25)
        };

        // Act
        Array.Sort(ages);

        // Assert
        ages.ShouldAllBe(a => a.Value == 25);
    }

    #endregion
}
