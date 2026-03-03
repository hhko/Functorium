using ApplyParallelValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyParallelValidation.Tests.Unit;

/// <summary>
/// UserRegistration 값 객체의 Apply 병렬 검증 패턴 테스트
///
/// 학습 목표:
/// 1. Apply 연산자를 통한 병렬 검증 동작 이해
/// 2. 모든 검증이 실행되어 모든 에러가 수집되는 것을 확인
/// 3. 독립적인 검증 규칙의 병렬 실행 검증
/// </summary>
[Trait("Part2-Validation", "02-Apply-Parallel")]
public class UserRegistrationTests
{
    // 테스트 시나리오: 모든 필드가 유효할 때 UserRegistration 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsAreValid()
    {
        // Arrange
        string email = "user@example.com";
        string password = "password123";
        string name = "John Doe";
        string ageInput = "25";

        // Act
        Fin<UserRegistration> actual = UserRegistration.Create(email, password, name, ageInput);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: registration =>
            {
                registration.Email.ShouldBe(email);
                registration.Password.ShouldBe(password);
                registration.Name.ShouldBe(name);
                registration.Age.ShouldBe(25);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 이메일 형식이 유효하지 않을 때 실패 반환
    [Fact]
    public void Create_ReturnsFail_WhenEmailMissingAt()
    {
        // Arrange
        string email = "userexample.com";  // @ 없음
        string password = "password123";
        string name = "John Doe";
        string ageInput = "25";

        // Act
        Fin<UserRegistration> actual = UserRegistration.Create(email, password, name, ageInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 비밀번호가 너무 짧을 때 실패 반환
    [Fact]
    public void Create_ReturnsFail_WhenPasswordTooShort()
    {
        // Arrange
        string email = "user@example.com";
        string password = "short";  // 8자 미만
        string name = "John Doe";
        string ageInput = "25";

        // Act
        Fin<UserRegistration> actual = UserRegistration.Create(email, password, name, ageInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 이름이 너무 짧을 때 실패 반환
    [Fact]
    public void Create_ReturnsFail_WhenNameTooShort()
    {
        // Arrange
        string email = "user@example.com";
        string password = "password123";
        string name = "J";  // 2자 미만
        string ageInput = "25";

        // Act
        Fin<UserRegistration> actual = UserRegistration.Create(email, password, name, ageInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 나이가 숫자가 아닐 때 실패 반환
    [Fact]
    public void Create_ReturnsFail_WhenAgeNotNumeric()
    {
        // Arrange
        string email = "user@example.com";
        string password = "password123";
        string name = "John Doe";
        string ageInput = "twenty";  // 숫자가 아님

        // Act
        Fin<UserRegistration> actual = UserRegistration.Create(email, password, name, ageInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: Apply 패턴에서 모든 에러가 수집됨
    [Fact]
    public void Validate_CollectsAllErrors_WhenUsingApplyPattern()
    {
        // Arrange - 모든 필드가 유효하지 않은 경우
        string email = "invalid";       // @ 없음
        string password = "short";      // 8자 미만
        string name = "J";              // 2자 미만
        string ageInput = "abc";        // 숫자가 아님

        // Act
        var actual = UserRegistration.Validate(email, password, name, ageInput);

        // Assert - Apply 패턴은 모든 에러를 수집
        actual.IsFail.ShouldBeTrue();
        // Apply 패턴은 모든 검증을 실행하여 모든 에러를 수집 (ManyErrors로 결합됨)
        actual.Match(
            Succ: _ => throw new Exception("예상치 못한 성공"),
            Fail: error => error.Count.ShouldBe(4));
    }

    // 테스트 시나리오: 일부 필드만 유효하지 않을 때 해당 에러만 수집
    [Fact]
    public void Validate_CollectsOnlyRelevantErrors_WhenSomeFieldsAreInvalid()
    {
        // Arrange - 이메일과 비밀번호만 유효하지 않음
        string email = "invalid";       // @ 없음
        string password = "short";      // 8자 미만
        string name = "John Doe";       // 유효
        string ageInput = "25";         // 유효

        // Act
        var actual = UserRegistration.Validate(email, password, name, ageInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
        // 유효하지 않은 필드 수만큼 에러 수집 (ManyErrors로 결합됨)
        actual.Match(
            Succ: _ => throw new Exception("예상치 못한 성공"),
            Fail: error => error.Count.ShouldBe(2));
    }

    // 테스트 시나리오: 이메일에 @ 기호가 있지만 . 기호가 없을 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenEmailMissingDot()
    {
        // Arrange
        string email = "user@examplecom";  // . 없음
        string password = "password123";
        string name = "John Doe";
        string ageInput = "25";

        // Act
        Fin<UserRegistration> actual = UserRegistration.Create(email, password, name, ageInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 동일한 입력에 대해 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        string email = "user@example.com";
        string password = "password123";
        string name = "John Doe";
        string ageInput = "25";

        // Act
        Fin<UserRegistration> actual1 = UserRegistration.Create(email, password, name, ageInput);
        Fin<UserRegistration> actual2 = UserRegistration.Create(email, password, name, ageInput);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }
}
