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
        var tags = _tags.Value ??= [];
        var entry = new KeyValuePair<string, object?>(name, value);
        tags.Add(entry);
        return new MetricsTagDisposable(tags, entry);
    }

    /// <summary>
    /// 현재 AsyncLocal에 저장된 MetricsTag ctx.* 필드 목록을 반환합니다.
    /// UsecaseMetricsPipeline에서 표준 태그 생성 후 TagList에 병합할 때 사용합니다.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<string, object?>>? CurrentTags => _tags.Value;

    /// <summary>
    /// 현재 AsyncLocal에 저장된 MetricsTag ctx.* 필드가 있는지 확인합니다.
    /// </summary>
    public static bool HasTags => _tags.Value is { Count: > 0 };

    private sealed class MetricsTagDisposable(
        List<KeyValuePair<string, object?>> tags,
        KeyValuePair<string, object?> entry) : IDisposable
    {
        public void Dispose() => tags.Remove(entry);
    }
}
