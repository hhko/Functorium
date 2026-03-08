using WrappedPrimitives.ValueObjects;

namespace WrappedPrimitives.Tests.Unit;

/// <summary>
/// EmailAddress 값 객체 테스트
///
/// 테스트 목적:
/// 1. 유효한 이메일 주소로 생성 성공 확인
/// 2. 유효하지 않은 이메일 주소로 생성 실패 확인
/// </summary>
[Trait("Part1-Semantic-Types", "02-WrappedPrimitives")]
public class EmailAddressTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenEmailIsValid()
    {
        // Act
        var actual = EmailAddress.Create("user@example.com");

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: email => email.ToString().ShouldBe("user@example.com"),
            Fail: _ => throw new Exception("생성 실패"));
    }

    [Fact]
    public void Create_ReturnsFail_WhenEmailIsEmpty()
    {
        // Act
        var actual = EmailAddress.Create("");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenEmailHasNoAtSign()
    {
        // Act
        var actual = EmailAddress.Create("not-an-email");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
