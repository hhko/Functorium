using LanguageExt;
using Functorium.Domains.Errors;

namespace FinalContact;

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
    /// 인증 전이: Unverified → Verified
    /// 이미 인증된 상태에서는 Fail 반환
    /// </summary>
    public static Fin<EmailVerificationState> Verify(
        EmailVerificationState state, DateTime verifiedAt) => state switch
    {
        Unverified u => new Verified(u.Email, verifiedAt),
        Verified => Fin.Fail<EmailVerificationState>(
            DomainError.For<EmailVerificationState>(
                new DomainErrorType.InvalidTransition(FromState: "Verified", ToState: "Verified"),
                state.ToString()!,
                "이미 인증된 이메일입니다")),
        _ => throw new InvalidOperationException()
    };
}
