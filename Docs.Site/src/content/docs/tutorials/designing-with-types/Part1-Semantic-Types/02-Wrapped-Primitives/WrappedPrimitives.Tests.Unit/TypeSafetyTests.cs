using WrappedPrimitives.ValueObjects;

namespace WrappedPrimitives.Tests.Unit;

/// <summary>
/// 래핑된 타입 간 타입 안전성 테스트
///
/// 테스트 목적:
/// 1. EmailAddress와 ZipCode가 서로 다른 타입임을 확인
/// 2. 같은 값이라도 타입이 다르면 동등하지 않음을 확인
/// </summary>
[Trait("Part1-Semantic-Types", "02-WrappedPrimitives")]
public class TypeSafetyTests
{
    [Fact]
    public void EmailAddress_IsNotEqualTo_ZipCode_EvenWithSameString()
    {
        // Arrange — 같은 문자열이지만 다른 타입
        var email = EmailAddress.Create("12345@test.com").Match(
            Succ: e => e,
            Fail: _ => throw new Exception("생성 실패"));
        var zip = ZipCode.Create("12345").Match(
            Succ: z => z,
            Fail: _ => throw new Exception("생성 실패"));

        // Assert — 타입이 다르므로 동등하지 않음
        email.Equals(zip).ShouldBeFalse();
    }

    [Fact]
    public void ZipCode_Create_ReturnsSuccess_WhenFiveDigits()
    {
        // Act
        var actual = ZipCode.Create("90210");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void ZipCode_Create_ReturnsFail_WhenNotFiveDigits()
    {
        // Act
        var actual = ZipCode.Create("1234");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void StateCode_Create_ReturnsSuccess_WhenTwoUppercaseLetters()
    {
        // Act
        var actual = StateCode.Create("CA");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void StateCode_Create_ReturnsFail_WhenLowercase()
    {
        // Act
        var actual = StateCode.Create("ca");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
