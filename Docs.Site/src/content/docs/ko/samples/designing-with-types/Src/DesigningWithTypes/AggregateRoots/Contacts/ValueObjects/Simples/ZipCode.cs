using System.Text.RegularExpressions;

namespace DesigningWithTypes.AggregateRoots.Contacts.ValueObjects;

/// <summary>
/// 우편번호 값 객체 (5자리 숫자, 향상: string? 입력)
/// </summary>
public sealed partial class ZipCode : SimpleValueObject<string>
{
    private ZipCode(string value) : base(value) { }

    public static Fin<ZipCode> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new ZipCode(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<ZipCode>
            .NotNull(value)
            .ThenNotEmpty()
            .ThenMatches(ZipPattern());

    public static ZipCode CreateFromValidated(string value) => new(value);

    public static implicit operator string(ZipCode vo) => vo.Value;

    [GeneratedRegex(@"^\d{5}$")]
    private static partial Regex ZipPattern();
}
