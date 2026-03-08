namespace BooleanBlindness;

/// <summary>
/// 나이브한 ContactInfo — nullable 필드로 표현
/// 이메일과 우편 주소 모두 없는 불법 상태 허용
/// </summary>
public class ContactInfo
{
    public string? EmailAddress { get; set; }
    public string? PostalAddress { get; set; }

    /// <summary>
    /// 런타임 검증 — 컴파일 타임에는 잡을 수 없음
    /// </summary>
    public bool IsValid() =>
        EmailAddress is not null || PostalAddress is not null;
}
