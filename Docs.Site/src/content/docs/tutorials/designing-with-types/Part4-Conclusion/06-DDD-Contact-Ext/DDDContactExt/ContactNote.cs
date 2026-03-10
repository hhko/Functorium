namespace DDDContactExt;

/// <summary>
/// 연락처 메모 자식 엔티티
/// Contact Aggregate의 하위 엔티티로, NoteContent VO를 포함합니다.
/// </summary>
[GenerateEntityId]
public sealed class ContactNote : Entity<ContactNoteId>
{
    public NoteContent Content { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ContactNote(ContactNoteId id, NoteContent content) : base(id)
    {
        Content = content;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Create: 검증된 VO를 받으므로 직접 반환 (Fin 불필요)
    /// </summary>
    public static ContactNote Create(NoteContent content)
    {
        return new ContactNote(ContactNoteId.New(), content);
    }

    /// <summary>
    /// CreateFromValidated: ORM/Repository 복원용 (검증 없음)
    /// </summary>
    public static ContactNote CreateFromValidated(
        ContactNoteId id, NoteContent content, DateTime createdAt) =>
        new(id, content) { CreatedAt = createdAt };
}
