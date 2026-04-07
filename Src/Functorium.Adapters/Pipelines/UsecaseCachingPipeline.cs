using Functorium.Applications.Usecases;

using Mediator;

using Microsoft.Extensions.Caching.Memory;

namespace Functorium.Adapters.Pipelines;

/// <summary>
/// ICacheable을 구현한 Query 요청에 대한 캐싱 Pipeline.
/// Validation 후, Transaction 전 위치에서 실행됩니다.
/// 캐시 히트 시 DB 라운드트립 없이 즉시 반환합니다.
/// </summary>
/// <remarks>
/// <para>where TRequest : IQuery&lt;TResponse&gt; 제약 조건으로 Query에만 적용됩니다.</para>
/// </remarks>
internal sealed class UsecaseCachingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    private readonly IMemoryCache _cache;

    public UsecaseCachingPipeline(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheable cacheable)
            return await next(request, cancellationToken);

        if (_cache.TryGetValue(cacheable.CacheKey, out TResponse? cached) && cached is not null)
            return cached;

        var response = await next(request, cancellationToken);

        if (response.IsSucc)
        {
            var options = new MemoryCacheEntryOptions();
            if (cacheable.Duration is { } duration)
                options.AbsoluteExpirationRelativeToNow = duration;
            else
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);

            _cache.Set(cacheable.CacheKey, response, options);
        }

        return response;
    }
}
