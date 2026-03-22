using Functorium.Domains.Entities;

namespace Functorium.Domains.Events;

/// <summary>
/// Aggregate 로드 없이 벌크(Bulk) 삭제 사실을 알리는 경량 이벤트.
/// ExecuteDeleteAsync/ExecuteUpdateAsync 등 직접 SQL 삭제에서 사용합니다.
/// </summary>
public sealed record BulkDeletedEvent : DomainEvent
{
    public BulkDeletedEvent(Seq<string> deletedIds, int affectedCount) : base()
    {
        DeletedIds = deletedIds;
        AffectedCount = affectedCount;
    }

    /// <summary>삭제된 엔티티 ID 목록 (문자열 표현)</summary>
    public Seq<string> DeletedIds { get; }

    /// <summary>실제 삭제된 행 수</summary>
    public int AffectedCount { get; }

    /// <summary>
    /// EntityId 타입에서 BulkDeletedEvent를 생성합니다.
    /// </summary>
    public static BulkDeletedEvent From<TId>(IReadOnlyList<TId> ids, int affectedCount)
        where TId : struct, IEntityId<TId>
        => new(toSeq(ids.Select(id => id.ToString()!)), affectedCount);
}
