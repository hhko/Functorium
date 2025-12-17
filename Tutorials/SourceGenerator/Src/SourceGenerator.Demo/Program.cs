// ============================================================================
// Source Generator 튜토리얼
// ============================================================================
//
// 이 프로젝트는 Functorium.Adapters.SourceGenerator의 동작을 이해하기 위한
// 튜토리얼입니다.
//
// [GeneratePipeline] 어트리뷰트가 적용된 클래스에 대해 Source Generator가
// 자동으로 Pipeline 래퍼 클래스를 생성합니다.
//
// 생성된 코드 확인 방법:
//   1. 프로젝트 빌드: dotnet build
//   2. 생성된 파일 확인:
//      obj/Generated/Functorium.Adapters.SourceGenerator/
//      - GeneratePipelineAttribute.g.cs
//      - Adapters.UserRepositoryPipeline.g.cs
//
// ============================================================================

using System.Diagnostics;

using LanguageExt;
using Microsoft.Extensions.Logging;

using SourceGenerator.Demo.Adapters;
using SourceGenerator.Demo.Mocks;

// ============================================================================
// 1. 원본 Repository vs Pipeline
// ============================================================================
Console.WriteLine("1. Original UserRepository (Observability 없음):");
Console.WriteLine("-------------------------------------------");
var repository = new UserRepository();
Console.WriteLine($"   Type: {repository.GetType().Name}");
Console.WriteLine($"   RequestCategory: {repository.RequestCategory}");
Console.WriteLine();

// ============================================================================
// 2. Pipeline 인스턴스 생성 (Source Generator가 생성한 클래스)
// ============================================================================
Console.WriteLine("2. UserRepositoryPipeline (Observability 포함):");
Console.WriteLine("-------------------------------------------");

// Logger 설정 (콘솔 출력)
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug) // Debug 레벨 활성화
        .AddSimpleConsole(options =>
        {
            options.IncludeScopes = true;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
});

var logger = loggerFactory.CreateLogger<UserRepositoryPipeline>();
var adapterTrace = new MockAdapterTrace();
var adapterMetric = new MockAdapterMetric();

// Pipeline 인스턴스 생성
var pipeline = new UserRepositoryPipeline(
    parentContext: Activity.Current?.Context ?? default,
    logger: logger,
    adapterTrace: adapterTrace,
    adapterMetric: adapterMetric);

Console.WriteLine($"   Type: {pipeline.GetType().Name}");
Console.WriteLine($"   Base Type: {pipeline.GetType().BaseType?.Name}");
Console.WriteLine();

// ============================================================================
// 3. Pipeline 메서드 실행 - Observability 확인
// ============================================================================
Console.WriteLine("3. GetUserById(1) 실행 (Observability 출력 확인):");
Console.WriteLine("-------------------------------------------");

// FinT<IO, User>를 실행하고 결과 확인
var getUserResult = pipeline.GetUserById(1)
    .Run()  // FinT<IO, User> -> IO<Fin<User>>
    .Run(); // IO<Fin<User>> -> Fin<User>

Console.WriteLine();
getUserResult.Match(
    Succ: user => Console.WriteLine($"Result: User(Id={user.Id}, Name={user.Name}, Email={user.Email})"),
    Fail: error => Console.WriteLine($"Error: {error}")
);
Console.WriteLine();

// ============================================================================
// 4. 파라미터 없는 메서드 실행
// ============================================================================
Console.WriteLine("4. GetAllUsers() 실행 (파라미터 없는 메서드):");
Console.WriteLine("-------------------------------------------");

var getAllUsersResult = pipeline.GetAllUsers()
    .Run()  // FinT<IO, IReadOnlyList<User>> -> IO<Fin<IReadOnlyList<User>>>
    .Run(); // IO<Fin<IReadOnlyList<User>>> -> Fin<IReadOnlyList<User>>

Console.WriteLine();
getAllUsersResult.Match(
    Succ: users =>
    {
        Console.WriteLine($"Result: {users.Count} users");
        foreach (var user in users)
        {
            Console.WriteLine($"  - User(Id={user.Id}, Name={user.Name})");
        }
    },
    Fail: error => Console.WriteLine($"   Error: {error}")
);
Console.WriteLine();
