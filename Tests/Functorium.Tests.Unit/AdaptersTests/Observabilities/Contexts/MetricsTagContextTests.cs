using Functorium.Adapters.Observabilities.Contexts;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.Observabilities.Contexts;

[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters)]
public class MetricsTagContextTests
{
    #region Push Tests

    [Fact]
    public void Push_AddsTag_WhenCalled()
    {
        // Arrange & Act
        using var disposable = MetricsTagContext.Push("key", "value");

        // Assert
        var actual = MetricsTagContext.CurrentTags;
        actual.ShouldNotBeNull();
        actual.Count.ShouldBe(1);
        actual[0].Key.ShouldBe("key");
        actual[0].Value.ShouldBe("value");
    }

    #endregion

    #region CurrentTags Tests

    [Fact]
    public async Task CurrentTags_ReturnsNull_WhenNoTagsPushed()
    {
        // Arrange — AsyncLocal은 새 async flow에서 null
        // Act & Assert
        await Task.Run(() =>
        {
            MetricsTagContext.CurrentTags.ShouldBeNull();
        });
    }

    #endregion

    #region HasTags Tests

    [Fact]
    public async Task HasTags_ReturnsFalse_WhenNoTagsPushed()
    {
        // Arrange — AsyncLocal은 새 async flow에서 null
        // Act & Assert
        await Task.Run(() =>
        {
            MetricsTagContext.HasTags.ShouldBeFalse();
        });
    }

    [Fact]
    public void HasTags_ReturnsTrue_WhenTagExists()
    {
        // Arrange
        using var disposable = MetricsTagContext.Push("key", "value");

        // Act
        var actual = MetricsTagContext.HasTags;

        // Assert
        actual.ShouldBeTrue();
    }

    #endregion

    #region LIFO Dispose Tests

    [Fact]
    public void Dispose_RemovesLastTag_WhenCalledInLifoOrder()
    {
        // Arrange
        var disposableA = MetricsTagContext.Push("tagA", "valueA");
        var disposableB = MetricsTagContext.Push("tagB", "valueB");
        var disposableC = MetricsTagContext.Push("tagC", "valueC");

        // Act & Assert — LIFO 순서: C → B → A
        disposableC.Dispose();
        MetricsTagContext.CurrentTags!.Count.ShouldBe(2);
        MetricsTagContext.CurrentTags[0].Key.ShouldBe("tagA");
        MetricsTagContext.CurrentTags[1].Key.ShouldBe("tagB");

        disposableB.Dispose();
        MetricsTagContext.CurrentTags!.Count.ShouldBe(1);
        MetricsTagContext.CurrentTags[0].Key.ShouldBe("tagA");

        disposableA.Dispose();
        MetricsTagContext.CurrentTags!.Count.ShouldBe(0);
        MetricsTagContext.HasTags.ShouldBeFalse();
    }

    [Fact]
    public void Dispose_RemovesAllTags_WhenMultipleDisposedInLifoOrder()
    {
        // Arrange
        var disposables = Enumerable.Range(1, 5)
            .Select(i => MetricsTagContext.Push($"tag{i}", $"value{i}"))
            .ToList();

        // Act — LIFO 순서로 전부 Dispose
        for (int i = disposables.Count - 1; i >= 0; i--)
            disposables[i].Dispose();

        // Assert
        MetricsTagContext.CurrentTags!.Count.ShouldBe(0);
        MetricsTagContext.HasTags.ShouldBeFalse();
    }

    [Fact]
    public void Dispose_MaintainsRemainingTags_WhenPartiallyDisposed()
    {
        // Arrange
        var disposableA = MetricsTagContext.Push("tagA", "valueA");
        var disposableB = MetricsTagContext.Push("tagB", "valueB");
        var disposableC = MetricsTagContext.Push("tagC", "valueC");

        // Act — C만 Dispose (LIFO 최상위)
        disposableC.Dispose();

        // Assert — A, B만 남아있어야 한다
        var actual = MetricsTagContext.CurrentTags;
        actual.ShouldNotBeNull();
        actual.Count.ShouldBe(2);
        actual[0].Key.ShouldBe("tagA");
        actual[0].Value.ShouldBe("valueA");
        actual[1].Key.ShouldBe("tagB");
        actual[1].Value.ShouldBe("valueB");

        // Cleanup
        disposableB.Dispose();
        disposableA.Dispose();
    }

    #endregion
}
