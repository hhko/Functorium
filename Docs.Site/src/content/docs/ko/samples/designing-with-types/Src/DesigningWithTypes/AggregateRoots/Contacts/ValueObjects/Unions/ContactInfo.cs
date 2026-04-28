namespace DesigningWithTypes.AggregateRoots.Contacts.ValueObjects;

/// <summary>
/// 연락처 정보 union (Part 2 + Part 3 통합)
/// 최소 하나의 연락 수단이 항상 존재
/// Email 케이스는 EmailVerificationState를 포함하여 인증 상태를 추적
/// </summary>
[UnionType]
public abstract partial record ContactInfo : UnionValueObject
{
    public sealed record EmailOnly(EmailVerificationState EmailState) : ContactInfo;
    public sealed record PostalOnly(PostalAddress Address) : ContactInfo;
    public sealed record EmailAndPostal(EmailVerificationState EmailState, PostalAddress Address) : ContactInfo;

    private ContactInfo() { }
}
