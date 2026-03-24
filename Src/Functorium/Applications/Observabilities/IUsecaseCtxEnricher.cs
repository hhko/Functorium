using Functorium.Applications.Usecases;

namespace Functorium.Applications.Observabilities;

/// <summary>
/// Usecase의 비즈니스 컨텍스트 필드를 3-Pillar(Logging, Tracing, Metrics)에 전파하는 Enricher 인터페이스.
/// CtxEnricherPipeline이 최선두에서 실행되어 Request/Response 시점에 ctx.* 필드를
/// CtxEnricherContext.Push를 통해 모든 대상 Pillar에 동시 전파합니다.
/// </summary>
/// <typeparam name="TRequest">대상 Request 타입</typeparam>
/// <typeparam name="TResponse">대상 Response 타입 (IFinResponse 구현)</typeparam>
public interface IUsecaseCtxEnricher<in TRequest, in TResponse>
    where TResponse : IFinResponse
{
    /// <summary>
    /// Request 처리 시작 시 호출됩니다.
    /// CtxEnricherContext.Push로 ctx.* 필드를 대상 Pillar에 전파하고 IDisposable을 반환합니다.
    /// </summary>
    IDisposable? EnrichRequest(TRequest request);

    /// <summary>
    /// Response 처리 완료 시 호출됩니다.
    /// CtxEnricherContext.Push로 ctx.* 필드를 대상 Pillar에 전파하고 IDisposable을 반환합니다.
    /// </summary>
    IDisposable? EnrichResponse(TRequest request, TResponse response);
}
