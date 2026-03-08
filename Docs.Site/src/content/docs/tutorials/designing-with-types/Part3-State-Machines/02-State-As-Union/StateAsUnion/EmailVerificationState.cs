namespace StateAsUnion;

/// <summary>
/// 타입 안전한 이메일 인증 상태 — sealed record union
/// Verified는 항상 인증일을 보유, Unverified는 절대 보유하지 않음
/// </summary>
public abstract record EmailVerificationState
{
    public sealed record Unverified(string Email) : EmailVerificationState;
    public sealed record Verified(string Email, DateTime VerifiedAt) : EmailVerificationState;

    private EmailVerificationState() { }
}
