namespace StateAsEnum;

public enum VerificationStatus
{
    Unverified,
    Verified
}

/// <summary>
/// 나이브한 이메일 인증 상태 — enum + nullable 필드
/// "미인증인데 인증일이 있는" 불법 상태 허용
/// </summary>
public class EmailState
{
    public required string Email { get; set; }
    public required VerificationStatus Status { get; set; }
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// 런타임 유효성 검사 — 컴파일 타임에는 잡을 수 없음
    /// </summary>
    public bool IsValid() => Status switch
    {
        VerificationStatus.Unverified => VerifiedAt is null,
        VerificationStatus.Verified => VerifiedAt is not null,
        _ => false
    };
}
