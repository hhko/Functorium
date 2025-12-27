using LanguageExt;

namespace CqrsIntegration.Tests.Unit;

/// <summary>
/// CreateUserCommand Handler 테스트
///
/// 테스트 목적:
/// 1. CQRS Command에서 값 객체 검증 확인
/// 2. Bind 패턴을 통한 순차 검증 동작 확인
/// 3. 성공/실패 시나리오 검증
/// </summary>
[Trait("Part4-CQRS-Integration", "CreateUserCommandHandlerTests")]
public class CreateUserCommandHandlerTests
{
    private readonly CreateUserCommandHandler _handler;
    private readonly UserRepository _repository;

    public CreateUserCommandHandlerTests()
    {
        _repository = new UserRepository();
        _handler = new CreateUserCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenAllInputsAreValid()
    {
        // Arrange
        var command = new CreateUserCommand("홍길동", "hong@example.com", 25);

        // Act
        var actual = await _handler.Handle(command, CancellationToken.None);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: response => response.UserId.ShouldNotBe(Guid.Empty),
            Fail: _ => throw new Exception("Expected success")
        );
    }

    [Theory]
    [InlineData("", "hong@example.com", 25)]
    [InlineData("   ", "hong@example.com", 25)]
    public async Task Handle_ReturnsFail_WhenNameIsEmpty(string name, string email, int age)
    {
        // Arrange
        var command = new CreateUserCommand(name, email, age);

        // Act
        var actual = await _handler.Handle(command, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("사용자 이름이 비어있습니다")
        );
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenEmailIsInvalid()
    {
        // Arrange
        var command = new CreateUserCommand("홍길동", "invalid-email", 25);

        // Act
        var actual = await _handler.Handle(command, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("이메일 형식이 올바르지 않습니다")
        );
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task Handle_ReturnsFail_WhenAgeIsNegative(int age)
    {
        // Arrange
        var command = new CreateUserCommand("홍길동", "hong@example.com", age);

        // Act
        var actual = await _handler.Handle(command, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("나이는 음수일 수 없습니다")
        );
    }

    [Fact]
    public async Task Handle_ReturnsFail_WhenAgeExceedsMaximum()
    {
        // Arrange
        var command = new CreateUserCommand("홍길동", "hong@example.com", 200);

        // Act
        var actual = await _handler.Handle(command, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("나이는 150세를 초과할 수 없습니다")
        );
    }

    [Fact]
    public async Task Handle_ReturnsFirstError_WhenMultipleInputsAreInvalid()
    {
        // Arrange - Name이 빈 값이므로 첫 번째 검증 실패
        var command = new CreateUserCommand("", "invalid-email", -5);

        // Act
        var actual = await _handler.Handle(command, CancellationToken.None);

        // Assert
        actual.IsFail.ShouldBeTrue();
        // Bind 패턴은 순차적으로 검증하므로 첫 번째 오류만 반환
        actual.Match(
            Succ: _ => throw new Exception("Expected failure"),
            Fail: error => error.Message.ShouldContain("사용자 이름이 비어있습니다")
        );
    }
}
