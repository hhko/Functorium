using LanguageExt;
using LanguageExt.Common;

namespace ReturnTypeValidation.Domains;

public sealed class PhoneNumber
{
    public string Value { get; }
    private PhoneNumber(string value) => Value = value;

    public static Fin<PhoneNumber> Create(string value)
        => string.IsNullOrWhiteSpace(value)
            ? Fin.Fail<PhoneNumber>(Error.New("Invalid phone number"))
            : Fin.Succ(new PhoneNumber(value));

    public override string ToString() => Value;
}
