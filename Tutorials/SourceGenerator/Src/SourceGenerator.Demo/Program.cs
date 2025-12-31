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
var adapterTrace = new MockSpanFactory();
var adapterMetric = new MockMetricRecorder();

// Pipeline 인스턴스 생성
var pipeline = new UserRepositoryPipeline(
    parentContext: null,
    logger: logger,
    spanFactory: adapterTrace,
    metricRecorder: adapterMetric);

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

// ============================================================================
// 5. Throw Expression 동작 확인
// ============================================================================
//
// 질문: `() => throw error.ToException()`에서 throw가 값으로 처리되는가?
//
// 답변: 예, C# 7.0부터 throw는 "throw expression"으로 사용될 수 있습니다.
// throw expression의 타입은 "never type"으로, 모든 타입과 호환됩니다.
// 따라서 `IO.lift<A>(() => throw ...)` 에서 `Func<A>`의 반환값으로 사용 가능합니다.
//
// 실제로는:
// 1. 람다 `() => throw exception`은 `Func<A>` 타입으로 컴파일됨
// 2. 이 람다가 실행되면 예외가 throw됨 (값을 반환하지 않음)
// 3. throw expression은 "절대 정상 반환하지 않음"을 컴파일러에게 알림
// ============================================================================
Console.WriteLine("5. Throw Expression 동작 확인:");
Console.WriteLine("-------------------------------------------");

// 테스트 1: throw expression이 Func<T>로 사용 가능한지 확인
Func<int> throwingFunc = () => throw new InvalidOperationException("Test exception");
Console.WriteLine($"   throwingFunc 타입: {throwingFunc.GetType()}");

// 테스트 2: IO.lift에서 throw expression 동작 확인
var errorIO = IO.lift<string>(() => throw new InvalidOperationException("IO lift test"));
Console.WriteLine($"   errorIO 생성 완료 (아직 실행 안 됨)");

// Run() vs RunSafe() 비교
Console.WriteLine();
Console.WriteLine("   [Run() vs RunSafe() 비교]");

// Run()은 예외를 그대로 throw
Console.WriteLine("   - Run(): 예외를 그대로 throw");
try
{
    var runResult = errorIO.Run();
    Console.WriteLine($"     결과: {runResult}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"     예외 발생: {ex.Message}");
}

// RunSafe()는 Fin<A>를 반환 (예외를 값으로 캡처)
Console.WriteLine("   - RunSafe(): Fin<A> 반환 (예외를 값으로 캡처)");
var safeResult = errorIO.RunSafe();  // Fin<string> 반환, 예외 없음!
Console.WriteLine($"     반환 타입: {safeResult.GetType().Name}");
safeResult.Match(
    Succ: value => Console.WriteLine($"     성공: {value}"),
    Fail: err => Console.WriteLine($"     실패 (값으로 처리): {err.Message}")
);

// 테스트 3: IO에서 에러 표현 방법 탐색
Console.WriteLine();
Console.WriteLine("   IO 에러 표현 방법 탐색:");

var error = LanguageExt.Common.Error.New("Test error message");

// 방법 1: IO.fail (소문자) 테스트
Console.WriteLine("   - IO.fail<A>(error) 테스트:");
var ioFail1 = IO.fail<string>(error);
var result1 = ioFail1.RunSafe();
result1.Match(
    Succ: v => Console.WriteLine($"     성공: {v}"),
    Fail: e => Console.WriteLine($"     실패: {e.Message}")
);

// 방법 2: FinT.Run() 직접 사용 (가장 간단)
Console.WriteLine();
Console.WriteLine("   - FinT.Run() 직접 사용 (권장):");
var finTFail = FinT.Fail<IO, string>(error);
var ioFinResult = (IO<Fin<string>>)finTFail.Run();
var result2 = ioFinResult.Run();  // IO<Fin<string>> → Fin<string>
result2.Match(
    Succ: v => Console.WriteLine($"     성공: {v}"),
    Fail: e => Console.WriteLine($"     실패: {e.Message}")
);

// 방법 3: 개선된 FinTToIO with IO.fail
Console.WriteLine();
Console.WriteLine("   - 개선된 FinTToIO (IO.fail 사용):");
var ioFromFinTImproved = ((IO<IO<string>>)finTFail.Match(
    Succ: value => IO.pure(value),
    Fail: err => IO.fail<string>(err)  // IO.fail 사용
)).Flatten();

var result3 = ioFromFinTImproved.RunSafe();
Console.WriteLine($"     RunSafe() 완료!");
result3.Match(
    Succ: v => Console.WriteLine($"     성공: {v}"),
    Fail: e => Console.WriteLine($"     실패 (값으로 처리): {e.Message}")
);

// ============================================================================
// 6. 에러를 값으로 처리하는 대안 (예외 없이)
// ============================================================================
//
// 현재 방식: FinTToIO가 IO<A>를 반환 → Fail 시 예외 throw
// 대안 방식: FinTToIOWithFin이 IO<Fin<A>>를 반환 → Fail 시 Fin.Fail 값 반환
//
// 장점:
// - 예외 없이 순수 함수형 스타일로 에러 처리
// - try-catch 불필요, Match로 처리
// - 더 예측 가능한 제어 흐름
//
// 단점:
// - 반환 타입이 IO<Fin<A>>로 변경되어 기존 파이프라인 구조 변경 필요
// - ExecuteWithActivity 등 연관 메서드도 수정 필요
// ============================================================================
Console.WriteLine("6. 에러를 값으로 처리하는 대안:");
Console.WriteLine("-------------------------------------------");

// 대안 1: IO<Fin<A>>를 반환하는 방식 (예외 없음)
static IO<Fin<A>> FinTToIOWithFin<A>(FinT<IO, A> finT) =>
    (IO<Fin<A>>)finT.Run();  // FinT<IO, A>.Run() → K<IO, Fin<A>> → IO<Fin<A>>

Console.WriteLine("   대안 방식: FinT.Run() → IO<Fin<A>>");

var finTFailAlt = FinT.Fail<IO, string>(LanguageExt.Common.Error.New("Alternative error"));
var ioFinResultAlt = FinTToIOWithFin(finTFailAlt);

Console.WriteLine($"   ioFinResultAlt 생성 완료 (예외 없음)");

// 예외 없이 실행
var finResultAlt = ioFinResultAlt.Run();  // IO<Fin<string>> → Fin<string>
Console.WriteLine($"   Run() 완료 - 예외 없음!");

finResultAlt.Match(
    Succ: value => Console.WriteLine($"   성공: {value}"),
    Fail: err => Console.WriteLine($"   실패 (값으로 처리): {err.Message}")
);

Console.WriteLine();

// 성공 케이스도 확인
var finTSuccAlt = FinT.Succ<IO, string>("Success value");
var ioFinSuccResult = FinTToIOWithFin(finTSuccAlt).Run();

ioFinSuccResult.Match(
    Succ: value => Console.WriteLine($"   성공 케이스: {value}"),
    Fail: err => Console.WriteLine($"   실패: {err.Message}")
);

Console.WriteLine();
Console.WriteLine("-------------------------------------------");
Console.WriteLine("비교 정리:");
Console.WriteLine();
Console.WriteLine("  현재 방식 (FinTToIO → IO<A>):");
Console.WriteLine("    - Fail 시 예외 발생");
Console.WriteLine("    - try-catch로 처리");
Console.WriteLine("    - Pipeline에서 IfFail로 예외 재처리 가능");
Console.WriteLine();
Console.WriteLine("  대안 방식 (FinT.Run() → IO<Fin<A>>):");
Console.WriteLine("    - Fail 시 Fin.Fail 값 반환");
Console.WriteLine("    - Match로 처리");
Console.WriteLine("    - 예외 없는 순수 함수형 스타일");
Console.WriteLine();
Console.WriteLine("  권장: 파이프라인 내부에서 예외를 처리하고 외부로 Fin<A>를 노출하는");
Console.WriteLine("        현재 방식이 실용적. FinT 자체가 이미 Fin을 감싸고 있음.");
Console.WriteLine();
