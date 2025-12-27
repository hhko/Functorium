using Framework.Abstractions.Errors;
using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ArchitectureTest.ValueObjects.ComparableNot.CompositeValueObjects;

/// <summary>
/// 거리명을 나타내는 값 객체
/// SimpleLanguageExtValueObject를 상속받아 기본 기능 활용
/// 10-Validated-Value-Creation 패턴 적용
/// </summary>
public sealed class Street : SimpleValueObject<string>
{
    /// <summary>
    /// Street 인스턴스를 생성하는 private 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="value">거리명</param>
    private Street(string value)
        : base(value)
    {
    }

    /// <summary>
    /// Street 인스턴스를 생성하는 팩토리 메서드
    /// 부모 클래스의 CreateFromValidation 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="value">거리명</param>
    /// <returns>성공 시 Street, 실패 시 Error</returns>
    public static Fin<Street> Create(string value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Street(validValue));

    /// <summary>
    /// 이미 검증된 값으로 Street 인스턴스를 생성하는 static internal 메서드
    /// 부모 값 객체에서만 사용
    /// </summary>
    /// <param name="validatedValue">이미 검증된 거리명</param>
    /// <returns>생성된 Street 인스턴스</returns>
    internal static Street CreateFromValidated(string validatedValue) =>
        new Street(validatedValue);

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 검증 로직만 담당하는 별도 메서드
    /// </summary>
    /// <param name="value">검증할 값</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, string> Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainErrors.Empty(value)
            : value;

    internal static class DomainErrors
    {
        /// <summary>
        /// 빈 거리명에 대한 에러
        /// </summary>
        /// <param name="value">실패한 거리명 값</param>
        /// <returns>구조화된 에러 정보</returns>
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Street)}.{nameof(Empty)}",
                errorCurrentValue: value);
    }
}
