
namespace DesigningWithTypes.Tests.Unit;

/// <summary>
/// EmailVerificationState 상태 VO 테스트
/// </summary>
[Trait("Sample", "DesigningWithTypes")]
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
        EmailVerificationState verified = new EmailVerificationState.Verified(email, new DateTime(2024, 1, 15));

        // Act & Assert
        unverified.ShouldBeOfType<EmailVerificationState.Unverified>();
        verified.ShouldBeOfType<EmailVerificationState.Verified>();
    }

    [Fact]
    public void Verify_ReturnsVerified_WhenUnverified()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        EmailVerificationState sut = new EmailVerificationState.Unverified(email);
        var verifiedAt = new DateTime(2024, 1, 15);

        // Act
        var actual = sut.Verify(verifiedAt);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        var verified = actual.ThrowIfFail();
        verified.Email.ShouldBe(email);
        verified.VerifiedAt.ShouldBe(verifiedAt);
    }

    [Fact]
    public void Verify_ReturnsFail_WhenAlreadyVerified()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        EmailVerificationState sut = new EmailVerificationState.Verified(email, new DateTime(2024, 1, 15));

        // Act
        var actual = sut.Verify(new DateTime(2024, 2, 1));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Match_CoversAllCases()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        EmailVerificationState sut = new EmailVerificationState.Unverified(email);

        // Act
        var actual = sut.Match(
            unverified: u => $"unverified:{u.Email}",
            verified: v => $"verified:{v.Email}");

        // Assert
        actual.ShouldBe("unverified:user@example.com");
    }

    [Fact]
    public void Switch_CoversAllCases()
    {
        // Arrange
        var email = EmailAddress.Create("user@example.com").ThrowIfFail();
        EmailVerificationState sut = new EmailVerificationState.Verified(email, new DateTime(2024, 1, 15));
        string? actual = null;

        // Act
        sut.Switch(
            unverified: _ => actual = "unverified",
            verified: _ => actual = "verified");

        // Assert
        actual.ShouldBe("verified");
    }
}
