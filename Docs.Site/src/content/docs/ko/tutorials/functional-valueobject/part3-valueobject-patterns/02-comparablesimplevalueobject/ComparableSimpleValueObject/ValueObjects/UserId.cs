using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

namespace ComparableSimpleValueObject.ValueObjects;

/// <summary>
/// 2. 비교 가능한 primitive 값 객체 - ComparableSimpleValueObject&lt;T&gt;
/// 사용자 ID를 나타내는 값 객체 (int 기반)
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class UserId : ComparableSimpleValueObject<int>
{
    private UserId(int value) : base(value) { }

    public int Id => Value;

    public static Fin<UserId> Create(int value) =>
        CreateFromValidation(Validate(value), v => new UserId(v));

    public static UserId CreateFromValidated(int validatedValue) =>
        new(validatedValue);

    public static Validation<Error, int> Validate(int value) =>
        ValidationRules<UserId>.Positive(value);

    public static implicit operator int(UserId userId) => userId.Value;
}
