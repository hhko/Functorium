namespace Functorium.Adapters.Observabilities.Contexts;

/// <summary>
/// CtxEnricherPipeline에서 Push된 MetricsTag ctx.* 필드를
/// UsecaseMetricsPipeline이 읽을 수 있도록 AsyncLocal 기반으로 공유하는 컨텍스트.
/// </summary>
public static class MetricsTagContext
{
    private static readonly AsyncLocal<List<KeyValuePair<string, object?>>?> _tags = new();

    /// <summary>
    /// MetricsTag ctx.* 필드를 AsyncLocal에 저장합니다.
    /// Dispose 시 해당 항목이 제거됩니다.
    /// </summary>
    public static IDisposable Push(string name, object? value)
    {
        var tags = _tags.Value ??= new(4);
        tags.Add(new KeyValuePair<string, object?>(name, value));
        return new MetricsTagDisposable(tags);
    }

    /// <summary>
    /// 현재 AsyncLocal에 저장된 MetricsTag ctx.* 필드 목록의 immutable snapshot을 반환합니다.
    /// UsecaseMetricsPipeline에서 표준 태그 생성 후 TagList에 병합할 때 사용합니다.
    /// 항목이 없거나 한 번도 Push되지 않았으면 null을 반환합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 내부 가변 List를 직접 노출하면 caller가 enumerate 중에 다른 위치(Push/Dispose)에서
    /// List가 변경될 경우 InvalidOperationException 또는 silent corruption이 발생할 수 있습니다.
    /// 매 호출마다 array snapshot을 만들어 race를 차단합니다 (Count > 0일 때만 할당).
    /// </para>
    /// <para>
    /// snapshot 도입 전에 잠재 race가 가능했던 안티패턴 예시:
    /// <code>
    /// async Task UserHandler()
    /// {
    ///     using var ctx = MetricsTagContext.Push("user.id", userId);
    ///
    ///     // fire-and-forget 자식 task — AsyncLocal flow를 캡처하므로 ctxTags를 공유
    ///     _ = Task.Run(async () =&gt;
    ///     {
    ///         // 자식 task가 ctxTags.foreach 중...
    ///     });
    ///
    ///     return; // ctx.Dispose() → 내부 List.RemoveAt() → 자식의 foreach와 race
    /// }
    /// </code>
    /// 또는 Parallel.ForEach / Task.WhenAll로 분기된 child들이 ctx를 enumerate하는 도중
    /// 부모 또는 다른 child가 Push/Dispose하는 패턴도 동일한 race를 유발합니다. snapshot
    /// 패턴은 이 모든 케이스에서 caller가 자기 소유 array를 enumerate하므로 안전합니다.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<KeyValuePair<string, object?>>? CurrentTags =>
        _tags.Value is { Count: > 0 } tags ? tags.ToArray() : null;

    /// <summary>
    /// 현재 AsyncLocal에 저장된 MetricsTag ctx.* 필드가 있는지 확인합니다.
    /// </summary>
    public static bool HasTags => _tags.Value is { Count: > 0 };

    private sealed class MetricsTagDisposable(
        List<KeyValuePair<string, object?>> tags) : IDisposable
    {
        public void Dispose() => tags.RemoveAt(tags.Count - 1);
    }
}
