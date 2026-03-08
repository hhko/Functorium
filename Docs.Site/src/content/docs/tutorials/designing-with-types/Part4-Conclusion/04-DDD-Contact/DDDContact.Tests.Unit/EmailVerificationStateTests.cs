using DDDContact;

namespace DDDContact.Tests.Unit;

/// <summary>
/// EmailVerificationState 상태 VO 테스트
///
/// 테스트 목적:
/// 1. Unverified/Verified 상태 생성
/// 2. 패턴 매칭을 통한 이메일 접근
/// </summary>
[Trait("Part4-Conclusion", "04-DDDContact")]
public class EmailVerificationStateTests
{
    [Fact]
    public void Unverified_StoresEmail()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();

        // Act
        var actual = new EmailVerificationState.Unverified(email);

        // Assert
        actual.Email.ShouldBe(email);
    }

    [Fact]
    public void Verified_StoresEmailAndDate()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        var verifiedAt = new DateTime(2024, 1, 15);

        // Act
        var actual = new EmailVerificationState.Verified(email, verifiedAt);

        // Assert
        actual.Email.ShouldBe(email);
        actual.VerifiedAt.ShouldBe(verifiedAt);
    }

    [Fact]
    public void PatternMatch_DistinguishesStates()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        EmailVerificationState unverified = new EmailVerificationState.Unverified(email);
        EmailVerificationState verified = new EmailVerificationState.Verified(email, DateTime.UtcNow);

        // Act & Assert
        unverified.ShouldBeOfType<EmailVerificationState.Unverified>();
        verified.ShouldBeOfType<EmailVerificationState.Verified>();
    }
}
