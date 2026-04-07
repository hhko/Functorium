using Functorium.Applications.Observabilities;
using Functorium.Applications.Usecases;

using Mediator;

namespace Functorium.Adapters.Pipelines;

/// <summary>
/// ctx.* 사용자 정의 컨텍스트 필드를 3-Pillar(Logging, Tracing, Metrics)에 전파하는 최선두 Pipeline.
/// IUsecaseCtxEnricher가 DI에 등록되어 있으면 Request/Response 시점에
/// CtxEnricherContext.Push를 통해 모든 대상 Pillar에 동시 전파합니다.
///
/// 파이프라인 실행 순서:
/// Request → CtxEnricher → Metrics → Tracing → Logging → Validation → ... → Handler
/// </summary>
internal sealed class CtxEnricherPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    private readonly IUsecaseCtxEnricher<TRequest, TResponse>? _enricher;

    public CtxEnricherPipeline(
        IUsecaseCtxEnricher<TRequest, TResponse>? enricher = null)
    {
        _enricher = enricher;
    }

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (_enricher is null)
        {
            return await next(request, cancellationToken);
        }

        // Request 시점: ctx.* 필드를 Logging/Tracing/MetricsTag에 동시 전파
        using IDisposable? requestEnrichment = _enricher.EnrichRequest(request);

        TResponse response = await next(request, cancellationToken);

        // Response 시점: ctx.* 필드를 Logging/Tracing/MetricsTag에 동시 전파
        using IDisposable? responseEnrichment = _enricher.EnrichResponse(request, response);

        return response;
    }
}
