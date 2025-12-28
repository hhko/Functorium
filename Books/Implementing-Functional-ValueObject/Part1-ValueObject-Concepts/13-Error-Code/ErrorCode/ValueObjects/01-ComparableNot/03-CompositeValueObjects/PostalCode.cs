using Framework.Abstractions.Errors;
using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCode.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// 우편번호를 나타내는 값 객체
/// SimpleLanguageExtValueObject를 상속받아 기본 기능 활용
/// 10-Validated-Value-Creation 패턴 적용
/// </summary>
public sealed class PostalCode : SimpleValueObject<string>
{
    /// <summary>
    /// PostalCode 인스턴스를 생성하는 private 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="value">우편번호</param>
    private PostalCode(string value)
        : base(value)
    {
    }

    /// <summary>
    /// PostalCode 인스턴스를 생성하는 팩토리 메서드
    /// 부모 클래스의 CreateFromValidation 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="value">우편번호</param>
    /// <returns>성공 시 PostalCode, 실패 시 Error</returns>
    public static Fin<PostalCode> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new PostalCode(validValue));

    /// <summary>
    /// 이미 검증된 값으로 PostalCode 인스턴스를 생성하는 static internal 메서드
    /// 부모 값 객체에서만 사용
    /// </summary>
    /// <param name="validatedValue">이미 검증된 우편번호</param>
    /// <returns>생성된 PostalCode 인스턴스</returns>
    internal static PostalCode CreateFromValidated(string validatedValue) =>
        new PostalCode(validatedValue);

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 검증 로직만 담당하는 별도 메서드
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(ValidateFormat);

    /// <summary>
    /// 빈 값 검증 - 논리적 단위 1
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainErrors.Empty(value)
            : value;

    /// <summary>
    /// 형식 검증 - 논리적 단위 2
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, string> ValidateFormat(string value) =>
        value.Length != 5 || !value.All(char.IsDigit)
            ? DomainErrors.NotFiveDigits(value)
            : value;

    internal static class DomainErrors
    {
        /// <summary>
        /// 빈 우편번호에 대한 에러
        /// </summary>
        /// <param name="value">실패한 우편번호 값</param>
        /// <returns>구조화된 에러 정보</returns>
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PostalCode)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Postal code cannot be empty. Current value: '{value}'");

        /// <summary>
        /// 5자리 숫자가 아닌 우편번호에 대한 에러
        /// </summary>
        /// <param name="value">실패한 우편번호 값</param>
        /// <returns>구조화된 에러 정보</returns>
        public static Error NotFiveDigits(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(PostalCode)}.{nameof(NotFiveDigits)}",
                errorCurrentValue: value,
                errorMessage: $"Postal code must be exactly 5 digits. Current value: '{value}'");
    }
}
