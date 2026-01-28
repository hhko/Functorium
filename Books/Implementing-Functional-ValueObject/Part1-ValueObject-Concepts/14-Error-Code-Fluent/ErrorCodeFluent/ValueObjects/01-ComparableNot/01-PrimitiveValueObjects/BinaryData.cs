using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCodeFluent.ValueObjects.ComparableNot.PrimitiveValueObjects;

/// <summary>
/// 바이너리 데이터를 나타내는 값 객체
/// SimpleValueObject&lt;byte[]&gt; 기반으로 비교 기능 없이 구현
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) : base(value) { }

    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(Validate(value), validValue => new BinaryData(validValue));

    public static BinaryData CreateFromValidated(byte[] validatedValue) =>
        new BinaryData(validatedValue);

    public static Validation<Error, byte[]> Validate(byte[]? value) =>
        value == null || value.Length == 0
            ? DomainError.For<BinaryData>(new DomainErrorType.Empty(), value?.Length.ToString() ?? "null",
                $"Binary data cannot be empty or null. Current value: '{value?.Length.ToString() ?? "null"}'")
            : value;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Convert.ToBase64String(Value);
    }

    public override string ToString() =>
        $"BinaryData[{Value.Length} bytes: {BitConverter.ToString(Value).Replace("-", " ")}]";
}
