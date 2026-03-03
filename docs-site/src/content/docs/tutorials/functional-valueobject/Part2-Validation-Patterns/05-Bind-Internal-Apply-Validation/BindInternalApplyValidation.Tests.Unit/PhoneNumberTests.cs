using BindInternalApplyValidation.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace BindInternalApplyValidation.Tests.Unit;

/// <summary>
/// PhoneNumber 값 객체의 Bind 내부 Apply 검증 패턴 테스트
///
/// 학습 목표:
/// 1. 외부 Bind + 내부 Apply 중첩 검증 패턴 이해
/// 2. 선행 검증 성공 후 병렬 검증 실행 확인
/// 3. 전체 형식 검증 후 구성 요소 병렬 검증의 조합
/// </summary>
[Trait("Part2-Validation", "05-Bind-Internal-Apply")]
public class PhoneNumberTests
{
    // 테스트 시나리오: 유효한 한국 전화번호로 PhoneNumber 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenKoreanPhoneNumberIsValid()
    {
        // Arrange
        string phoneNumber = "+82101234567";

        // Act
        Fin<PhoneNumber> actual = PhoneNumber.Create(phoneNumber);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: phone =>
            {
                phone.CountryCode.ShouldBe("+82");
                phone.AreaCode.ShouldBe("101");
                phone.LocalNumber.ShouldBe("234567");
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 유효한 미국 전화번호로 PhoneNumber 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenUSPhoneNumberIsValid()
    {
        // Arrange
        string phoneNumber = "+1 2125551234";

        // Act
        Fin<PhoneNumber> actual = PhoneNumber.Create(phoneNumber);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: phone =>
            {
                phone.CountryCode.ShouldBe("+1 ");
                phone.AreaCode.ShouldBe("212");
                phone.LocalNumber.ShouldBe("5551234");
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 전화번호가 너무 짧을 때 실패 (외부 Bind 실패)
    [Fact]
    public void Create_ReturnsFail_WhenPhoneNumberTooShort()
    {
        // Arrange
        string phoneNumber = "+821234";  // 10자 미만

        // Act
        Fin<PhoneNumber> actual = PhoneNumber.Create(phoneNumber);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 국가 코드가 지원되지 않을 때 실패 (내부 Apply 검증)
    [Fact]
    public void Create_ReturnsFail_WhenCountryCodeUnsupported()
    {
        // Arrange
        string phoneNumber = "+44101234567";  // 영국 코드 (지원 안 함)

        // Act
        Fin<PhoneNumber> actual = PhoneNumber.Create(phoneNumber);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 지역 코드가 숫자가 아닐 때 실패 (내부 Apply 검증)
    [Fact]
    public void Create_ReturnsFail_WhenAreaCodeInvalid()
    {
        // Arrange
        string phoneNumber = "+82ABC1234567";  // 지역 코드가 숫자가 아님

        // Act
        Fin<PhoneNumber> actual = PhoneNumber.Create(phoneNumber);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 외부 Bind 실패 시 내부 Apply가 실행되지 않음
    [Fact]
    public void Validate_DoesNotExecuteInnerApply_WhenOuterBindFails()
    {
        // Arrange - 전화번호가 너무 짧음 (외부 Bind 실패)
        string phoneNumber = "short";

        // Act
        var actual = PhoneNumber.Validate(phoneNumber);

        // Assert - 외부 Bind에서 실패하면 에러가 1개만 있어야 함
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("예상치 못한 성공"),
            Fail: error => error.Count.ShouldBe(1));
    }

    // 테스트 시나리오: 외부 Bind 성공 후 내부 Apply에서 모든 에러 수집
    [Fact]
    public void Validate_CollectsAllApplyErrors_WhenOuterBindSucceeds()
    {
        // Arrange - 형식은 유효하지만 구성 요소가 모두 유효하지 않음
        string phoneNumber = "+44ABCDEFGHIJ";  // 국가코드, 지역코드, 로컬번호 모두 실패

        // Act
        var actual = PhoneNumber.Validate(phoneNumber);

        // Assert - 내부 Apply에서 3개 에러 수집 (국가코드, 지역코드, 로컬번호)
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => throw new Exception("예상치 못한 성공"),
            Fail: error => error.Count.ShouldBe(3));
    }

    // 테스트 시나리오: 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        string phoneNumber = "+82101234567";

        // Act
        Fin<PhoneNumber> actual1 = PhoneNumber.Create(phoneNumber);
        Fin<PhoneNumber> actual2 = PhoneNumber.Create(phoneNumber);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: 빈 문자열로 실패
    [Fact]
    public void Create_ReturnsFail_WhenPhoneNumberIsEmpty()
    {
        // Arrange
        string phoneNumber = "";

        // Act
        Fin<PhoneNumber> actual = PhoneNumber.Create(phoneNumber);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
