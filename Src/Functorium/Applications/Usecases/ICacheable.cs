namespace Functorium.Applications.Usecases;

public interface ICacheable
{
    string CacheKey { get; }
    TimeSpan? Duration { get; }
}