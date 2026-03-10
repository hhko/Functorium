using DDDContactExt;

namespace DDDContactExt.Tests.Unit;

/// <summary>
/// ContactNote 자식 엔티티 테스트
/// </summary>
[Trait("Part4-Conclusion", "05-DDDContactExt")]
public class ContactNoteTests
{
    private static readonly DateTime Now = new(2024, 1, 1);

    [Fact]
    public void Create_ReturnsNoteWithContent()
    {
        // Arrange
        var content = NoteContent.Create("메모 내용").ThrowIfFail();

        // Act
        var actual = ContactNote.Create(content, Now);

        // Assert
        actual.Content.ShouldBe(content);
        actual.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public void Create_AssignsUniqueId()
    {
        // Arrange
        var content = NoteContent.Create("메모").ThrowIfFail();

        // Act
        var note1 = ContactNote.Create(content, Now);
        var note2 = ContactNote.Create(content, Now);

        // Assert
        note1.Id.ShouldNotBe(note2.Id);
    }

    [Fact]
    public void CreateFromValidated_RestoresProperties()
    {
        // Arrange
        var id = ContactNoteId.New();
        var content = NoteContent.CreateFromValidated("복원 메모");
        var createdAt = new DateTime(2024, 1, 1);

        // Act
        var actual = ContactNote.CreateFromValidated(id, content, createdAt);

        // Assert
        actual.Id.ShouldBe(id);
        actual.Content.ShouldBe(content);
        actual.CreatedAt.ShouldBe(createdAt);
    }
}
