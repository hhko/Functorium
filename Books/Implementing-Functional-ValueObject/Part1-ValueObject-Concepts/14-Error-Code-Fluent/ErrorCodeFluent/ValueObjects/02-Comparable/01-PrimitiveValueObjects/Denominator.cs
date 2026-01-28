using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCodeFluent.ValueObjects.Comparable.PrimitiveValueObjects;

/// <summary>
/// 0이 아닌 정수를 나타내는 분모 값 객체
/// ComparableSimpleValueObject&lt;int&gt; 기반으로 비교 가능성 자동 구현
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Denominator : ComparableSimpleValueObject<int>
{
    private Denominator(int value) : base(value) { }

    public static Fin<Denominator> Create(int value) =>
        CreateFromValidation(Validate(value), validValue => new Denominator(validValue));

    public static Denominator CreateFromValidated(int validatedValue) =>
        new Denominator(validatedValue);

    public static Validation<Error, int> Validate(int value) =>
        value == 0
            ? DomainError.For<Denominator, int>(new DomainErrorType.Custom("Zero"), value,
                $"Denominator cannot be zero. Current value: '{value}'")
            : value;
}
