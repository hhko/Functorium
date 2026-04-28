using System.Text.RegularExpressions;

namespace DesigningWithTypes.AggregateRoots.Contacts.ValueObjects;

/// <summary>
/// 미국 주 코드 값 객체 (2자리 대문자, 향상: string? 입력)
/// </summary>
public sealed partial class StateCode : SimpleValueObject<string>
{
    private StateCode(string value) : base(value) { }

    public static Fin<StateCode> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new StateCode(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<StateCode>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMatches(StatePattern());

    public static StateCode CreateFromValidated(string value) => new(value);

    public static implicit operator string(StateCode vo) => vo.Value;

    [GeneratedRegex(@"^[A-Z]{2}$")]
    private static partial Regex StatePattern();
}
