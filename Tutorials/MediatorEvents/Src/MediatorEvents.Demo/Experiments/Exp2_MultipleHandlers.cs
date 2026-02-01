using MediatorEvents.Demo.Domain;
using Mediator;

namespace MediatorEvents.Demo.Experiments;

/// <summary>
/// 실험 2: 다중 핸들러 실행 순서
///
/// Q2. 이벤트가 N개일 때 N개 모두 완료될 때까지 대기하는 거니?
/// A2. 예, 하나의 이벤트에 여러 핸들러가 있으면 순차 실행됩니다 (기본 동작).
/// </summary>
public sealed class Exp2_MultipleHandlers(IPublisher publisher)
{
    public async Task RunAsync()
    {
        Console.WriteLine("=== 실험 2: 다중 핸들러 순차 실행 ===");
        Console.WriteLine();
        Console.WriteLine("목표: 동일 이벤트의 여러 핸들러가 순차 실행되는지 확인");
        Console.WriteLine();
        Console.WriteLine("OrderCreatedEvent에는 2개의 핸들러가 등록됨:");
        Console.WriteLine("  - OrderCreatedHandler: 2초 소요");
        Console.WriteLine("  - OrderCreatedLogger: 1초 소요");
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms] 이벤트 발행 시작");
        Console.WriteLine();

        await publisher.Publish(new OrderCreatedEvent(
            OrderId: "ORD-002",
            CustomerName: "김철수",
            TotalAmount: 75000m));

        Console.WriteLine();
        Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms] 이벤트 발행 완료");

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"결론: 총 {stopwatch.ElapsedMilliseconds}ms 소요");
        Console.WriteLine("→ 병렬(~2초)이 아닌 순차(~3초) 실행됨을 확인!");
        Console.WriteLine("→ 핸들러 A 완료 후 핸들러 B 시작 (순차적)");
        Console.WriteLine();
    }
}
