namespace DDDContactExt;

/// <summary>
/// 이메일 인증 상태 (Part 3 상태 기계 통합)
/// Unverified → Verified 단방향 전이만 허용
/// </summary>
public abstract record EmailVerificationState
{
    public sealed record Unverified(EmailAddress Email) : EmailVerificationState;
    public sealed record Verified(EmailAddress Email, DateTime VerifiedAt) : EmailVerificationState;

    private EmailVerificationState() { }

    /// <summary>
    /// Unverified → Verified 전이. Verified 상태에서는 실패를 반환합니다.
    /// </summary>
    public Fin<Verified> Verify(DateTime verifiedAt) => this switch
    {
        Unverified u => new Verified(u.Email, verifiedAt),
        Verified => Fin.Fail<Verified>(
            DomainError.For<EmailVerificationState>(
                new DomainErrorType.InvalidTransition(
                    FromState: "Verified", ToState: "Verified"),
                ToString()!,
                "이미 인증된 이메일입니다")),
        _ => throw new InvalidOperationException()
    };
}
