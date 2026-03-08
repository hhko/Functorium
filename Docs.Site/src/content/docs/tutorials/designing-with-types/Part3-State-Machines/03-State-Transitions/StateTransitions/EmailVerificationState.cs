using LanguageExt;
using Functorium.Domains.Errors;

namespace StateTransitions;

/// <summary>
/// 상태 전이가 포함된 이메일 인증 상태
/// </summary>
public abstract record EmailVerificationState
{
    public sealed record Unverified(string Email) : EmailVerificationState;
    public sealed record Verified(string Email, DateTime VerifiedAt) : EmailVerificationState;

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
