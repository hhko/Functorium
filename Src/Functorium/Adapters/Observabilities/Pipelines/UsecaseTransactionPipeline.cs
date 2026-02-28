using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Usecases;
using Functorium.Applications.Events;
using Functorium.Applications.Persistence;

using Mediator;

using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Pipelines;

/// <summary>
/// Command Usecase에 대해 명시적 트랜잭션 + UoW.SaveChanges + 도메인 이벤트 발행을 자동으로 처리하는 Pipeline.
/// Query는 바이패스합니다.
/// </summary>
/// <remarks>
/// 실행 순서:
/// 1. 명시적 트랜잭션 시작 (ExecuteDeleteAsync/ExecuteUpdateAsync도 이 트랜잭션에 참여)
/// 2. Handler 실행 (next)
/// 3. 실패 시 → 트랜잭션 미커밋 → 롤백, 응답 반환
/// 4. UoW.SaveChanges()
/// 5. 트랜잭션 커밋
/// 6. IDomainEventPublisher.PublishTrackedEvents()로 추적된 Aggregate의 이벤트 발행
/// 7. 원래 성공 응답 반환
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

        // 명시적 트랜잭션 시작 → ExecuteDeleteAsync/ExecuteUpdateAsync도 이 트랜잭션에 참여
        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

        // Handler 실행
        var response = await next(request, cancellationToken);
        if (response.IsFail)
            return response;   // 트랜잭션 미커밋 → Dispose 시 롤백

        // SaveChanges
        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();
        var saveResult = await _unitOfWork.SaveChanges(cancellationToken).Run().RunAsync();
        double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);

        if (saveResult.IsFail)
        {
            var error = saveResult.Match(Succ: _ => default!, Fail: e => e);
            _logger.LogWarning("SaveChanges 실패: {Handler}, 소요시간: {ElapsedSeconds:F4}s, 에러: {Error}",
                handler, elapsed, error);
            return TResponse.CreateFail(error);   // 트랜잭션 미커밋 → Dispose 시 롤백
        }

        _logger.LogDebug("SaveChanges 성공: {Handler}, 소요시간: {ElapsedSeconds:F4}s", handler, elapsed);

        // 모두 성공 → 트랜잭션 커밋
        await transaction.CommitAsync(cancellationToken);

        // 도메인 이벤트 발행 (ObservableDomainEventPublisher가 관찰성 처리)
        await _eventPublisher.PublishTrackedEvents(cancellationToken).Run().RunAsync();

        return response;
    }
}
