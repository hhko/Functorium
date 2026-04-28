using LanguageExt;
using LanguageExt.Common;

namespace ReturnTypeValidation.Domains;

public sealed class Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value)
        => string.IsNullOrWhiteSpace(value) || !value.Contains('@')
            ? Fin.Fail<Email>(Error.New("Invalid email"))
            : Fin.Succ(new Email(value));

    public override string ToString() => Value;
}
