using Cqrs.Demo;
using Cqrs.Demo.Infrastructure;
using Cqrs.Demo.Usecases;
using LanguageExt;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== CQRS Pattern Demo ===");
Console.WriteLine();

// DI Container 구성
ServiceCollection services = new();

// Logging
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Mediator 등록
services.AddMediator();

// Repository 등록
services.AddSingleton<IUserRepository, InMemoryUserRepository>();

// Service Provider 빌드
await using ServiceProvider serviceProvider = services.BuildServiceProvider();

IMediator mediator = serviceProvider.GetRequiredService<IMediator>();
ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

Console.WriteLine("1. Creating users...");
Console.WriteLine(new string('-', 50));

// Command 실행: 사용자 생성
Fin<CreateUserCommand.Response> createResult1 = await mediator.Send(
    new CreateUserCommand.Request("Alice", "alice@example.com"));

PrintResult("Create User (Alice)", createResult1);

Fin<CreateUserCommand.Response> createResult2 = await mediator.Send(
    new CreateUserCommand.Request("Bob", "bob@example.com"));

PrintResult("Create User (Bob)", createResult2);

// 중복 이메일로 생성 시도
Fin<CreateUserCommand.Response> createResult3 = await mediator.Send(
    new CreateUserCommand.Request("Alice Clone", "alice@example.com"));

PrintResult("Create User (Duplicate Email)", createResult3);

Console.WriteLine();
Console.WriteLine("2. Querying users...");
Console.WriteLine(new string('-', 50));

// Query 실행: 모든 사용자 조회
Fin<GetAllUsersQuery.Response> allUsersResult = await mediator.Send(
    new GetAllUsersQuery.Request());

allUsersResult.Match(
    Succ: response =>
    {
        Console.WriteLine($"All Users ({response.Users.Count}):");
        foreach (GetAllUsersQuery.UserDto user in response.Users)
        {
            Console.WriteLine($"  - {user.Name} ({user.Email})");
        }
        return LanguageExt.Unit.Default;
    },
    Fail: error =>
    {
        Console.WriteLine($"Error: {error.Message}");
        return LanguageExt.Unit.Default;
    });

Console.WriteLine();
Console.WriteLine("3. Querying user by ID...");
Console.WriteLine(new string('-', 50));

// Query 실행: ID로 사용자 조회
if (createResult1.IsSucc)
{
    CreateUserCommand.Response value = createResult1.Match(Succ: v => v, Fail: _ => null!);
    Fin<GetUserByIdQuery.Response> userResult = await mediator.Send(
        new GetUserByIdQuery.Request(value.UserId));

    PrintResult("Get User by ID (Alice)", userResult);
}

// 존재하지 않는 사용자 조회
Fin<GetUserByIdQuery.Response> notFoundResult = await mediator.Send(
    new GetUserByIdQuery.Request(Guid.NewGuid()));

PrintResult("Get User by ID (Not Found)", notFoundResult);

Console.WriteLine();
Console.WriteLine("=== Demo Completed ===");

static void PrintResult<T>(string operation, Fin<T> result) where T : IResponse
{
    result.Match(
        Succ: value =>
        {
            Console.WriteLine($"[SUCCESS] {operation}");
            Console.WriteLine($"  Result: {value}");
            return LanguageExt.Unit.Default;
        },
        Fail: error =>
        {
            Console.WriteLine($"[FAILURE] {operation}");
            Console.WriteLine($"  Error: {error.Message}");
            return LanguageExt.Unit.Default;
        });
    Console.WriteLine();
}
