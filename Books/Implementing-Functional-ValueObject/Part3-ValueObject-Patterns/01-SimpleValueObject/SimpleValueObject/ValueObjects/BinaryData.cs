using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;

namespace SimpleValueObject.ValueObjects;

/// <summary>
/// 1. 비교 불가능한 primitive 값 객체 - SimpleValueObject<T>
/// 이진 데이터를 나타내는 값 객체 (byte[] 기반)
/// </summary>
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) 
        : base(value) 
    {
    }

    /// <summary>
    /// 이진 데이터 값 객체 생성
    /// </summary>
    /// <param name="value">이진 데이터 배열</param>
    /// <returns>Fin<BinaryData> - 성공 시 BinaryData, 실패 시 Error</returns>
    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(Validate(value), val => new BinaryData(val));

    /// <summary>
    /// 이미 검증된 이진 데이터로 값 객체 생성
    /// </summary>
    /// <param name="validatedValue">검증된 이진 데이터</param>
    /// <returns>BinaryData 값 객체</returns>
    internal static BinaryData CreateFromValidated(byte[] validatedValue) =>
        new BinaryData(validatedValue);

    /// <summary>
    /// 이진 데이터 유효성 검증
    /// </summary>
    /// <param name="value">이진 데이터 배열</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, byte[]> Validate(byte[] value) =>
        value != null && value.Length > 0
            ? value
            : DomainErrors.Empty(value);

    public override string ToString() =>
        $"BinaryData[{Value.Length} bytes: {BitConverter.ToString(Value).Replace("-", " ")}]";

    internal static class DomainErrors
    {
        public static Error Empty(byte[] value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(BinaryData)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Binary data cannot be empty or null. Current value: '{(value == null ? "null" : $"{value.Length} bytes")}'");
    }
}