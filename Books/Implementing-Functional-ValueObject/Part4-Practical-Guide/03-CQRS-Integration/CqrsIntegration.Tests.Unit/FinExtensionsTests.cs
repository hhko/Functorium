using Functorium.Applications.Cqrs;
using LanguageExt;
using LanguageExt.Common;

namespace CqrsIntegration.Tests.Unit;

/// <summary>
/// FinResponse 테스트
///
/// 테스트 목적:
/// 1. Fin<T> → FinResponse<T> 변환 검증
/// 2. 성공/실패 케이스 매핑 검증
/// </summary>
[Trait("Part4-CQRS-Integration", "FinExtensionsTests")]
public class FinExtensionsTests
{
    [Fact]
    public void ToFinResponse_ReturnsSuccess_WhenFinIsSucc()
    {
        // Arrange
        Fin<string> fin = "성공 데이터";

        // Act
        var actual = fin.ToFinResponse();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: data => data.ShouldBe("성공 데이터"),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void ToFinResponse_ReturnsFailure_WhenFinIsFail()
    {
        // Arrange
        Fin<string> fin = Error.New("테스트 오류 메시지");

        // Act
        var actual = fin.ToFinResponse();

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldBe("테스트 오류 메시지")
        );
    }

    [Fact]
    public void ToFinResponse_PreservesComplexType_WhenFinIsSucc()
    {
        // Arrange
        var response = new GetUserByIdQuery.Response("홍길동", "hong@example.com", 25);
        Fin<GetUserByIdQuery.Response> fin = response;

        // Act
        var actual = fin.ToFinResponse();

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: data =>
            {
                data.Name.ShouldBe("홍길동");
                data.Email.ShouldBe("hong@example.com");
                data.Age.ShouldBe(25);
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public void ToFinResponse_WithMapping_TransformsData()
    {
        // Arrange
        var entity = new UserEntity(
            UserName.CreateFromValidated("홍길동"),
            Email.CreateFromValidated("hong@example.com"),
            Age.CreateFromValidated(25));
        Fin<UserEntity> fin = entity;

        // Act
        var actual = fin.ToFinResponse(e =>
            new GetUserByIdQuery.Response(e.Name.Name, e.Email.Address, e.Age.Years));

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: data =>
            {
                data.Name.ShouldBe("홍길동");
                data.Email.ShouldBe("hong@example.com");
                data.Age.ShouldBe(25);
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }
}
