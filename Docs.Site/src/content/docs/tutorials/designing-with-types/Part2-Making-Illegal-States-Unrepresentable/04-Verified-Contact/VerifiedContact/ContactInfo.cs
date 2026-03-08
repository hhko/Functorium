namespace VerifiedContact;

/// <summary>
/// 타입 안전한 ContactInfo — sealed record union
/// </summary>
public abstract record ContactInfo
{
    public sealed record EmailOnly(EmailAddress Email) : ContactInfo;
    public sealed record PostalOnly(string Address) : ContactInfo;
    public sealed record EmailAndPostal(EmailAddress Email, string Address) : ContactInfo;

    private ContactInfo() { }
}
