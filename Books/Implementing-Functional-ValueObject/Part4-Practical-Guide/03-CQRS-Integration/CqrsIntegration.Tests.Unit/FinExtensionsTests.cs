using LanguageExt;
using LanguageExt.Common;

namespace CqrsIntegration.Tests.Unit;

/// <summary>
/// FinExtensions 테스트
///
/// 테스트 목적:
/// 1. Fin<T> → ApiResponse<T> 변환 검증
/// 2. 성공/실패 케이스 매핑 검증
/// </summary>
[Trait("Part4-CQRS-Integration", "FinExtensionsTests")]
public class FinExtensionsTests
{
    [Fact]
    public void ToApiResponse_ReturnsSuccess_WhenFinIsSucc()
    {
        // Arrange
        Fin<string> fin = "성공 데이터";

        // Act
        var actual = fin.ToApiResponse();

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Data.ShouldBe("성공 데이터");
        actual.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void ToApiResponse_ReturnsFailure_WhenFinIsFail()
    {
        // Arrange
        Fin<string> fin = Error.New("테스트 오류 메시지");

        // Act
        var actual = fin.ToApiResponse();

        // Assert
        actual.IsSuccess.ShouldBeFalse();
        actual.Data.ShouldBeNull();
        actual.ErrorMessage.ShouldBe("테스트 오류 메시지");
    }

    [Fact]
    public void ToApiResponse_PreservesComplexType_WhenFinIsSucc()
    {
        // Arrange
        var dto = new UserDto("홍길동", "hong@example.com", 25);
        Fin<UserDto> fin = dto;

        // Act
        var actual = fin.ToApiResponse();

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Data.ShouldNotBeNull();
        actual.Data!.Name.ShouldBe("홍길동");
        actual.Data.Email.ShouldBe("hong@example.com");
        actual.Data.Age.ShouldBe(25);
    }

    [Fact]
    public void ToApiResponse_ReturnsDataAsNull_WhenFinHasNullValue()
    {
        // Arrange
        Fin<string?> fin = (string?)null!;

        // Act
        var actual = fin.ToApiResponse();

        // Assert
        actual.IsSuccess.ShouldBeTrue();
        actual.Data.ShouldBeNull();
    }
}
