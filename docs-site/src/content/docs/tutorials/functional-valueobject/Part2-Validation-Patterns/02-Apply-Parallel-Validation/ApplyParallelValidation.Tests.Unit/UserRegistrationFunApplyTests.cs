using ApplyParallelValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyParallelValidation.Tests.Unit;

/// <summary>
/// UserRegistrationFunApply 값 객체의 fun 기반 개별 Apply 패턴 테스트
///
/// 학습 목표:
/// 1. fun 함수와 개별 Apply 체이닝을 통한 병렬 검증 동작 이해
/// 2. 튜플 기반 Apply와 동일한 에러 수집 결과 확인
/// 3. Currying 기반 Apply 패턴의 동작 검증
/// </summary>
[Trait("Part2-Validation", "02-Apply-Parallel-FunApply")]
public class UserRegistrationFunApplyTests
{
    // 테스트 시나리오: 모든 필드가 유효할 때 UserRegistrationFunApply 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsAreValid()
    {
        // Arrange
        string email = "user@example.com";
        string password = "password123";
        string name = "John Doe";
        string ageInput = "25";

        // Act
        Fin<UserRegistrationFunApply> actual = UserRegistrationFunApply.Create(email, password, name, ageInput);

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

    // 테스트 시나리오: fun 기반 Apply 패턴에서 모든 에러가 수집됨
    [Fact]
    public void Validate_CollectsAllErrors_WhenUsingFunApplyPattern()
    {
        // Arrange - 모든 필드가 유효하지 않은 경우
        string email = "invalid";       // @ 없음
        string password = "short";      // 8자 미만
        string name = "J";              // 2자 미만
        string ageInput = "abc";        // 숫자가 아님

        // Act
        var actual = UserRegistrationFunApply.Validate(email, password, name, ageInput);

        // Assert - fun 기반 Apply 패턴도 모든 에러를 수집
        actual.IsFail.ShouldBeTrue();
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
        var actual = UserRegistrationFunApply.Validate(email, password, name, ageInput);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("예상치 못한 성공"),
            Fail: error => error.Count.ShouldBe(2));
    }

    // ============================================================
    // 튜플 기반 Apply와 fun 기반 Apply 비교 테스트
    // ============================================================

    // 테스트 시나리오: 튜플 기반과 fun 기반 Apply가 동일한 성공 결과 반환
    [Fact]
    public void Validate_TupleAndFunApply_ReturnSameSuccessResult()
    {
        // Arrange
        string email = "user@example.com";
        string password = "password123";
        string name = "John Doe";
        string ageInput = "25";

        // Act
        var tupleResult = UserRegistration.Validate(email, password, name, ageInput);
        var funResult = UserRegistrationFunApply.Validate(email, password, name, ageInput);

        // Assert - 둘 다 성공
        tupleResult.IsSuccess.ShouldBeTrue();
        funResult.IsSuccess.ShouldBeTrue();

        // 결과 값도 동일
        tupleResult.Match(
            Succ: tupleValue => funResult.Match(
                Succ: funValue =>
                {
                    funValue.Email.ShouldBe(tupleValue.Email);
                    funValue.Password.ShouldBe(tupleValue.Password);
                    funValue.Name.ShouldBe(tupleValue.Name);
                    funValue.Age.ShouldBe(tupleValue.Age);
                },
                Fail: _ => throw new Exception("fun 결과가 실패함")),
            Fail: _ => throw new Exception("tuple 결과가 실패함"));
    }

    // 테스트 시나리오: 튜플 기반과 fun 기반 Apply가 동일한 에러 개수 수집
    [Fact]
    public void Validate_TupleAndFunApply_CollectSameErrorCount()
    {
        // Arrange - 모든 필드가 유효하지 않음
        string email = "invalid";
        string password = "short";
        string name = "J";
        string ageInput = "abc";

        // Act
        var tupleResult = UserRegistration.Validate(email, password, name, ageInput);
        var funResult = UserRegistrationFunApply.Validate(email, password, name, ageInput);

        // Assert - 둘 다 실패하고 동일한 에러 개수
        tupleResult.IsFail.ShouldBeTrue();
        funResult.IsFail.ShouldBeTrue();

        var tupleErrorCount = tupleResult.Match(Succ: _ => 0, Fail: e => e.Count);
        var funErrorCount = funResult.Match(Succ: _ => 0, Fail: e => e.Count);

        tupleErrorCount.ShouldBe(funErrorCount);
        tupleErrorCount.ShouldBe(4);
    }

    // 테스트 시나리오: 두 가지 방법으로 부분 실패 시 동일한 에러 개수
    [Theory]
    [InlineData("invalid", "password123", "John Doe", "25", 1)]      // 이메일만 실패
    [InlineData("user@example.com", "short", "John Doe", "25", 1)]   // 비밀번호만 실패
    [InlineData("invalid", "short", "John Doe", "25", 2)]            // 이메일 + 비밀번호 실패
    [InlineData("invalid", "short", "J", "25", 3)]                   // 이메일 + 비밀번호 + 이름 실패
    [InlineData("invalid", "short", "J", "abc", 4)]                  // 모두 실패
    public void Validate_TupleAndFunApply_CollectSameErrorCount_ForVariousInputs(
        string email, string password, string name, string ageInput, int expectedErrorCount)
    {
        // Act
        var tupleResult = UserRegistration.Validate(email, password, name, ageInput);
        var funResult = UserRegistrationFunApply.Validate(email, password, name, ageInput);

        // Assert
        var tupleErrorCount = tupleResult.Match(Succ: _ => 0, Fail: e => e.Count);
        var funErrorCount = funResult.Match(Succ: _ => 0, Fail: e => e.Count);

        tupleErrorCount.ShouldBe(expectedErrorCount);
        funErrorCount.ShouldBe(expectedErrorCount);
        tupleErrorCount.ShouldBe(funErrorCount);
    }

    // 테스트 시나리오: ValidateTupleStyle이 UserRegistration.Validate와 동일한 결과 반환
    [Fact]
    public void ValidateTupleStyle_ReturnsSameResultAsUserRegistrationValidate()
    {
        // Arrange
        string email = "invalid";
        string password = "short";
        string name = "J";
        string ageInput = "abc";

        // Act
        var originalResult = UserRegistration.Validate(email, password, name, ageInput);
        var tupleStyleResult = UserRegistrationFunApply.ValidateTupleStyle(email, password, name, ageInput);

        // Assert
        originalResult.IsFail.ShouldBeTrue();
        tupleStyleResult.IsFail.ShouldBeTrue();

        var originalErrorCount = originalResult.Match(Succ: _ => 0, Fail: e => e.Count);
        var tupleStyleErrorCount = tupleStyleResult.Match(Succ: _ => 0, Fail: e => e.Count);

        originalErrorCount.ShouldBe(tupleStyleErrorCount);
    }
}
