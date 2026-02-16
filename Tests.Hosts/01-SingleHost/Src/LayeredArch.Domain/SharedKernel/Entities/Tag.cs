namespace LayeredArch.Domain.SharedKernel.Entities;

/// <summary>
/// 태그 엔티티 (SharedKernel 빌딩 블록)
/// 여러 Aggregate에서 공유 가능한 엔티티입니다.
/// TagId는 [GenerateEntityId] 속성에 의해 소스 생성기로 자동 생성됩니다.
/// </summary>
[GenerateEntityId]
public sealed class Tag : Entity<TagId>
{
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
