namespace MediatorEvents.Demo.Experiments;

/// <summary>
/// 실험 4: CQRS와 이벤트의 관계
///
/// Q4. DDD 관점에서 이벤트는 CQRS의 Command에 해당하나?
/// A4. 아니요, 이벤트는 Command도 Query도 아닙니다. 별개의 개념입니다.
/// </summary>
public sealed class Exp4_CqrsRelationship
{
    public Task RunAsync()
    {
        Console.WriteLine("=== 실험 4: CQRS와 Event의 관계 ===");
        Console.WriteLine();
        Console.WriteLine("CQRS 구조:");
        Console.WriteLine("┌─────────────┐");
        Console.WriteLine("│   Command   │ → 상태 변경 요청 (Create, Update, Delete)");
        Console.WriteLine("├─────────────┤");
        Console.WriteLine("│   Query     │ → 상태 조회 요청 (Get, List)");
        Console.WriteLine("├─────────────┤");
        Console.WriteLine("│   Event     │ → 상태 변경 통지 (이미 발생한 사실)");
        Console.WriteLine("└─────────────┘");
        Console.WriteLine();

        Console.WriteLine("구분 비교:");
        Console.WriteLine("┌──────────┬────────────┬──────────┬────────────────────┐");
        Console.WriteLine("│ 구분     │ 의미       │ 시제     │ 예시               │");
        Console.WriteLine("├──────────┼────────────┼──────────┼────────────────────┤");
        Console.WriteLine("│ Command  │ 요청       │ 명령형   │ CreateProduct      │");
        Console.WriteLine("│ Query    │ 질의       │ 의문형   │ GetProductById     │");
        Console.WriteLine("│ Event    │ 통지       │ 과거형   │ ProductCreated     │");
        Console.WriteLine("└──────────┴────────────┴──────────┴────────────────────┘");
        Console.WriteLine();

        Console.WriteLine("흐름 예시:");
        Console.WriteLine();
        Console.WriteLine("  1. [Command] CreateOrder 요청 수신");
        Console.WriteLine("        ↓");
        Console.WriteLine("  2. [Handler] 비즈니스 로직 실행, 상태 변경");
        Console.WriteLine("        ↓");
        Console.WriteLine("  3. [Event] OrderCreated 발행 (이미 발생한 사실)");
        Console.WriteLine("        ↓");
        Console.WriteLine("  4. [EventHandler] 부수효과 처리 (이메일, 캐시, 외부 시스템)");
        Console.WriteLine();

        Console.WriteLine("Event의 역할:");
        Console.WriteLine("  • Command 처리 후 발생한 사실을 다른 모듈에 알림");
        Console.WriteLine("  • 느슨한 결합 (Publisher는 Subscriber를 몰라도 됨)");
        Console.WriteLine("  • 부수효과 처리 (이메일 발송, 캐시 갱신, 외부 시스템 동기화)");
        Console.WriteLine();

        Console.WriteLine("Mediator 인터페이스 비교:");
        Console.WriteLine("  • Command/Query → IRequest<TResponse> → Send() → 1:1");
        Console.WriteLine("  • Event         → INotification       → Publish() → 1:N");
        Console.WriteLine();

        return Task.CompletedTask;
    }
}
