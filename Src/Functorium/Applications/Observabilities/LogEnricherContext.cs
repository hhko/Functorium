namespace Functorium.Applications.Observabilities;

public static class LogEnricherContext
{
    private static Func<string, object?, IDisposable> _pushProperty = static (_, _) => NullDisposable.Instance;

    public static void SetPushPropertyFactory(Func<string, object?, IDisposable> factory)
        => _pushProperty = factory ?? throw new ArgumentNullException(nameof(factory));

    public static IDisposable PushProperty(string name, object? value) => _pushProperty(name, value);

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}
