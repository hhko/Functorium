using MediatorEvents.Demo.Domain;
using Mediator;
using Microsoft.Extensions.Logging;

namespace MediatorEvents.Demo.Experiments;

/// <summary>
/// 실험 3: 로깅 전략 비교
///
/// Q3. 이벤트 로깅은 발행 측 vs 핸들러 측 어디가 좋을까?
/// A3. 둘 다 목적이 다릅니다:
///     - 발행 측: 전체 흐름 추적 (이벤트 개수, 전체 소요 시간)
///     - 핸들러 측: 개별 처리 상태 (처리 성공/실패, 부수효과 결과)
/// </summary>
public sealed class Exp3_LoggingStrategy(IPublisher publisher, ILogger<Exp3_LoggingStrategy> logger)
{
    public async Task RunAsync()
    {
        Console.WriteLine("=== 실험 3: 로깅 전략 비교 ===");
        Console.WriteLine();
        Console.WriteLine("목표: 발행 측 로깅 vs 핸들러 측 로깅의 차이 이해");
        Console.WriteLine();

        var order = Order.Create("이영희", 100000m);
        var events = order.GetDomainEvents();

        Console.WriteLine("시나리오: 주문 생성 후 배송 처리");
        Console.WriteLine();

        // === 발행 측 로깅: 전체 흐름 추적 ===
        Console.WriteLine("─── 발행 측 로깅 (전체 흐름) ───");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        logger.LogInformation(
            "[Publisher] 이벤트 발행 시작 - 이벤트 개수: {EventCount}",
            events.Count);

        foreach (var domainEvent in events)
        {
            logger.LogInformation(
                "[Publisher] 이벤트 발행: {EventType}",
                domainEvent.GetType().Name);

            await publisher.Publish(domainEvent);
        }

        stopwatch.Stop();
        logger.LogInformation(
            "[Publisher] 이벤트 발행 완료 - 총 소요 시간: {ElapsedMs}ms",
            stopwatch.ElapsedMilliseconds);

        Console.WriteLine();
        Console.WriteLine("─── 핸들러 측 로깅 (개별 처리) ───");
        Console.WriteLine("→ 각 핸들러 내부에서 처리 시작/완료/실패 로깅");
        Console.WriteLine("→ 위 출력에서 [OrderCreatedHandler], [OrderCreatedLogger] 로그 참조");

        Console.WriteLine();
        Console.WriteLine("결론:");
        Console.WriteLine("  발행 측: '이벤트 N개 발행됨', '전체 Xms 소요' → 운영 모니터링");
        Console.WriteLine("  핸들러 측: '처리 성공/실패', '부수효과 결과' → 디버깅/추적");
        Console.WriteLine();
    }
}
