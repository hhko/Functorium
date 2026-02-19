namespace Functorium.Domains.Entities;

/// <summary>
/// 소프트 삭제 지원 인터페이스.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// 삭제 시각.
    /// </summary>
    Option<DateTime> DeletedAt { get; }

    /// <summary>
    /// 삭제 여부 (DeletedAt에서 파생).
    /// </summary>
    bool IsDeleted => DeletedAt.IsSome;
}

/// <summary>
/// 소프트 삭제 및 삭제자 추적 인터페이스.
/// </summary>
public interface ISoftDeletableWithUser : ISoftDeletable
{
    /// <summary>
    /// 삭제자 식별자.
    /// </summary>
    Option<string> DeletedBy { get; }
}
