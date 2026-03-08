namespace StateAsUnion.Tests.Unit;

/// <summary>
/// Union 상태 테스트
///
/// 테스트 목적:
/// 1. Verified는 항상 인증일을 보유함을 확인
/// 2. Unverified는 인증일이 없음을 확인
/// </summary>
[Trait("Part3-StateMachines", "02-StateAsUnion")]
public class EmailVerificationStateTests
{
    [Fact]
    public void Unverified_HasEmailOnly()
    {
        // Act
        var state = new EmailVerificationState.Unverified("user@example.com");

        // Assert
        state.Email.ShouldBe("user@example.com");
    }

    [Fact]
    public void Verified_HasEmailAndDate()
    {
        // Arrange
        var verifiedAt = new DateTime(2024, 1, 15);

        // Act
        var state = new EmailVerificationState.Verified("user@example.com", verifiedAt);

        // Assert
        state.Email.ShouldBe("user@example.com");
        state.VerifiedAt.ShouldBe(verifiedAt);
    }

    [Fact]
    public void Switch_CoversAllCases()
    {
        // Arrange
        EmailVerificationState state = new EmailVerificationState.Unverified("test@test.com");

        // Act
        var hasDate = state switch
        {
            EmailVerificationState.Unverified => false,
            EmailVerificationState.Verified => true,
            _ => throw new InvalidOperationException()
        };

        // Assert
        hasDate.ShouldBeFalse();
    }
}
