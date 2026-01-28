namespace Functorium.Domains.Entities;

/// <summary>
/// 소프트 삭제 지원 인터페이스.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// 삭제 여부.
    /// </summary>
    bool IsDeleted { get; }

    /// <summary>
    /// 삭제 시각.
    /// </summary>
    DateTime? DeletedAt { get; }
}

/// <summary>
/// 소프트 삭제 및 삭제자 추적 인터페이스.
/// </summary>
public interface ISoftDeletableWithUser : ISoftDeletable
{
    /// <summary>
    /// 삭제자 식별자.
    /// </summary>
    string? DeletedBy { get; }
}
