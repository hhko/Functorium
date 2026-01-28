namespace Functorium.Domains.Entities;

/// <summary>
/// 생성/수정 시각 추적 인터페이스.
/// </summary>
public interface IAuditable
{
    /// <summary>
    /// 생성 시각.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// 최종 수정 시각.
    /// </summary>
    DateTime? UpdatedAt { get; }
}

/// <summary>
/// 생성/수정 시각 및 사용자 추적 인터페이스.
/// </summary>
public interface IAuditableWithUser : IAuditable
{
    /// <summary>
    /// 생성자 식별자.
    /// </summary>
    string? CreatedBy { get; }

    /// <summary>
    /// 최종 수정자 식별자.
    /// </summary>
    string? UpdatedBy { get; }
}
