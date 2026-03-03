namespace EcommerceDomain.Tests.Unit;

/// <summary>
/// ProductCode 값 객체 테스트
///
/// 학습 목표:
/// 1. 정규식 기반 형식 검증
/// 2. 파생 속성 검증 (Category, Number)
/// </summary>
[Trait("Part5-Ecommerce-Domain", "ProductCodeTests")]
public class ProductCodeTests
{
    #region 생성 테스트

    [Theory]
    [InlineData("EL-001234")]
    [InlineData("BK-999999")]
    [InlineData("sp-123456")]
    public void Create_ReturnsSuccess_WhenFormatIsValid(string code)
    {
        // Act
        var actual = ProductCode.Create(code);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_NormalizesToUpperCase()
    {
        // Act
        var actual = ProductCode.Create("el-001234");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: c => c.Code.ShouldBe("EL-001234"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("EL001234")]
    [InlineData("E-001234")]
    [InlineData("EL-12345")]
    [InlineData("EL-1234567")]
    public void Create_ReturnsFail_WhenFormatIsInvalid(string code)
    {
        // Act
        var actual = ProductCode.Create(code);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region 파생 속성 테스트

    [Fact]
    public void Category_ReturnsFirstTwoCharacters()
    {
        // Arrange
        var code = ProductCode.Create("EL-001234").Match(c => c, _ => null!);

        // Act & Assert
        code.Category.ShouldBe("EL");
    }

    [Fact]
    public void Number_ReturnsNumericPart()
    {
        // Arrange
        var code = ProductCode.Create("EL-001234").Match(c => c, _ => null!);

        // Act & Assert
        code.Number.ShouldBe("001234");
    }

    #endregion

    #region 동등성 테스트

    [Fact]
    public void Equals_ReturnsTrue_WhenCodesMatch()
    {
        // Arrange
        var code1 = ProductCode.Create("EL-001234").Match(c => c, _ => null!);
        var code2 = ProductCode.Create("el-001234").Match(c => c, _ => null!);

        // Act & Assert
        code1.Equals(code2).ShouldBeTrue();
    }

    #endregion
}
