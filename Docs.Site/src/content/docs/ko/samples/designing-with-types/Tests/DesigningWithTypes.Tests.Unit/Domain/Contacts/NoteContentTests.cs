
namespace DesigningWithTypes.Tests.Unit;

/// <summary>
/// NoteContent 값 객체 테스트
/// </summary>
[Trait("Sample", "DesigningWithTypes")]
public class NoteContentTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = NoteContent.Create("메모 내용입니다");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenNull()
    {
        // Act
        var actual = NoteContent.Create(null);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenEmpty()
    {
        // Act
        var actual = NoteContent.Create("");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenTooLong()
    {
        // Act
        var actual = NoteContent.Create(new string('a', 501));

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        // Act
        var actual = NoteContent.Create("  메모 내용  ").ThrowIfFail();

        // Assert
        string value = actual;
        value.ShouldBe("메모 내용");
    }

    [Fact]
    public void CreateFromValidated_CreatesDirectly()
    {
        // Act
        var actual = NoteContent.CreateFromValidated("메모");

        // Assert
        string value = actual;
        value.ShouldBe("메모");
    }

    [Fact]
    public void Create_ReturnsSuccess_WithExactly500Chars()
    {
        // Act
        var actual = NoteContent.Create(new string('a', 500));

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }
}
