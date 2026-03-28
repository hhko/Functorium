using MediatorEvents.Demo.Domain;
using Mediator;

namespace MediatorEvents.Demo.Experiments;

/// <summary>
/// 실험 5: 트랜잭션과 이벤트 발행 타이밍
///
/// Q5. 트랜잭션과 이벤트 통지는 어떻게 연동되나?
/// A5. 현재 Functorium 패턴에서는 트랜잭션 커밋 후 이벤트가 발행됩니다.
///     이는 최종 일관성(Eventual Consistency) 상태를 만들 수 있습니다.
/// </summary>
public sealed class Exp5_TransactionTiming(IPublisher publisher)
{
    public async Task RunAsync()
    {
        Console.WriteLine("=== 실험 5: 트랜잭션과 이벤트 발행 타이밍 ===");
        Console.WriteLine();
        Console.WriteLine("현재 Functorium 패턴:");
        Console.WriteLine();
        Console.WriteLine("  1. Command 처리 (비즈니스 로직)");
        Console.WriteLine("  2. Repository 저장 ← 트랜잭션 내부");
        Console.WriteLine("  3. 이벤트 발행 ← 트랜잭션 외부!");
        Console.WriteLine();

        // 시뮬레이션: 주문 생성 → 저장 → 이벤트 발행
        Console.WriteLine("─── 시뮬레이션 ───");
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 1. 도메인 로직 실행
        Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms] 1. 비즈니스 로직 실행");
        var order = Order.Create("박민수", 200000m);
        var events = order.GetDomainEvents();

        // 2. 트랜잭션 시뮬레이션
        Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms] 2. 트랜잭션 시작 (시뮬레이션)");

        try
        {
            // DB 저장 시뮬레이션
            Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms]    - Repository.SaveAsync() 호출");
            await Task.Delay(100); // DB 저장 시뮬레이션

            // 트랜잭션 커밋
            Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms]    - Transaction.CommitAsync() 완료");

            // 3. 이벤트 발행 (트랜잭션 외부!)
            Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms] 3. 이벤트 발행 (트랜잭션 외부)");

            foreach (var domainEvent in events)
            {
                await publisher.Publish(domainEvent);
            }

            Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms] 4. 이벤트 발행 완료");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{stopwatch.ElapsedMilliseconds,5}ms]    - Transaction.RollbackAsync()");
            Console.WriteLine($"    오류: {ex.Message}");
        }

        stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine("⚠️ 문제점:");
        Console.WriteLine("  - 저장 성공 → 이벤트 발행 실패 시: 데이터는 저장되었지만 알림 누락");
        Console.WriteLine("  - 최종 일관성(Eventual Consistency) 상태");
        Console.WriteLine();

        Console.WriteLine("해결책 - Outbox 패턴:");
        Console.WriteLine();
        Console.WriteLine("  1. Command 처리");
        Console.WriteLine("  2. 데이터 저장 + Outbox 테이블에 이벤트 저장 ← 같은 트랜잭션");
        Console.WriteLine("  3. 트랜잭션 커밋");
        Console.WriteLine("  4. 백그라운드 워커가 Outbox에서 이벤트 읽어서 발행");
        Console.WriteLine();

        Console.WriteLine("Outbox 패턴의 장점:");
        Console.WriteLine("  • 데이터와 이벤트가 원자적으로 저장됨");
        Console.WriteLine("  • 이벤트 발행 실패 시 재시도 가능");
        Console.WriteLine("  • 순서 보장 가능");
        Console.WriteLine();
    }
}
