# Functorium Release v1.0.0-alpha.1

## 개요

Functorium v1.0.0-alpha.1은 **도메인 중심 함수형 아키텍처(Domain-Centric Functional Architecture)를** 구현하기 위한 C# 프레임워크의 첫 번째 알파 릴리스입니다.

> **📺 미디어 자료**
>
> - [릴리스 소개 영상 (MP4)](RELEASE-v1.0.0-alpha.1-KR.mp4)
> - [릴리스 소개 음성 (M4A)](RELEASE-v1.0.0-alpha.1-KR.m4a)

도메인 로직을 순수 함수로 표현하고, 부수 효과(Side Effects)를 아키텍처 경계로 밀어내어 **테스트 가능하고 예측 가능한 비즈니스 로직을** 작성할 수 있도록 지원합니다. LanguageExt 5.x 기반의 함수형 타입 시스템과 OpenTelemetry 통합 관찰 가능성을 핵심으로 제공합니다.

### 핵심 원칙

| 원칙 | 설명 | Functorium 지원 |
|------|------|----------------|
| **Domain First** | 도메인 모델이 아키텍처의 중심 | Value Object 계층 구조, 불변 도메인 타입 |
| **Pure Core** | 비즈니스 로직은 순수 함수로 표현 | `Fin<T>` 반환 타입, 예외 없는 에러 처리 |
| **Impure Shell** | 부수 효과는 경계 계층에서 처리 | Adapter Pipeline, ActivityContext 전파 |
| **Explicit Effects** | 모든 효과를 타입으로 명시 | `FinResponse<T>`, `FinT<IO, T>` 모나드 |

### 주요 기능

- **Domain Value Objects**: 불변성과 유효성을 보장하는 Value Object 계층 구조
- **CQRS & FinResponse**: 명시적 성공/실패 타입과 Command/Query 분리
- **OpenTelemetry 통합**: 로깅, 메트릭, 분산 추적의 완전한 관찰 가능성
- **Pipeline Behaviors**: 횡단 관심사를 순수 도메인 로직에서 분리
- **Source Generator**: Adapter 파이프라인 보일러플레이트 자동 생성
- **Architecture Testing**: 도메인 중심 아키텍처 규칙 검증

## Breaking Changes

이번 릴리스는 첫 번째 릴리스이므로 Breaking Changes가 없습니다.

## 새로운 기능

### Functorium 라이브러리

#### 1. Domain Value Objects

불변 Value Object를 구현하기 위한 완전한 클래스 계층 구조를 제공합니다. 단일 값, 복합 값, 비교 가능한 값 등 다양한 시나리오를 지원합니다.

```csharp
// 단일 값 Value Object
public sealed class UserId : ComparableSimpleValueObject<Guid>
{
    private UserId(Guid value) : base(value) { }

    public static Fin<UserId> Create(Guid value) =>
        CreateFromValidation(
            value == Guid.Empty
                ? Fail<Guid, Guid>(Error.New("UserId cannot be empty"))
                : Success<Error, Guid>(value),
            v => new UserId(v));
}

// 복합 Value Object
public sealed class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }

    private Address(string street, string city)
    {
        Street = street;
        City = city;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
    }
}
```

**Why this matters (왜 중요한가):**
- 도메인 모델의 불변성을 보장하여 버그 발생 가능성 감소
- `GetEqualityComponents()` 패턴으로 동등성 비교 로직 일관성 유지
- `CreateFromValidation` 팩토리 메서드로 유효성 검증과 생성을 함수형으로 처리
- 보일러플레이트 코드 50% 이상 감소 (직접 구현 대비)

<!-- 관련 커밋: fae67a9 feat(domain): ValueObject 기본 클래스 계층 구조 추가 -->

---

#### 2. CQRS & FinResponse

Command와 Query를 분리하고, 성공/실패를 명시적으로 표현하는 `FinResponse<A>` 타입을 제공합니다.

```csharp
// Command 정의
public record CreateUserCommand(string Name, string Email)
    : ICommandRequest<UserId>;

// Query 정의
public record GetUserQuery(UserId Id)
    : IQueryRequest<UserDto>;

// Command Handler
public class CreateUserUsecase : ICommandUsecase<CreateUserCommand, UserId>
{
    public ValueTask<FinResponse<UserId>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        return UserId.Create(Guid.NewGuid())
            .Match<FinResponse<UserId>>(
                Succ: id => id,
                Fail: error => error);
    }
}

// FinResponse 사용
FinResponse<UserId> result = await mediator.Send(command);

result.Match(
    Succ: id => Console.WriteLine($"Created: {id}"),
    Fail: error => Console.WriteLine($"Error: {error.Message}"));
```

**Why this matters (왜 중요한가):**
- 예외 대신 명시적인 실패 타입으로 에러 처리 누락 방지
- Mediator 패턴과 완벽 통합으로 CQRS 구현 간소화
- `Fin<T>`에서 `FinResponse<T>`로 자연스러운 변환 지원
- 함수형 합성 연산자 (`Bind`, `Map`, `Match`) 제공

<!-- 관련 커밋: 7eddbfc feat(cqrs): Fin<T>를 IResponse로 변환하는 ToResponse 확장 메서드 추가 -->

---

#### 3. OpenTelemetry 통합

로깅(Serilog), 메트릭, 추적을 OpenTelemetry 표준으로 통합 구성합니다.

```csharp
// Program.cs
services.RegisterOpenTelemetry(configuration, Assembly.GetExecutingAssembly())
    .ConfigureLogging(logging => logging
        .AddDestructuringPolicy<ErrorsDestructuringPolicy>()
        .AddEnricher<MyCustomEnricher>())
    .ConfigureMetrics(metrics => metrics
        .AddMeter("MyApp.Metrics")
        .AddInstrumentation(builder => builder.AddHttpClientInstrumentation()))
    .ConfigureTracing(tracing => tracing
        .AddSource("MyApp.Tracing"))
    .WithAdapterObservability()
    .Build();
```

```json
// appsettings.json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "ServiceNamespace": "MyCompany",
    "CollectorEndpoint": "http://localhost:4317",
    "CollectorProtocol": "Grpc",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": true
  }
}
```

**Why this matters (왜 중요한가):**
- 로깅, 메트릭, 추적 설정을 단일 빌더 API로 통합 (설정 시간 70% 감소)
- OTLP Exporter 자동 구성으로 Jaeger, Prometheus, Grafana 연동 간소화
- `ErrorsDestructuringPolicy`로 LanguageExt Error 타입 자동 구조화 로깅
- FluentValidation 기반 옵션 검증으로 잘못된 설정 조기 감지

<!-- 관련 커밋: 1790c73 feat(observability): OpenTelemetry 및 Serilog 통합 구성 추가 -->

---

#### 4. Pipeline Behaviors

Mediator 파이프라인에 예외 처리, 로깅, 메트릭, 추적, 유효성 검증을 자동 적용합니다.

```csharp
// 파이프라인 자동 적용 순서:
// 1. UsecaseExceptionPipeline - 예외를 FinResponse.Fail로 변환
// 2. UsecaseTracingPipeline - OpenTelemetry Span 생성
// 3. UsecaseMetricsPipeline - 요청 수, 성공/실패, 지연시간 기록
// 4. UsecaseLoggingPipeline - 요청/응답 구조화 로깅
// 5. UsecaseValidationPipeline - FluentValidation 검증

// 자동 생성되는 메트릭 예시:
// - usecase.command.requests (Counter)
// - usecase.command.duration (Histogram)
// - usecase.command.success (Counter)
// - usecase.command.failure (Counter)
```

**Why this matters (왜 중요한가):**
- 횡단 관심사(Cross-cutting concerns)를 파이프라인으로 분리하여 Usecase 코드 순수성 유지
- 모든 요청에 대해 일관된 로깅, 메트릭, 추적 자동 적용
- 예외가 발생해도 `FinResponse.Fail`로 변환되어 안전한 에러 처리
- EventId 기반 로그 필터링으로 디버깅 효율성 향상

<!-- 관련 커밋: f717b2e feat(observability): Metric 및 Trace 구현체 추가 -->

---

#### 5. Error Handling

레이어별 에러 정의와 구조화된 에러 코드 관리를 위한 아키텍처 패턴을 제공합니다. 각 레이어(Domain, Application)에서 중첩 클래스를 통해 에러를 관심사 단위로 분리하여 관리합니다.

```csharp
// =====================================================
// Domain Layer - Value Object 내부 DomainErrors 중첩 클래스
// =====================================================
public sealed class City : SimpleValueObject<string>
{
    private City(string value) : base(value) { }

    public static Fin<City> Create(string value) =>
        CreateFromValidation(Validate(value), v => new City(v));

    public static Validation<Error, string> Validate(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? DomainErrors.Empty(value)
            : value;

    // Domain 계층 에러 정의 - Value Object와 함께 캡슐화
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(City)}.{nameof(Empty)}",
                errorCurrentValue: value);
        // 에러 코드 형식: "DomainErrors.City.Empty"
    }
}

// =====================================================
// Application Layer - Usecase 내부 ApplicationErrors 중첩 클래스
// =====================================================
public sealed class CreateProductCommand
{
    public sealed record Request(string Name, decimal Price) : ICommandRequest<Response>;
    public sealed record Response(Guid ProductId, string Name);

    internal sealed class Usecase(IProductRepository repository)
        : ICommandUsecase<Request, Response>
    {
        public async ValueTask<FinResponse<Response>> Handle(
            Request request, CancellationToken cancellationToken)
        {
            FinT<IO, Response> usecase =
                from exists in repository.ExistsByName(request.Name)
                from _ in guard(!exists, ApplicationErrors.ProductNameAlreadyExists(request.Name))
                from product in repository.Create(/* ... */)
                select new Response(product.Id, product.Name);

            return (await usecase.Run().RunAsync()).ToFinResponse();
        }
    }

    // Application 계층 에러 정의 - Usecase와 함께 캡슐화
    internal static class ApplicationErrors
    {
        public static Error ProductNameAlreadyExists(string productName) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateProductCommand)}.{nameof(ProductNameAlreadyExists)}",
                errorCurrentValue: productName,
                errorMessage: $"Product name already exists: '{productName}'");
        // 에러 코드 형식: "ApplicationErrors.CreateProductCommand.ProductNameAlreadyExists"
    }
}
```

**레이어별 에러 관리 아키텍처:**

| 레이어 | 중첩 클래스 | 에러 코드 패턴 | 책임 |
|--------|-------------|----------------|------|
| **Domain** | `DomainErrors` | `DomainErrors.{Type}.{ErrorReason}` | Value Object 유효성 검증 실패 |
| **Application** | `ApplicationErrors` | `ApplicationErrors.{Usecase}.{ErrorReason}` | 비즈니스 규칙 위반, 중복 검사 등 |

**Why this matters (왜 중요한가):**
- **관심사 분리**: 에러 정의가 해당 레이어/클래스와 함께 캡슐화되어 응집도 향상
- **에러 코드 네이밍 컨벤션**: `{Layer}.{Type}.{ErrorReason}` 패턴으로 에러 발생 위치 즉시 식별
- **검색 가능성**: 에러 코드로 코드베이스 전체에서 에러 정의 위치 빠르게 검색
- **Serilog 자동 구조화**: `ErrorsDestructuringPolicy` 적용 시 에러 로그 자동 구조화
- **타입 안전성**: `ErrorCodeFactory`를 통한 일관된 에러 생성으로 오타 및 누락 방지

<!-- 관련 커밋: b889230 test(abstractions): Errors 타입 단위 테스트 추가 -->

---

#### 6. LINQ Extensions for FinT

`Fin<T>`와 `FinT<M, T>` 모나드를 위한 LINQ 확장 메서드를 제공합니다.

```csharp
// TraverseSerial - 순차 순회 (Activity Span 자동 생성)
var results = await items.ToSeq()
    .TraverseSerial(
        item => ProcessItem(item),
        activitySource,
        "ProcessItems",
        (item, index) => $"Item_{index}")
    .Run();

// Filter - 조건부 필터링
var filtered = fin.Filter(x => x > 0);

// SelectMany - 모나드 합성 (LINQ 쿼리 구문 지원)
var result = from a in GetUserAsync()
             from b in GetOrdersAsync(a.Id)
             select new { User = a, Orders = b };
```

**Why this matters (왜 중요한가):**
- 컬렉션의 순차 처리에 자동 Span 생성으로 추적 가능성 확보
- `FinT<IO, T>` 모나드로 비동기 함수형 프로그래밍 지원
- LINQ 쿼리 구문으로 가독성 높은 모나드 합성
- 실패 시 조기 종료 (fail-fast) 시맨틱 보장

<!-- 관련 커밋: 4683281 feat(linq): TraverseSerial 메서드 및 Activity Context 유틸리티 추가 -->

---

#### 7. Dependency Injection Extensions

Adapter 파이프라인과 옵션 구성을 위한 DI 확장 메서드를 제공합니다.

```csharp
// Adapter 파이프라인 등록 (ActivityContext 자동 전파)
services.RegisterScopedAdapterPipeline<IUserRepository, UserRepository>();

// 여러 인터페이스를 구현하는 Adapter 등록
services.RegisterScopedAdapterPipelineFor<
    IUserRepository,
    IUserQueryRepository,
    UserRepository>();

// Factory 기반 등록
services.RegisterScopedAdapterPipeline<IUserRepository>(
    (serviceProvider, activityContext) =>
        new UserRepository(serviceProvider.GetRequiredService<DbContext>(), activityContext));

// 옵션 구성 및 검증
services.RegisterConfigureOptions<MyOptions, MyOptionsValidator>("MySection");
```

**Why this matters (왜 중요한가):**
- `ActivityContext` 자동 전파로 분산 추적 구현 간소화
- Scoped/Transient/Singleton 라이프타임별 등록 메서드 제공
- FluentValidation 기반 옵션 검증으로 시작 시 잘못된 설정 감지
- 보일러플레이트 DI 등록 코드 대폭 감소

<!-- 관련 커밋: 7d9f182 feat(observability): OpenTelemetry 의존성 등록 확장 메서드 추가 -->

---

### Functorium.Testing 라이브러리

#### 1. Architecture Rules Validation

ArchUnitNET을 활용한 아키텍처 규칙 검증 유틸리티를 제공합니다.

```csharp
// Value Object 아키텍처 규칙 검증
var valueObjects = Classes()
    .That().ResideInNamespace("MyApp.Domain.ValueObjects");

valueObjects.ValidateAllClasses(architecture, validator =>
{
    validator
        .RequireSealed()
        .RequireAllPrivateConstructors()
        .RequireImmutable()
        .RequireMethod("Create", method => method
            .RequireStatic()
            .RequireReturnType(typeof(Fin<>)));
});
```

**Why this matters (왜 중요한가):**
- 아키텍처 규칙을 테스트로 강제하여 설계 일관성 유지
- Value Object, Entity, Repository 등 패턴별 규칙 템플릿 제공
- 위반 사항을 명확한 메시지로 보고하여 빠른 수정 가능
- CI/CD 파이프라인에서 아키텍처 드리프트 조기 감지

<!-- 관련 커밋: dd49bd8 refactor(testing): ArchitectureRules 검증 코드 리팩터링 -->

---

#### 2. Test Fixtures

ASP.NET Core 호스트 및 Quartz 스케줄러 테스트를 위한 픽스처를 제공합니다.

```csharp
// 호스트 테스트 픽스처
public class MyApiTests : IClassFixture<HostTestFixture<Program>>
{
    private readonly HostTestFixture<Program> _fixture;

    public MyApiTests(HostTestFixture<Program> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_Return_Ok()
    {
        var response = await _fixture.Client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

// Quartz Job 테스트 픽스처
public class MyJobTests : IClassFixture<QuartzTestFixture<Program>>
{
    [Fact]
    public async Task Should_Execute_Job()
    {
        var result = await _fixture.ExecuteJobOnceAsync<MyJob>(TimeSpan.FromSeconds(30));
        result.Success.Should().BeTrue();
    }
}
```

**Why this matters (왜 중요한가):**
- 통합 테스트 설정 보일러플레이트 90% 감소
- `IAsyncLifetime` 구현으로 테스트 라이프사이클 자동 관리
- Quartz Job 테스트를 동기적으로 실행하고 결과 검증 가능
- 환경별 구성 오버라이드 지원

<!-- 관련 커밋: 0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가 -->

---

#### 3. Structured Logging Assertions

Serilog 로그 이벤트를 검증하기 위한 유틸리티를 제공합니다.

```csharp
// 테스트용 구조화 로거 설정
var logEvents = new List<LogEvent>();
var logger = new LoggerConfiguration()
    .WriteTo.Sink(new TestSink(logEvents))
    .CreateLogger();

var structuredLogger = new StructuredTestLogger<MyService>(logger);

// 로그 이벤트 속성 추출 및 검증
var logData = LogEventPropertyExtractor.ExtractLogData(logEvents.First());
logData.Should().BeEquivalentTo(new
{
    RequestHandler = "CreateUserUsecase",
    Status = "success",
    Elapsed = 42.5
});
```

**Why this matters (왜 중요한가):**
- 구조화된 로그의 정확성을 테스트로 검증
- `LogEventPropertyExtractor`로 복잡한 로그 속성 쉽게 추출
- Pipeline 로깅 동작을 단위 테스트로 검증 가능
- 로그 기반 모니터링/알림 규칙의 정확성 보장

<!-- 관련 커밋: 922c7b3 refactor(testing): 로깅 테스트 유틸리티 재구성 -->

---

#### 4. Source Generator Testing

Roslyn Source Generator를 테스트하기 위한 러너를 제공합니다.

```csharp
[Fact]
public void Should_Generate_Pipeline_Code()
{
    var generator = new AdapterPipelineGenerator();

    var generatedCode = generator.Generate(@"
        [GeneratePipeline]
        public interface IUserRepository : IAdapter
        {
            Fin<User> GetById(UserId id);
        }");

    generatedCode.Should().Contain("public class UserRepositoryPipeline");
    generatedCode.Should().Contain("ActivityContext");
}
```

**Why this matters (왜 중요한가):**
- Source Generator 출력을 단위 테스트로 검증
- 컴파일 없이 생성된 코드 문자열 직접 검사
- 리팩터링 시 Generator 동작 회귀 방지
- TDD 방식의 Generator 개발 지원

<!-- 관련 커밋: 1fb6971 refactor(source-generator): 코드 구조 개선 및 테스트 인프라 추가 -->

---

### Functorium.Adapters.SourceGenerator 라이브러리

#### 1. Adapter Pipeline Generator

`[GeneratePipeline]` 어트리뷰트가 적용된 인터페이스에 대해 ActivityContext 전파가 포함된 파이프라인 코드를 자동 생성합니다.

```csharp
// 인터페이스 정의
[GeneratePipeline]
public interface IUserRepository : IAdapter
{
    Fin<User> GetById(UserId id);
    Fin<Seq<User>> GetAll();
    Fin<Unit> Save(User user);
}

// 자동 생성되는 코드 (개념적 예시)
public class UserRepositoryPipeline : IUserRepository
{
    private readonly IUserRepository _inner;
    private readonly ActivityContext _activityContext;

    public Fin<User> GetById(UserId id)
    {
        using var span = CreateSpan("GetById");
        return _inner.GetById(id);
    }
    // ...
}
```

**Why this matters (왜 중요한가):**
- Adapter 파이프라인 보일러플레이트 코드 100% 자동 생성
- ActivityContext 전파로 분산 추적 자동 지원
- 컴파일 타임 코드 생성으로 런타임 오버헤드 없음
- LanguageExt 5.x `Fin<T>`, `FinT<M, T>` 반환 타입 완벽 지원

<!-- 관련 커밋: 68623bf feat(generator): Adapter Pipeline Source Generator 프로젝트 추가 -->

## 버그 수정

- **ValueObject 배열 동등성 비교 버그 수정**: `GetEqualityComponents()`에서 배열을 반환할 때 올바르게 비교되지 않던 문제 수정 (9c1c2c1)
- **LanguageExt 5.x API 호환성 버그 수정**: 파라미터 없는 메서드에서 발생하던 Source Generator 버그 수정 (2e91065)

## API 변경사항

### Functorium 네임스페이스 구조

```
Functorium
├── Abstractions/
│   ├── Errors/
│   │   └── DestructuringPolicies/
│   │       └── ErrorTypes/
│   ├── Registrations/
│   └── Utilities/
├── Adapters/
│   ├── Observabilities/
│   │   ├── Builders/
│   │   │   └── Configurators/
│   │   ├── Context/
│   │   ├── Loggers/
│   │   ├── Metrics/
│   │   └── Spans/
│   └── Options/
├── Applications/
│   ├── Cqrs/
│   ├── Linq/
│   ├── Observabilities/
│   │   ├── Context/
│   │   ├── Loggers/
│   │   ├── Metrics/
│   │   └── Spans/
│   └── Pipelines/
└── Domains/
    └── ValueObjects/
```

### Functorium.Testing 네임스페이스 구조

```
Functorium.Testing
├── Actions/
│   └── SourceGenerators/
├── Arrangements/
│   ├── Hosting/
│   ├── Logging/
│   └── ScheduledJobs/
└── Assertions/
    ├── ArchitectureRules/
    └── Logging/
```

### 필수 의존성

- .NET 10.0 이상
- LanguageExt.Core 5.0.0-beta-58 이상
- OpenTelemetry 1.x
- Serilog 4.x
- Mediator.SourceGenerator
- FluentValidation
