using Framework.Layers.Domains;
using Framework.Layers.Domains.Validations;
using LanguageExt;
using LanguageExt.Common;

namespace ValidationFluent.ValueObjects.ComparableNot.PrimitiveValueObjects;

/// <summary>
/// 바이너리 데이터를 나타내는 값 객체
/// SimpleValueObject&lt;byte[]&gt; 기반으로 비교 기능 없이 구현
/// Validate&lt;T&gt; Fluent API를 사용한 간결한 검증
/// </summary>
public sealed class BinaryData : SimpleValueObject<byte[]>
{
    private BinaryData(byte[] value) : base(value) { }

    public static Fin<BinaryData> Create(byte[] value) =>
        CreateFromValidation(Validate(value), validValue => new BinaryData(validValue));

    public static BinaryData CreateFromValidated(byte[] validatedValue) =>
        new BinaryData(validatedValue);

    public static Validation<Error, byte[]> Validate(byte[]? value) =>
        ValidationRules<BinaryData>.NotEmptyArray(value);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Convert.ToBase64String(Value);
    }

    public override string ToString() =>
        $"BinaryData[{Value.Length} bytes: {BitConverter.ToString(Value).Replace("-", " ")}]";
}
