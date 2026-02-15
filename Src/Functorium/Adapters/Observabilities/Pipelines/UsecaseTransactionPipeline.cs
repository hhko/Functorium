using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Cqrs;
using Functorium.Applications.Events;
using Functorium.Applications.Persistence;

using Mediator;

using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Pipelines;

/// <summary>
/// Command Usecase에 대해 UoW.SaveChanges + 도메인 이벤트 발행을 자동으로 처리하는 Pipeline.
/// Query는 바이패스합니다.
/// </summary>
/// <remarks>
/// 실행 순서:
/// 1. Handler 실행 (next)
/// 2. 실패 시 → 커밋 안함, 응답 반환
/// 3. UoW.SaveChanges()
/// 4. IDomainEventPublisher.PublishTrackedEvents()로 추적된 Aggregate의 이벤트 발행
/// 5. 원래 성공 응답 반환
/// </remarks>
internal sealed class UsecaseTransactionPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly ILogger<UsecaseTransactionPipeline<TRequest, TResponse>> _logger;

    public UsecaseTransactionPipeline(
        IUnitOfWork unitOfWork,
        IDomainEventPublisher eventPublisher,
        ILogger<UsecaseTransactionPipeline<TRequest, TResponse>> logger)
    {
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(
        TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        // Query → 바이패스
        if (GetRequestCategoryType(request) != ObservabilityNaming.CategoryTypes.Command)
            return await next(request, cancellationToken);

        string handler = GetRequestHandler();

        // Handler 실행
        var response = await next(request, cancellationToken);
        if (response.IsFail)
            return response;

        // Commit
        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();
        var saveResult = await _unitOfWork.SaveChanges(cancellationToken).Run().RunAsync();
        double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

        if (saveResult.IsFail)
        {
            var error = saveResult.Match(Succ: _ => default!, Fail: e => e);
            _logger.LogWarning("SaveChanges 실패: {Handler}, 소요시간: {ElapsedSeconds:F4}s, 에러: {Error}",
                handler, elapsed, error);
            return TResponse.CreateFail(error);
        }

        _logger.LogDebug("SaveChanges 성공: {Handler}, 소요시간: {ElapsedSeconds:F4}s", handler, elapsed);

        // 도메인 이벤트 발행 (ObservableDomainEventPublisher가 관찰성 처리)
        await _eventPublisher.PublishTrackedEvents(cancellationToken).Run().RunAsync();

        return response;
    }
}
