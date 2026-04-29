using System.Collections.Concurrent;
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
        // Count == 0이면 CurrentTags는 null을 반환합니다 (snapshot 패턴).
        MetricsTagContext.CurrentTags.ShouldBeNull();
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

        // Assert — Count == 0이면 CurrentTags는 null
        MetricsTagContext.CurrentTags.ShouldBeNull();
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

    #region Snapshot Race-Free Tests

    /// <summary>
    /// snapshot 패턴 검증: CurrentTags가 반환한 값은 immutable snapshot이어야 한다.
    /// 즉 caller가 snapshot을 받은 뒤 후속 Push/Dispose가 일어나도 snapshot의 Count·내용은
    /// 변하지 않아야 한다(내부 mutable List 직접 노출 시 발생하던 race를 차단).
    /// </summary>
    [Fact]
    public void CurrentTags_ReturnsImmutableSnapshot_NotAffectedByLaterMutation()
    {
        // Arrange
        using var disposable1 = MetricsTagContext.Push("k1", "v1");

        // Act — 첫 번째 snapshot 캡처
        var snapshot1 = MetricsTagContext.CurrentTags;
        snapshot1.ShouldNotBeNull();
        snapshot1.Count.ShouldBe(1);

        // 추가 Push (내부 List 변경)
        var disposable2 = MetricsTagContext.Push("k2", "v2");

        // Assert — 첫 번째 snapshot은 변경에 영향 받지 않음
        snapshot1.Count.ShouldBe(1);
        snapshot1[0].Key.ShouldBe("k1");

        // 두 번째 snapshot은 새 상태 반영
        var snapshot2 = MetricsTagContext.CurrentTags;
        snapshot2.ShouldNotBeNull();
        snapshot2.Count.ShouldBe(2);

        // Dispose 후에도 첫 번째 snapshot 그대로
        disposable2.Dispose();
        snapshot1.Count.ShouldBe(1);
        snapshot2.Count.ShouldBe(2);
    }

    /// <summary>
    /// concurrent Push/Dispose vs enumerate race 차단 검증.
    /// 내부 List를 직접 노출하던 옛 구현에서는 enumerate 도중 다른 위치의 Push/Dispose가
    /// List를 변경하면 InvalidOperationException("Collection was modified")이 발생.
    /// snapshot 패턴은 caller가 자기 소유 array를 enumerate하므로 race 차단.
    /// </summary>
    [Fact]
    public async Task CurrentTags_NoCollectionModifiedException_WhenConcurrentPushAndEnumerate()
    {
        // Arrange — 같은 AsyncLocal flow에서 시작 태그 1개
        using var initialCtx = MetricsTagContext.Push("initial", "v");

        const int iterations = 500;
        var exceptions = new ConcurrentBag<Exception>();

        // Act — 자식 task가 반복 enumerate, 부모는 동시에 Push/Dispose 반복
        var enumerateTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                try
                {
                    var snapshot = MetricsTagContext.CurrentTags;
                    if (snapshot is not null)
                    {
                        foreach (var _ in snapshot)
                        {
                            // simulate enumeration work
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        });

        for (int i = 0; i < iterations; i++)
        {
            var temp = MetricsTagContext.Push($"k{i}", $"v{i}");
            temp.Dispose();
        }

        await enumerateTask;

        // Assert — snapshot 패턴이 race 차단
        exceptions.ShouldBeEmpty();
    }

    #endregion
}
