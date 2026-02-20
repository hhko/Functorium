namespace LayeredArch.Domain.AggregateRoots.Tags;

/// <summary>
/// 태그 도메인 모델 (Aggregate Root)
/// TagId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class Tag : AggregateRoot<TagId>, IAuditable
{
    #region Domain Events

    /// <summary>
    /// 태그 생성 이벤트
    /// </summary>
    public sealed record CreatedEvent(TagId TagId, TagName TagName) : DomainEvent;

    /// <summary>
    /// 태그 이름 변경 이벤트
    /// </summary>
    public sealed record RenamedEvent(TagId TagId, TagName OldName, TagName NewName) : DomainEvent;

    #endregion

    public TagName Name { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }

    private Tag(TagId id, TagName name) : base(id)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 새 태그를 생성합니다.
    /// </summary>
    public static Tag Create(TagName name)
    {
        var tag = new Tag(TagId.New(), name);
        tag.AddDomainEvent(new CreatedEvent(tag.Id, name));
        return tag;
    }

    /// <summary>
    /// 태그 이름을 변경합니다.
    /// </summary>
    public Tag Rename(TagName newName)
    {
        var oldName = Name;
        Name = newName;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new RenamedEvent(Id, oldName, newName));
        return this;
    }

    /// <summary>
    /// ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static Tag CreateFromValidated(
        TagId id,
        TagName name,
        DateTime createdAt,
        Option<DateTime> updatedAt) =>
        new(id, name)
        {
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
}
