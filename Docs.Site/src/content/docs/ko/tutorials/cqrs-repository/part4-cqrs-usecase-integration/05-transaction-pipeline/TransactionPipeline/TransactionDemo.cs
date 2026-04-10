using Functorium.Applications.Events;
using Functorium.Applications.Persistence;
using Functorium.Domains.Events;
using LanguageExt;

namespace TransactionPipeline;

public static class TransactionDemo
{
    public sealed record PipelineResult(bool HandlerSucceeded, bool SavedChanges, bool Committed, List<IDomainEvent> PublishedEvents);

    /// <summary>
    /// Command Pipeline 흐름을 시뮬레이션합니다.
    /// 실행 순서: Handler -> SaveChanges -> Commit -> Event Publishing
    /// </summary>
    public static async Task<PipelineResult> SimulateCommandPipeline(
        IUnitOfWork unitOfWork,
        IDomainEventCollector eventCollector,
        Func<Task<bool>> handler)
    {
        var publishedEvents = new List<IDomainEvent>();

        // 1. 트랜잭션 시작
        await using var transaction = await unitOfWork.BeginTransactionAsync();

        // 2. Handler 실행
        var handlerSucceeded = await handler();
        if (!handlerSucceeded)
            return new PipelineResult(false, false, false, publishedEvents);

        // 3. SaveChanges
        var saveResult = await unitOfWork.SaveChanges().Run().RunAsync();
        var saved = saveResult.Match(Succ: _ => true, Fail: _ => false);
        if (!saved)
            return new PipelineResult(true, false, false, publishedEvents);

        // 4. 트랜잭션 커밋
        await transaction.CommitAsync();

        // 5. 도메인 이벤트 수집 및 발행 (실제로는 IDomainEventPublisher가 처리)
        var trackedAggregates = eventCollector.GetTrackedAggregates();
        foreach (var aggregate in trackedAggregates)
        {
            publishedEvents.AddRange(aggregate.DomainEvents);
        }

        return new PipelineResult(true, true, true, publishedEvents);
    }
}
