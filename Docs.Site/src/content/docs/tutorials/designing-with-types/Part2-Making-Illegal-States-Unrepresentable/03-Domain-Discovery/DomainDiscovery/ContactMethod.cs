namespace DomainDiscovery;

/// <summary>
/// 연락 방법 — 타입 리팩터링에서 발견된 도메인 개념
/// </summary>
public abstract record ContactMethod
{
    public sealed record Email(string Address) : ContactMethod;
    public sealed record PostalMail(string FullAddress) : ContactMethod;
    public sealed record Phone(string Number) : ContactMethod;

    private ContactMethod() { }
}

/// <summary>
/// ContactMethod 처리기 — 패턴 매칭으로 모든 케이스 처리
/// </summary>
public static class ContactMethodHandler
{
    public static string Describe(ContactMethod method) => method switch
    {
        ContactMethod.Email e => $"이메일: {e.Address}",
        ContactMethod.PostalMail p => $"우편: {p.FullAddress}",
        ContactMethod.Phone ph => $"전화: {ph.Number}",
        _ => throw new InvalidOperationException("알 수 없는 연락 방법")
    };
}
