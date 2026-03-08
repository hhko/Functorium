using CompositeTypes.ValueObjects;

namespace CompositeTypes.Tests.Unit;

/// <summary>
/// PostalAddress 복합 값 객체 테스트
///
/// 테스트 목적:
/// 1. 유효한 주소로 생성 성공
/// 2. 잘못된 구성요소로 생성 실패
/// </summary>
[Trait("Part1-Semantic-Types", "04-CompositeTypes")]
public class PostalAddressTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsValid()
    {
        // Act
        var actual = PostalAddress.Create("123 Main St", "Springfield", "IL", "62701");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenZipCodeInvalid()
    {
        // Act
        var actual = PostalAddress.Create("123 Main St", "Springfield", "IL", "invalid");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenStateCodeInvalid()
    {
        // Act
        var actual = PostalAddress.Create("123 Main St", "Springfield", "xx", "62701");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
