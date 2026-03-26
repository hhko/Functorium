using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations.Typed;

using LanguageExt;
using LanguageExt.Common;

namespace Cqrs01.Demo.Domain.ValueObjects;

/// <summary>
/// 사용자 이름 Value Object
/// </summary>
public sealed class UserName : SimpleValueObject<string>
{
    public const int MaxLength = 100;

    private UserName(string value) : base(value) { }

    public static Fin<UserName> Create(string? value) =>
        CreateFromValidation(Validate(value), v => new UserName(v));

    public static Validation<Error, string> Validate(string? value) =>
        ValidationRules<UserName>.NotEmpty(value ?? "")
            .ThenNormalize(v => v.Trim())
            .ThenMaxLength(MaxLength);

    public static implicit operator string(UserName name) => name.ToString();
}
