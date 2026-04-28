using IncrementalCaching.Generated;

namespace IncrementalCaching.Usage;

[Cached]
public partial class CachedService
{
    public string DoWork() => "Working...";
}
