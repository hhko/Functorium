using ApplyInternalBindValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ApplyInternalBindValidation.Tests.Unit;

/// <summary>
/// MemberRegistration 값 객체의 Apply 내부 Bind 검증 패턴 테스트
///
/// 학습 목표:
/// 1. 외부 Apply + 내부 Bind 중첩 검증 패턴 이해
/// 2. 각 필드별 다단계 검증의 순차적 실행 확인
/// 3. 필드 간 병렬 검증과 필드 내 순차 검증의 조합
/// </summary>
[Trait("Part2-Validation", "04-Apply-Internal-Bind")]
public class MemberRegistrationTests
{
    // 테스트 시나리오: 모든 필드가 유효할 때 MemberRegistration 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenAllFieldsAreValid()
    {
        // Arrange
        string username = "johndoe";
        string email = "john@example.com";
        string password = "secure123";

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: member =>
            {
                member.Username.ShouldBe(username);
                member.Email.ShouldBe(email);
                member.Password.ShouldBe(password);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 사용자명이 너무 짧을 때 실패 (내부 Bind 첫 번째 단계)
    [Fact]
    public void Create_ReturnsFail_WhenUsernameTooShort()
    {
        // Arrange
        string username = "ab";  // 3자 미만
        string email = "john@example.com";
        string password = "secure123";

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 사용자명이 admin으로 시작할 때 실패 (내부 Bind 두 번째 단계)
    [Fact]
    public void Create_ReturnsFail_WhenUsernameStartsWithAdmin()
    {
        // Arrange
        string username = "administrator";  // admin으로 시작
        string email = "john@example.com";
        string password = "secure123";

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 이메일 형식이 유효하지 않을 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenEmailMissingAt()
    {
        // Arrange
        string username = "johndoe";
        string email = "johnexample.com";  // @ 없음
        string password = "secure123";

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 이메일 도메인이 지원되지 않을 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenEmailDomainUnsupported()
    {
        // Arrange
        string username = "johndoe";
        string email = "john@example.org";  // .org 도메인 지원 안 함
        string password = "secure123";

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 비밀번호가 약할 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenPasswordTooWeak()
    {
        // Arrange
        string username = "johndoe";
        string email = "john@example.com";
        string password = "weak";  // 6자 미만, 숫자 없음

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 비밀번호가 이전에 사용된 것일 때 실패
    [Fact]
    public void Create_ReturnsFail_WhenPasswordInHistory()
    {
        // Arrange
        string username = "johndoe";
        string email = "john@example.com";
        string password = "password123";  // 이전에 사용된 비밀번호

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 외부 Apply로 인해 각 필드의 첫 번째 에러가 병렬로 수집됨
    [Fact]
    public void Validate_CollectsFirstErrorFromEachField_WhenAllFieldsInvalid()
    {
        // Arrange - 모든 필드의 첫 번째 검증이 실패
        string username = "ab";             // 첫 번째 검증 실패 (3자 미만)
        string email = "invalid";           // 첫 번째 검증 실패 (@ 없음)
        string password = "weak";           // 첫 번째 검증 실패 (너무 약함)

        // Act
        var actual = MemberRegistration.Validate(username, email, password);

        // Assert - 외부 Apply로 각 필드에서 하나씩 3개 에러 수집
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("예상치 못한 성공"),
            Fail: error => error.Count.ShouldBe(3));
    }

    // 테스트 시나리오: .com 도메인 이메일 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenEmailHasComDomain()
    {
        // Arrange
        string username = "johndoe";
        string email = "john@example.com";
        string password = "secure123";

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: .co.kr 도메인 이메일 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenEmailHasCoKrDomain()
    {
        // Arrange
        string username = "johndoe";
        string email = "john@example.co.kr";
        string password = "secure123";

        // Act
        Fin<MemberRegistration> actual = MemberRegistration.Create(username, email, password);

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
