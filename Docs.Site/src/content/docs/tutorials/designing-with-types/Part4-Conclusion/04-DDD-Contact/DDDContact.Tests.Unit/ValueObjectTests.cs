using DDDContact;

namespace DDDContact.Tests.Unit;

/// <summary>
/// Value Object 3종 팩토리 패턴 테스트
///
/// 테스트 목적:
/// 1. Create 검증 성공/실패
/// 2. CreateFromValidated 직접 생성
/// 3. implicit operator 변환
/// </summary>
[Trait("Part4-Conclusion", "04-DDDContact")]
public class ValueObjectTests
{
    [Fact]
    public void String50_Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = String50.Create("Hello");

        // Assert
        actual.IsSucc.ShouldBeTrue();
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
    public void EmailAddress_Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = EmailAddress.Create("user@example.com");

        // Assert
        actual.IsSucc.ShouldBeTrue();
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
    public void EmailAddress_CreateFromValidated_CreatesDirectly()
    {
        // Act
        var actual = EmailAddress.CreateFromValidated("user@example.com");

        // Assert
        string value = actual;
        value.ShouldBe("user@example.com");
    }

    [Fact]
    public void StateCode_Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = StateCode.Create("IL");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void StateCode_Create_ReturnsFail_WhenInvalid()
    {
        // Act
        var actual = StateCode.Create("Illinois");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void ZipCode_Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = ZipCode.Create("60601");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ZipCode_Create_ReturnsFail_WhenInvalid()
    {
        // Act
        var actual = ZipCode.Create("bad");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
