using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace TransactionCachingPipeline;

/// <summary>
/// Transaction Pipeline: Command에만 적용, 성공 시 커밋
/// </summary>
public sealed class SimpleTransactionPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public List<string> Actions { get; } = [];

    public TResponse Execute(bool isCommand, Func<TResponse> handler)
    {
        if (!isCommand)
        {
            Actions.Add("Skip: not a command");
            return handler();
        }

        Actions.Add("Begin transaction");
        var response = handler();

        if (response.IsSucc)
        {
            Actions.Add("Commit");
        }
        else
        {
            Actions.Add("Rollback");
        }

        return response;
    }
}

/// <summary>
/// Caching Pipeline: Query에만 적용, 성공 응답만 캐싱
/// </summary>
public sealed class SimpleCachingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    private readonly Dictionary<string, TResponse> _cache = [];

    public TResponse GetOrExecute(string cacheKey, bool isCacheable, Func<TResponse> handler)
    {
        if (!isCacheable)
            return handler();

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var response = handler();

        // 성공 응답만 캐싱
        if (response.IsSucc)
            _cache[cacheKey] = response;

        return response;
    }
}
