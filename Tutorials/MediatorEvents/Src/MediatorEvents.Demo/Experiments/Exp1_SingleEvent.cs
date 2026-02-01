using MediatorEvents.Demo.Domain;
using Mediator;

namespace MediatorEvents.Demo.Experiments;

/// <summary>
/// 실험 1: 이벤트 대기 동작 확인
///
/// Q1. 이벤트 통지는 비동기로 처리되는데, 결과가 올때까지 대기하는 거니?
/// A1. 예, 대기합니다. Publish()는 모든 핸들러가 완료될 때까지 await 블로킹됩니다.
/// </summary>
public sealed class Exp1_SingleEvent(IPublisher publisher)
{
    public async Task RunAsync()
    {
        Console.WriteLine("=== 실험 1: 단일 이벤트 대기 동작 ===");
        Console.WriteLine();
        Console.WriteLine("목표: Publish()가 모든 핸들러 완료를 기다리는지 확인");
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms] 1. 이벤트 발행 시작");

        // OrderCreatedEvent를 발행하면 2개의 핸들러가 실행됨
        // - OrderCreatedHandler: 2초 소요
        // - OrderCreatedLogger: 1초 소요
        await publisher.Publish(new OrderCreatedEvent(
            OrderId: "ORD-001",
            CustomerName: "홍길동",
            TotalAmount: 50000m));

        // ← 이 시점에서 모든 핸들러가 완료됨!
        Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms] 2. 이벤트 발행 완료 (모든 핸들러 끝남)");

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"결론: 총 {stopwatch.ElapsedMilliseconds}ms 소요");
        Console.WriteLine("→ Fire-and-forget이 아닌, await 블로킹 방식임을 확인!");
        Console.WriteLine();
    }
}
