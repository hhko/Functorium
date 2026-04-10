namespace DesigningWithTypes.AggregateRoots.Contacts;

/// <summary>
/// 연락처 메모 자식 엔티티
/// Contact Aggregate의 하위 엔티티로, NoteContent VO를 포함합니다.
/// 생성 후 변경 불가(immutable)한 엔티티로, 식별자 기반 삭제(RemoveNote)를
/// 지원하기 위해 Entity로 모델링합니다.
/// </summary>
[GenerateEntityId]
public sealed class ContactNote : Entity<ContactNoteId>
{
    public NoteContent Content { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ContactNote(ContactNoteId id, NoteContent content, DateTime createdAt) : base(id)
    {
        Content = content;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Create: 검증된 VO를 받으므로 직접 반환 (도메인 내부용)
    /// </summary>
    public static ContactNote Create(NoteContent content, DateTime createdAt)
    {
        return new ContactNote(ContactNoteId.New(), content, createdAt);
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static ContactNote CreateFromValidated(
        ContactNoteId id, NoteContent content, DateTime createdAt) =>
        new(id, content, createdAt);
}
