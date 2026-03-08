namespace UnionTypes;

/// <summary>
/// 타입 안전한 ContactInfo — sealed record union
/// "둘 다 없는" 불법 상태가 타입 수준에서 불가능
/// </summary>
public abstract record ContactInfo
{
    public sealed record EmailOnly(string Email) : ContactInfo;
    public sealed record PostalOnly(string Address) : ContactInfo;
    public sealed record EmailAndPostal(string Email, string Address) : ContactInfo;

    private ContactInfo() { } // 외부 상속 차단
}
