namespace DesigningWithTypes.AggregateRoots.Contacts.ValueObjects;

/// <summary>
/// 이메일 인증 상태 (Part 3 상태 기계 통합)
/// Unverified → Verified 단방향 전이만 허용
/// </summary>
[UnionType]
public abstract partial record EmailVerificationState : UnionValueObject<EmailVerificationState>
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState;

    private EmailVerificationState() { }

    /// <summary>
    /// Unverified → Verified 전이. Verified 상태에서는 실패를 반환합니다.
    /// </summary>
    public Fin<Verified> Verify(DateTime verifiedAt) =>
        TransitionFrom<Unverified, Verified>(
            u => new Verified(u.Email, verifiedAt));
}
