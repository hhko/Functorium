using LanguageExt;

namespace CqrsIntegration.Tests.Unit;

/// <summary>
/// GetUserQuery Handler 테스트
///
/// 테스트 목적:
/// 1. CQRS Query에서 값 객체 조회 확인
/// 2. 존재하지 않는 엔티티 처리 확인
/// 3. Fin<T> 결과 처리 검증
/// </summary>
[Trait("Part4-CQRS-Integration", "GetUserQueryHandlerTests")]
public class GetUserQueryHandlerTests
{
    private readonly GetUserQueryHandler _handler;
    private readonly UserRepository _repository;

    public GetUserQueryHandlerTests()
    {
        _repository = new UserRepository();
        _handler = new GetUserQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenUserExists()
    {
        // Arrange - 기존에 등록된 사용자 ID 사용
        var existingUserId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var query = new GetUserQuery(existingUserId);

        // Act
        var actual = await _handler.Handle(query, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: user =>
            {
                user.Name.ShouldBe("기존 사용자");
                user.Email.ShouldBe("existing@example.com");
                user.Age.ShouldBe(30);
            },
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenUserNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var query = new GetUserQuery(nonExistentId);

        // Act
        var actual = await _handler.Handle(query, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("사용자를 찾을 수 없습니다")
        );
    }
}
