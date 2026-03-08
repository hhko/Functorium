namespace StateAsEnum.Tests.Unit;

/// <summary>
/// Enum 상태의 불법 상태 테스트
///
/// 테스트 목적:
/// 1. "미인증인데 인증일이 있는" 불법 상태가 생성 가능함을 증명
/// 2. 런타임 검증에 의존해야 함을 시연
/// </summary>
[Trait("Part3-StateMachines", "01-StateAsEnum")]
public class EmailStateTests
{
    [Fact]
    public void EmailState_AllowsIllegalState_UnverifiedWithDate()
    {
        // Act — 불법 상태: 미인증인데 인증일이 있음
        var illegal = new EmailState
        {
            Email = "user@example.com",
            Status = VerificationStatus.Unverified,
            VerifiedAt = DateTime.Now
        };

        // Assert — 컴파일러가 이를 허용함
        illegal.Status.ShouldBe(VerificationStatus.Unverified);
        illegal.VerifiedAt.ShouldNotBeNull();
        illegal.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void EmailState_AllowsIllegalState_VerifiedWithoutDate()
    {
        // Act — 불법 상태: 인증인데 인증일이 없음
        var illegal = new EmailState
        {
            Email = "user@example.com",
            Status = VerificationStatus.Verified,
            VerifiedAt = null
        };

        // Assert
        illegal.Status.ShouldBe(VerificationStatus.Verified);
        illegal.VerifiedAt.ShouldBeNull();
        illegal.IsValid().ShouldBeFalse();
    }

    [Fact]
    public void EmailState_IsValid_WhenUnverifiedWithNoDate()
    {
        // Act
        var state = new EmailState
        {
            Email = "user@example.com",
            Status = VerificationStatus.Unverified,
            VerifiedAt = null
        };

        // Assert
        state.IsValid().ShouldBeTrue();
    }

    [Fact]
    public void EmailState_IsValid_WhenVerifiedWithDate()
    {
        // Act
        var state = new EmailState
        {
            Email = "user@example.com",
            Status = VerificationStatus.Verified,
            VerifiedAt = DateTime.Now
        };

        // Assert
        state.IsValid().ShouldBeTrue();
    }
}
