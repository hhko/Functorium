namespace DDDContact;

/// <summary>
/// 이메일 인증 상태 (Part 3 상태 기계 통합)
/// Unverified → Verified 단방향 전이만 허용
/// 상태 전이 로직은 Contact 애그리거트가 소유
/// </summary>
public abstract record EmailVerificationState
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState;

    private EmailVerificationState() { }
}
