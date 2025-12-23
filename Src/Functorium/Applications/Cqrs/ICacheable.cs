namespace Functorium.Applications.Cqrs;

public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Duration { get; }
}