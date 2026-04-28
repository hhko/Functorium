namespace VisibilityAndModifiers.Internal;

internal sealed class OrderCache
{
    private readonly Dictionary<string, string> _cache = new();

    public void Add(string key, string value) => _cache[key] = value;
}
