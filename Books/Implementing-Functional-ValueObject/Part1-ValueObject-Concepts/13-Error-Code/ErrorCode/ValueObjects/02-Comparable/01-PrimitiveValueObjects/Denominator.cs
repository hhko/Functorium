using Framework.Abstractions.Errors;
using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCode.ValueObjects.Comparable.PrimitiveValueObjects;

/// <summary>
/// 0이 아닌 정수를 나타내는 분모 값 객체
/// ComparableSimpleValueObject<int> 기반으로 비교 가능성 자동 구현
/// 10-Validated-Value-Creation 패턴 적용
/// </summary>
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    /// <summary>
    /// Denominator 인스턴스를 생성하는 private 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="value">0이 아닌 정수 값</param>
    private Denominator(int value)
        : base(value)
    {
    }

    /// <summary>
    /// Denominator 인스턴스를 생성하는 팩토리 메서드
    /// 부모 클래스의 CreateFromValidation 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="value">0이 아닌 정수 값</param>
    /// <returns>성공 시 Denominator, 실패 시 Error</returns>
    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Denominator(validValue));

    /// <summary>
    /// 이미 검증된 값으로 Denominator 인스턴스를 생성하는 static internal 메서드
    /// 부모 값 객체에서만 사용
    /// </summary>
    /// <param name="validatedValue">이미 검증된 0이 아닌 정수 값</param>
    /// <returns>생성된 Denominator 인스턴스</returns>
    internal static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 검증 로직만 담당하는 별도 메서드
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, int> Validate(int value) =>
        value == 0
            ? DomainErrors.Zero(value)
            : value;

    internal static class DomainErrors
    {
        /// <summary>
        /// 0 값에 대한 에러
        /// </summary>
        /// <param name="value">실패한 값</param>
        /// <returns>구조화된 에러 정보</returns>
        public static Error Zero(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Denominator)}.{nameof(Zero)}",
                errorCurrentValue: value,
                errorMessage: $"Denominator cannot be zero. Current value: '{value}'");
    }

    // 비교 가능성은 ComparableSimpleValueObject<int>에서 자동으로 제공됨
    // - IComparable<Denominator> 구현
    // - 모든 비교 연산자 오버로딩 (<, <=, >, >=)
    // - GetComparableEqualityComponents() 자동 구현
}
