namespace StateTransitions.Tests.Unit;

/// <summary>
/// 상태 전이 테스트
///
/// 테스트 목적:
/// 1. 유효 전이(Unverified → Verified) 성공
/// 2. 무효 전이(Verified → Verified) Fin.Fail 반환
/// </summary>
[Trait("Part3-StateMachines", "03-StateTransitions")]
public class EmailVerificationStateTests
{
    [Fact]
    public void Verify_ReturnsSuccess_WhenUnverified()
    {
        // Arrange
        EmailVerificationState state = new EmailVerificationState.Unverified("user@example.com");
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
                verified.Email.ShouldBe("user@example.com");
                verified.VerifiedAt.ShouldBe(verifiedAt);
            },
            Fail: _ => throw new Exception("전이 실패"));
    }

    [Fact]
    public void Verify_ReturnsFail_WhenAlreadyVerified()
    {
        // Arrange
        EmailVerificationState state = new EmailVerificationState.Verified("user@example.com", DateTime.Now);

        // Act
        var actual = EmailVerificationState.Verify(state, DateTime.Now);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }
}
