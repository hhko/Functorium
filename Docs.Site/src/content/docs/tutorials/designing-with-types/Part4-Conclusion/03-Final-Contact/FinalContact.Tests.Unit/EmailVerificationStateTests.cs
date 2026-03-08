using FinalContact;

namespace FinalContact.Tests.Unit;

/// <summary>
/// EmailVerificationState 상태 전이 테스트
///
/// 테스트 목적:
/// 1. Unverified → Verified 전이 성공
/// 2. Verified → Verified 전이 실패 (Fin.Fail)
/// 3. 전이 후 EmailAddress 보존 확인
/// </summary>
[Trait("Part4-Conclusion", "03-FinalContact")]
public class EmailVerificationStateTests
{
    [Fact]
    public void Verify_ReturnsSuccess_WhenUnverified()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com")
            .Match(Succ: v => v, Fail: _ => throw new Exception("EmailAddress 생성 실패"));
        EmailVerificationState state = new EmailVerificationState.Unverified(email);
        var verifiedAt = new DateTime(2024, 1, 15);

        // Act
        var actual = EmailVerificationState.Verify(state, verifiedAt);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: s =>
            {
                s.ShouldBeOfType<EmailVerificationState.Verified>();
                var verified = (EmailVerificationState.Verified)s;
                verified.Email.ShouldBe(email);
                verified.VerifiedAt.ShouldBe(verifiedAt);
            },
            Fail: _ => throw new Exception("전이 실패"));
    }

    [Fact]
    public void Verify_ReturnsFail_WhenAlreadyVerified()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com")
            .Match(Succ: v => v, Fail: _ => throw new Exception("EmailAddress 생성 실패"));
        EmailVerificationState state = new EmailVerificationState.Verified(email, DateTime.Now);

        // Act
        var actual = EmailVerificationState.Verify(state, DateTime.Now);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Verify_PreservesEmailAddress_AfterTransition()
    {
        // Arrange
        var email = EmailAddress.Create("test@domain.com")
            .Match(Succ: v => v, Fail: _ => throw new Exception("EmailAddress 생성 실패"));
        EmailVerificationState state = new EmailVerificationState.Unverified(email);

        // Act
        var actual = EmailVerificationState.Verify(state, DateTime.Now);

        // Assert
        actual.Match(
            Succ: s =>
            {
                var verified = (EmailVerificationState.Verified)s;
                verified.Email.ShouldBe(email);
            },
            Fail: _ => throw new Exception("전이 실패"));
    }
}
