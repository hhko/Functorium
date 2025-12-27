using Framework.Abstractions.Errors;
using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCode.ValueObjects.ComparableNot.PrimitiveValueObjects;

/// <summary>
/// 바이너리 데이터를 나타내는 값 객체
/// SimpleValueObject<byte[]> 기반으로 비교 기능 없이 구현
/// 10-Validated-Value-Creation 패턴 적용
/// </summary>
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    /// <summary>
    /// BinaryData 인스턴스를 생성하는 private 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="value">바이너리 데이터</param>
    private BinaryData(byte[] value)
        : base(value)
    {
    }

    /// <summary>
    /// BinaryData 인스턴스를 생성하는 팩토리 메서드
    /// 부모 클래스의 CreateFromValidation 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="value">바이너리 데이터</param>
    /// <returns>성공 시 BinaryData, 실패 시 Error</returns>
    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new BinaryData(validValue));

    /// <summary>
    /// 이미 검증된 값으로 BinaryData 인스턴스를 생성하는 static internal 메서드
    /// 부모 값 객체에서만 사용
    /// </summary>
    /// <param name="validatedValue">이미 검증된 바이너리 데이터</param>
    /// <returns>생성된 BinaryData 인스턴스</returns>
    internal static BinaryData CreateFromValidated(byte[] validatedValue) =>
        new BinaryData(validatedValue);

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 검증 로직만 담당하는 별도 메서드
    /// </summary>
    /// <param name="value">검증할 바이너리 데이터</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, byte[]> Validate(byte[]? value) =>
        value == null || value.Length == 0
            ? DomainErrors.Empty(value)
            : value;

    internal static class DomainErrors
    {
        /// <summary>
        /// 빈 바이너리 데이터에 대한 에러
        /// </summary>
        /// <param name="value">실패한 바이너리 데이터 값</param>
        /// <returns>구조화된 에러 정보</returns>
        public static Error Empty(byte[]? value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(BinaryData)}.{nameof(Empty)}",
                errorCurrentValue: value?.Length.ToString() ?? "null");
    }

    /// <summary>
    /// 동등성 비교 구성 요소 반환
    /// byte[] 배열의 내용을 비교하기 위해 오버라이드
    /// </summary>
    /// <returns>바이너리 데이터의 내용을 나타내는 객체</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        // byte[] 배열의 내용을 비교하기 위해 문자열로 변환
        yield return Convert.ToBase64String(Value);
    }

    /// <summary>
    /// 바이너리 데이터의 문자열 표현
    /// </summary>
    /// <returns>바이너리 데이터의 16진수 표현</returns>
    public override string ToString() =>
        $"BinaryData[{Value.Length} bytes: {BitConverter.ToString(Value).Replace("-", " ")}]";

    // 비교 기능은 제공되지 않음 (의도적으로)
    // - byte[]는 IComparable을 구현하지 않음
    // - 동등성 비교와 해시코드만 자동 제공
    // - SimpleValueObject<byte[]>에서 자동으로 제공됨
}
