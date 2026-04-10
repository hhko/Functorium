
namespace DesigningWithTypes.Tests.Unit;

/// <summary>
/// Value Object 팩토리 패턴 테스트 (향상: null, Trim, 소문자 정규화)
/// </summary>
[Trait("Sample", "DesigningWithTypes")]
public class ValueObjectTests
{
    #region String50

    [Fact]
    public void String50_Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = String50.Create("Hello");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void String50_Create_ReturnsFail_WhenNull()
    {
        // Act
        var actual = String50.Create(null);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void String50_Create_ReturnsFail_WhenEmpty()
    {
        // Act
        var actual = String50.Create("");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void String50_Create_ReturnsFail_WhenTooLong()
    {
        // Act
        var actual = String50.Create(new string('a', 51));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void String50_Create_TrimsWhitespace()
    {
        // Act
        var actual = String50.Create("  Hello  ").ThrowIfFail();

        // Assert
        string value = actual;
        value.ShouldBe("Hello");
    }

    [Fact]
    public void String50_CreateFromValidated_CreatesDirectly()
    {
        // Act
        var actual = String50.CreateFromValidated("Hello");

        // Assert
        string value = actual;
        value.ShouldBe("Hello");
    }

    [Fact]
    public void String50_ImplicitOperator_ReturnsValue()
    {
        // Arrange
        var sut = String50.Create("Hello").ThrowIfFail();

        // Act
        string actual = sut;

        // Assert
        actual.ShouldBe("Hello");
    }

    [Fact]
    public void String50_Create_ReturnsSuccess_WithExactly50Chars()
    {
        // Act
        var actual = String50.Create(new string('a', 50));

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void String50_Create_ReturnsFail_WhenWhitespaceOnly()
    {
        // Act
        var actual = String50.Create("   ");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region EmailAddress

    [Fact]
    public void EmailAddress_Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = EmailAddress.Create("user@example.com");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void EmailAddress_Create_ReturnsFail_WhenNull()
    {
        // Act
        var actual = EmailAddress.Create(null);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void EmailAddress_Create_ReturnsFail_WhenInvalid()
    {
        // Act
        var actual = EmailAddress.Create("not-an-email");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void EmailAddress_Create_NormalizesToLowerCase()
    {
        // Act
        var actual = EmailAddress.Create("User@Example.COM").ThrowIfFail();

        // Assert
        string value = actual;
        value.ShouldBe("user@example.com");
    }

    [Fact]
    public void EmailAddress_CreateFromValidated_CreatesDirectly()
    {
        // Act
        var actual = EmailAddress.CreateFromValidated("user@example.com");

        // Assert
        string value = actual;
        value.ShouldBe("user@example.com");
    }

    #endregion

    #region StateCode

    [Fact]
    public void StateCode_Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = StateCode.Create("IL");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void StateCode_Create_ReturnsFail_WhenNull()
    {
        // Act
        var actual = StateCode.Create(null);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void StateCode_Create_ReturnsFail_WhenInvalid()
    {
        // Act
        var actual = StateCode.Create("Illinois");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion

    #region ZipCode

    [Fact]
    public void ZipCode_Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = ZipCode.Create("60601");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ZipCode_Create_ReturnsFail_WhenNull()
    {
        // Act
        var actual = ZipCode.Create(null);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ZipCode_Create_ReturnsFail_WhenInvalid()
    {
        // Act
        var actual = ZipCode.Create("bad");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    #endregion
}
