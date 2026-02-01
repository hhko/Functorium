using MediatorEvents.Demo.Experiments;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
Console.WriteLine("║     Mediator 이벤트 처리 이해 튜토리얼                   ║");
Console.WriteLine("║     Understanding Mediator Event (Notification) Handling ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
Console.WriteLine();

// DI Container 구성
ServiceCollection services = new();

// Logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Mediator 등록 (소스 생성기가 핸들러 자동 등록)
services.AddMediator();

// 실험 클래스 등록
services.AddTransient<Exp1_SingleEvent>();
services.AddTransient<Exp2_MultipleHandlers>();
services.AddTransient<Exp3_LoggingStrategy>();
services.AddTransient<Exp4_CqrsRelationship>();
services.AddTransient<Exp5_TransactionTiming>();

// Service Provider 빌드
await using ServiceProvider serviceProvider = services.BuildServiceProvider();

// 실험 선택 메뉴
while (true)
{
    Console.WriteLine("실행할 실험을 선택하세요:");
    Console.WriteLine();
    Console.WriteLine("  1. 단일 이벤트 대기 동작 (Q1: await 블로킹 확인)");
    Console.WriteLine("  2. 다중 핸들러 순차 실행 (Q2: N개 핸들러 순차 실행)");
    Console.WriteLine("  3. 로깅 전략 비교 (Q3: 발행 측 vs 핸들러 측)");
    Console.WriteLine("  4. CQRS와 Event 관계 (Q4: Command/Query/Event 구분)");
    Console.WriteLine("  5. 트랜잭션 타이밍 (Q5: 트랜잭션과 이벤트 연동)");
    Console.WriteLine("  A. 모든 실험 실행");
    Console.WriteLine("  Q. 종료");
    Console.WriteLine();
    Console.Write("선택 (1-5, A, Q): ");

    var input = Console.ReadLine()?.Trim().ToUpperInvariant();
    Console.WriteLine();

    if (input == "Q")
    {
        Console.WriteLine("프로그램을 종료합니다.");
        break;
    }

    switch (input)
    {
        case "1":
            await serviceProvider.GetRequiredService<Exp1_SingleEvent>().RunAsync();
            break;

        case "2":
            await serviceProvider.GetRequiredService<Exp2_MultipleHandlers>().RunAsync();
            break;

        case "3":
            await serviceProvider.GetRequiredService<Exp3_LoggingStrategy>().RunAsync();
            break;

        case "4":
            await serviceProvider.GetRequiredService<Exp4_CqrsRelationship>().RunAsync();
            break;

        case "5":
            await serviceProvider.GetRequiredService<Exp5_TransactionTiming>().RunAsync();
            break;

        case "A":
            Console.WriteLine("모든 실험을 순차 실행합니다...");
            Console.WriteLine(new string('=', 60));
            Console.WriteLine();

            await serviceProvider.GetRequiredService<Exp1_SingleEvent>().RunAsync();
            Console.WriteLine(new string('-', 60));
            Console.WriteLine();

            await serviceProvider.GetRequiredService<Exp2_MultipleHandlers>().RunAsync();
            Console.WriteLine(new string('-', 60));
            Console.WriteLine();

            await serviceProvider.GetRequiredService<Exp3_LoggingStrategy>().RunAsync();
            Console.WriteLine(new string('-', 60));
            Console.WriteLine();

            await serviceProvider.GetRequiredService<Exp4_CqrsRelationship>().RunAsync();
            Console.WriteLine(new string('-', 60));
            Console.WriteLine();

            await serviceProvider.GetRequiredService<Exp5_TransactionTiming>().RunAsync();
            Console.WriteLine(new string('=', 60));
            Console.WriteLine();
            break;

        default:
            Console.WriteLine("잘못된 선택입니다. 1-5, A, Q 중 선택하세요.");
            Console.WriteLine();
            break;
    }

    Console.WriteLine("계속하려면 Enter를 누르세요...");
    Console.ReadLine();

    try { Console.Clear(); } catch (IOException) { /* 리다이렉트된 입력에서는 Clear 무시 */ }
    Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     Mediator 이벤트 처리 이해 튜토리얼                   ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
    Console.WriteLine();
}
