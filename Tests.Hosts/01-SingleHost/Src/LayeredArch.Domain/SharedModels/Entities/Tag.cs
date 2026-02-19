namespace LayeredArch.Domain.SharedModels.Entities;

/// <summary>
/// 태그 엔티티 (SharedModels 빌딩 블록)
/// 여러 Aggregate에서 공유 가능한 엔티티입니다.
/// TagId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class Tag : Entity<TagId>
{
    /// <summary>
    /// Aggregate에 태그가 할당될 때 발행되는 공유 도메인 이벤트
    /// </summary>
    public sealed record AssignedEvent(TagId TagId, TagName TagName) : DomainEvent;

    /// <summary>
    /// Aggregate에서 태그가 제거될 때 발행되는 공유 도메인 이벤트
    /// </summary>
    public sealed record RemovedEvent(TagId TagId) : DomainEvent;

    public TagName Name { get; private set; }

    private Tag(TagId id, TagName name) : base(id)
    {
        Name = name;
    }

    /// <summary>
    /// 새 태그를 생성합니다.
    /// </summary>
    public static Tag Create(TagName name) =>
        new(TagId.New(), name);

    /// <summary>
    /// ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Tag CreateFromValidated(TagId id, TagName name) =>
        new(id, name);
}
