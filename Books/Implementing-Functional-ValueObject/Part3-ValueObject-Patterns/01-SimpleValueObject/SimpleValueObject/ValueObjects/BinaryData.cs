using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;

namespace SimpleValueObject.ValueObjects;

/// <summary>
/// 1. 비교 불가능한 primitive 값 객체 - SimpleValueObject&lt;T&gt;
/// 이진 데이터를 나타내는 값 객체 (byte[] 기반)
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) : base(value) { }

    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(Validate(value), v => new BinaryData(v));

    public static BinaryData CreateFromValidated(byte[] validatedValue) =>
        new(validatedValue);

    public static Validation<Error, byte[]> Validate(byte[] value) =>
        value != null && value.Length > 0
            ? value
            : DomainError.For<BinaryData, byte[]>(new DomainErrorType.Empty(), value!,
                $"Binary data cannot be empty or null. Current value: '{(value == null ? "null" : $"{value.Length} bytes")}'");

    public override string ToString() =>
        $"BinaryData[{Value.Length} bytes: {BitConverter.ToString(Value).Replace("-", " ")}]";
}
