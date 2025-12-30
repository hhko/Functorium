# CqrsObservability 프로젝트 기술 지식 문서

**작성일**: 2025-12-30  
**프로젝트**: CqrsObservability RabbitMQ 메시징 구현  
**상태**: 완료

---

## 📚 목차

1. [Wolverine 메시징 프레임워크](#wolverine-메시징-프레임워크)
2. [FinT 및 함수형 프로그래밍](#fint-및-함수형-프로그래밍)
3. [소스 생성기 및 관찰 가능성](#소스-생성기-및-관찰-가능성)
4. [메시지 타입 및 네임스페이스](#메시지-타입-및-네임스페이스)
5. [IHost 및 서비스 초기화](#ihost-및-서비스-초기화)
6. [통합 테스트 전략](#통합-테스트-전략)
7. [LINQ 쿼리 표현식과 모나드 체이닝](#linq-쿼리-표현식과-모나드-체이닝)

---

## Wolverine 메시징 프레임워크

### 패키지 이름 및 버전

**중요**: Wolverine의 NuGet 패키지 이름은 `WolverineFx`입니다. `Wolverine`이 아닙니다.

```xml
<PackageReference Include="WolverineFx" />
<PackageReference Include="WolverineFx.RabbitMQ" />
```

**버전**: 최신 버전 사용 (예: 5.9.2)

### Host 초기화 필수성

**핵심 학습**: Wolverine은 `IHost`가 시작되어야 작동합니다. `Host.Build()`만으로는 충분하지 않습니다.

```csharp
var host = Host.CreateDefaultBuilder()
    .UseWolverine(opts => { /* 설정 */ })
    .Build();

// ❌ 이것만으로는 작동하지 않음
// await host.RunAsync(); // 무한 대기

// ✅ Host를 시작해야 Wolverine이 작동함
await host.StartAsync();

// 작업 수행...

// 데모 목적이므로 종료
await host.StopAsync();
```

**원인**: Wolverine은 호스트의 생명주기 이벤트를 사용하여 메시지 버스와 핸들러를 초기화합니다.

### RabbitMQ 연결 설정

```csharp
.UseWolverine(opts =>
{
    var rabbitMqConnectionString = configuration["RabbitMQ:ConnectionString"] 
        ?? "amqp://guest:guest@localhost:5672";
    
    opts.UseRabbitMq(new Uri(rabbitMqConnectionString))
        .AutoProvision(); // 큐/익스체인지 자동 생성
    
    // Request/Reply 패턴: 응답 큐 설정
    opts.PublishMessage<CheckInventoryRequest>()
        .ToRabbitQueue("inventory.check-inventory");
    
    // Fire and Forget 패턴: 명령 큐 설정
    opts.PublishMessage<ReserveInventoryCommand>()
        .ToRabbitQueue("inventory.reserve-inventory");
    
    // 수신 측: 큐 리스닝 설정
    opts.ListenToRabbitQueue("inventory.check-inventory");
    opts.ListenToRabbitQueue("inventory.reserve-inventory");
})
```

### OpenTelemetry 통합

Wolverine 메시징 추적을 위해 OpenTelemetry 소스를 추가해야 합니다:

```csharp
opts.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("Wolverine") // Wolverine 메시징 추적
        .AddConsoleExporter());
```

**활동(Activity) 확인**:
- `receive`: 메시지 수신 시 생성
- `wolverine.stopping.listener`: 리스너 종료 시 생성

---

## FinT 및 함수형 프로그래밍

### FinT<IO, T> 실행 패턴

**핵심 학습**: `FinT<IO, T>`는 `Run()`을 통해 `IO<Fin<T>>`로 변환되며, 이를 비동기로 실행하려면 `RunAsync()`를 사용해야 합니다.

```csharp
// FinT<IO, T>
//  -Run()→           IO<Fin<T>>
//  -RunAsync()→      Fin<T> (비동기 실행)

FinT<IO, CheckInventoryResponse> usecase = /* ... */;

// ✅ 올바른 패턴
Fin<CheckInventoryResponse> result = await usecase.Run().RunAsync(cancellationToken);

// ❌ 잘못된 패턴
// Fin<CheckInventoryResponse> result = await usecase.Run(); // 컴파일 오류
// Fin<CheckInventoryResponse> result = await Task.Run(() => usecase.Run().Run(), cancellationToken); // 불필요한 래핑
```

### IO.liftAsync 패턴

**핵심 학습**: `IO.liftAsync`는 `async` 람다를 `IO<Fin<T>>`로 변환하며, LanguageExt는 이를 `FinT<IO, T>`로 자동 변환합니다.

```csharp
// ✅ 올바른 패턴 (간결함)
public FinT<IO, CheckInventoryResponse> CheckInventory(CheckInventoryRequest request)
{
    return IO.liftAsync(async () =>
    {
        try
        {
            var response = await _messageBus.InvokeAsync<CheckInventoryResponse>(request);
            return Fin.Succ(response);
        }
        catch (Exception ex)
        {
            return Fin.Fail<CheckInventoryResponse>(Error.New(ex.Message));
        }
    });
}

// ❌ 불필요한 래핑 (이전 패턴)
// return FinT.lift(IO.liftAsync(async () => { ... }));
```

**이유**: LanguageExt는 `IO<Fin<T>>`를 `FinT<IO, T>`로 암시적 변환하므로 `FinT.lift`가 불필요합니다.

### RunSafe().Flatten() 패턴

**핵심 학습**: `RunSafe()`는 예외를 `Fin.Fail`로 변환하지만, `IO<Fin<Fin<T>>>`를 반환하므로 `Flatten()`으로 중첩을 제거해야 합니다.

```csharp
var ioFin = usecase.Run(); // IO<Fin<Response>>
Fin<Response> response = ioFin.RunSafe().Flatten(); // IO<Fin<Fin<Response>>> → IO<Fin<Response>>
return response.ToFinResponse();
```

**단계별 설명**:
1. `usecase.Run()`: `FinT<IO, Response>` → `IO<Fin<Response>>`
2. `RunSafe()`: 예외를 `Fin.Fail`로 변환, `IO<Fin<Fin<Response>>>` 반환
3. `Flatten()`: 중첩 `Fin` 제거, `IO<Fin<Response>>` 반환
4. `ToFinResponse()`: `Fin<Response>` → `FinResponse<Response>`

---

## 소스 생성기 및 관찰 가능성

### [GeneratePipeline] 애트리뷰트

**핵심 학습**: `[GeneratePipeline]` 애트리뷰트를 클래스에 추가하면 컴파일 타임에 파이프라인 버전이 자동 생성됩니다.

```csharp
[GeneratePipeline]
public class RabbitMqInventoryMessaging : IInventoryMessaging
{
    public string RequestCategory => "Messaging";
    
    // 구현...
}
```

**생성되는 클래스**: `RabbitMqInventoryMessagingPipeline`

**요구사항**:
- `IAdapter` 인터페이스를 구현해야 함
- `RequestCategory` 속성을 정의해야 함
- `ActivityContext`를 첫 번째 매개변수로 받는 생성자가 필요함 (파이프라인에서 사용)

### 파이프라인 등록

```csharp
// Repository 등록
services.RegisterScopedAdapterPipeline<IOrderRepository, InMemoryOrderRepositoryPipeline>();

// Messaging Adapter 등록
services.RegisterScopedAdapterPipeline<IInventoryMessaging, RabbitMqInventoryMessagingPipeline>();
```

**자동 처리되는 기능**:
- Activity 생성 및 관리
- 로깅 (요청/응답)
- 분산 추적
- 메트릭 수집

---

## 메시지 타입 및 네임스페이스

### 공유 메시지 타입 프로젝트의 필요성

**핵심 학습**: Wolverine은 메시지 타입의 **완전한 네임스페이스**를 사용하여 핸들러를 매칭합니다. 서로 다른 네임스페이스의 동일한 타입은 다른 타입으로 인식됩니다.

**문제 상황**:
- `OrderService.Adapters.Messaging.Messages.CheckInventoryRequest`
- `InventoryService.Adapters.Messaging.Messages.CheckInventoryRequest`

→ `wolverine.no.handler` 오류 발생

**해결 방법**: 공유 메시지 타입 프로젝트 생성

```csharp
// CqrsObservability.Messages 프로젝트
namespace CqrsObservability.Messages;

public sealed record CheckInventoryRequest(
    Guid ProductId,
    int Quantity);

public sealed record CheckInventoryResponse(
    Guid ProductId,
    bool IsAvailable,
    int AvailableQuantity);
```

**프로젝트 참조 추가**:
```xml
<ItemGroup>
  <ProjectReference Include="..\CqrsObservability.Messages\CqrsObservability.Messages.csproj" />
</ItemGroup>
```

**네임스페이스 통일**:
```csharp
using CqrsObservability.Messages; // 모든 서비스에서 동일한 네임스페이스 사용
```

---

## IHost 및 서비스 초기화

### ServiceCollection을 Host에 전달

**핵심 학습**: `ServiceCollection`에 등록한 서비스를 `IHost`의 `Services`에 추가해야 합니다.

```csharp
ServiceCollection services = new();
// ... 서비스 등록 ...

var host = Host.CreateDefaultBuilder()
    .ConfigureServices((context, hostServices) =>
    {
        // ServiceCollection의 서비스를 Host의 Services에 추가
        foreach (var service in services)
        {
            hostServices.Add(service);
        }
    })
    .UseWolverine(opts => { /* 설정 */ })
    .Build();
```

**이유**: `Host.CreateDefaultBuilder()`는 새로운 `ServiceCollection`을 생성하므로, 기존 `ServiceCollection`의 서비스를 명시적으로 추가해야 합니다.

### Program 클래스 가시성

**핵심 학습**: Top-level statements를 사용하는 경우, `WebApplicationFactory`나 `AddValidatorsFromAssemblyContaining`을 사용하려면 `Program` 클래스를 명시적으로 선언해야 합니다.

```csharp
// Program.cs (Top-level statements 사용)

// ... 코드 ...

// 파일 끝에 추가
namespace OrderService
{
    public partial class Program { }
}
```

**이유**: 리플렉션 기반 기능들이 `Program` 클래스를 찾기 위해 필요합니다.

---

## 통합 테스트 전략

### Testcontainers.RabbitMq 사용

**핵심 학습**: 통합 테스트에서 격리된 RabbitMQ 인스턴스를 사용하기 위해 Testcontainers를 사용합니다.

```csharp
public class MessagingTestFixture : IAsyncLifetime
{
    private RabbitMqContainer? _rabbitMqContainer;

    public async ValueTask InitializeAsync()
    {
        _rabbitMqContainer = new RabbitMqBuilder()
            .WithImage("rabbitmq:3.13-management-alpine")
            .Build();
        
        await _rabbitMqContainer.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_rabbitMqContainer != null)
        {
            await _rabbitMqContainer.DisposeAsync();
        }
    }
}
```

**연결 문자열**:
```csharp
var connectionString = _rabbitMqContainer.GetConnectionString();
```

### IHost 기반 서비스 초기화

**핵심 학습**: 콘솔 애플리케이션을 테스트하려면 `IHost`를 직접 생성하고 초기화해야 합니다.

```csharp
public class OrderServiceTestFixture : IAsyncLifetime
{
    private IHost? _host;

    public async ValueTask InitializeAsync()
    {
        // Configuration 설정
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "RabbitMQ:ConnectionString", _rabbitMqConnectionString }
            })
            .Build();

        // ServiceCollection 설정
        var services = new ServiceCollection();
        // ... 서비스 등록 ...

        // Host 생성
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, hostServices) =>
            {
                foreach (var service in services)
                {
                    hostServices.Add(service);
                }
            })
            .UseWolverine(opts => { /* 설정 */ })
            .Build();

        // Host 시작 (필수!)
        await _host.StartAsync();
    }

    public IServiceProvider Services => _host?.Services 
        ?? throw new InvalidOperationException("Fixture not initialized");
}
```

### AddMediator 모호성 해결

**핵심 학습**: 여러 어셈블리에 `AddMediator` 확장 메서드가 있을 때 리플렉션을 사용하여 명시적으로 호출해야 합니다.

```csharp
// Mediator 등록 (확장 메서드 - 모호성 해결을 위해 직접 호출)
var orderServiceAssembly = typeof(OrderService.Program).Assembly;
var mediatorExtensionsType = orderServiceAssembly.GetType(
    "Microsoft.Extensions.DependencyInjection.MediatorDependencyInjectionExtensions");
var addMediatorMethod = mediatorExtensionsType?.GetMethod(
    "AddMediator", 
    new[] { typeof(IServiceCollection) });
addMediatorMethod?.Invoke(null, new object[] { services });
```

---

## LINQ 쿼리 표현식과 모나드 체이닝

### FinT를 LINQ로 사용하기

**핵심 학습**: `Functorium.Applications.Linq` 네임스페이스를 사용하면 `FinT<IO, T>`를 LINQ 쿼리 표현식에서 사용할 수 있습니다.

```csharp
using Functorium.Applications.Linq;
using LanguageExt;
using static LanguageExt.Prelude;

// LINQ 쿼리 표현식으로 FinT 모나드 체이닝
FinT<IO, Response> usecase =
    from checkResponse in _inventoryMessaging.CheckInventory(new CheckInventoryRequest(
        ProductId: request.ProductId,
        Quantity: request.Quantity))
    from _ in guard(checkResponse.IsAvailable, ApplicationErrors.InsufficientInventory(
        request.ProductId,
        request.Quantity,
        checkResponse.AvailableQuantity))
    let orderId = Guid.NewGuid()
    from order in _orderRepository.Create(new Order(
        id: orderId,
        productId: request.ProductId,
        quantity: request.Quantity,
        createdAt: DateTime.UtcNow))
    from __ in _inventoryMessaging.ReserveInventory(new ReserveInventoryCommand(
        OrderId: orderId,
        ProductId: request.ProductId,
        Quantity: request.Quantity))
    select new Response(
        order.Id,
        order.ProductId,
        order.Quantity,
        order.CreatedAt);
```

**핵심 요소**:
- `from ... in ...`: 모나드 바인딩 (`SelectMany`)
- `let ... = ...`: 중간 계산 결과 저장
- `guard(...)`: 조건 검사 (실패 시 `Fin.Fail` 반환)
- `select ...`: 최종 결과 생성

**장점**:
- 가독성 향상
- 함수형 스타일 유지
- 에러 처리가 자동으로 체이닝됨

### guard 함수 사용

**핵심 학습**: `guard` 함수는 조건이 `false`일 때 `Fin.Fail`을 반환하여 체이닝을 중단시킵니다.

```csharp
from _ in guard(
    checkResponse.IsAvailable, 
    ApplicationErrors.InsufficientInventory(...))
```

**동작**:
- `checkResponse.IsAvailable == true`: `Fin.Succ(unit)` 반환, 체이닝 계속
- `checkResponse.IsAvailable == false`: `Fin.Fail(error)` 반환, 체이닝 중단

---

## 핸들러 구현 패턴

### 순수 비즈니스 로직만 처리

**핵심 학습**: 핸들러는 순수 비즈니스 로직만 처리하고, 로깅과 예외 처리는 파이프라인에서 자동으로 처리됩니다.

```csharp
// ✅ 올바른 패턴
public static async Task<CheckInventoryResponse> Handle(
    CheckInventoryRequest request,
    IInventoryRepository repository,
    CancellationToken cancellationToken = default)
{
    FinT<IO, CheckInventoryResponse> usecase =
        from item in repository.GetByProductId(request.ProductId)
        let availableQuantity = item.AvailableQuantity
        let isAvailable = availableQuantity >= request.Quantity
        select new CheckInventoryResponse(
            ProductId: request.ProductId,
            IsAvailable: isAvailable,
            AvailableQuantity: availableQuantity);

    Fin<CheckInventoryResponse> result = await usecase.Run().RunAsync();

    return result.Match(
        Succ: response => response,
        Fail: _ => new CheckInventoryResponse(
            ProductId: request.ProductId,
            IsAvailable: false,
            AvailableQuantity: 0));
}

// ❌ 잘못된 패턴 (로깅 코드 포함)
// logger.LogInformation("재고 확인 시작..."); // 파이프라인에서 처리됨
```

**이유**: `UsecaseLoggerPipeline`이 자동으로 로깅을 처리하므로 핸들러에서 직접 로깅할 필요가 없습니다.

### Request/Reply vs Fire and Forget 패턴

**Request/Reply 패턴**:
- `Fail` 케이스에서도 응답을 반환해야 함
- 예외를 던지지 않음

```csharp
return result.Match(
    Succ: response => response,
    Fail: _ => new CheckInventoryResponse(
        ProductId: request.ProductId,
        IsAvailable: false,
        AvailableQuantity: 0));
```

**Fire and Forget 패턴**:
- `Fail` 케이스에서 예외를 던져 파이프라인에서 처리하도록 함
- `UsecaseExceptionPipeline`이 자동으로 처리

```csharp
result.Match(
    Succ: _ => { },
    Fail: error => throw new Exception(error.Message));
```

---

## 에러 처리 및 예외

### UsecaseExceptionPipeline의 역할

**핵심 학습**: `UsecaseExceptionPipeline`이 핸들러에서 발생한 예외를 자동으로 처리하므로, 핸들러는 `Fin.Fail`을 예외로 변환할 수 있습니다.

```csharp
// 핸들러에서
result.Match(
    Succ: _ => { },
    Fail: error => throw new Exception(error.Message)); // 파이프라인에서 처리됨
```

**파이프라인 처리 순서**:
1. Request → Metric → Trace → Logger → Validation → **Exception** → Handler
2. Response ← Metric ← Trace ← Logger ← Validation ← **Exception** ← Handler

---

## Docker Compose 및 테스트 자동화

### RabbitMQ Docker Compose 설정

```yaml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    container_name: cqrsobservability-rabbitmq
    ports:
      - "5672:5672"   # AMQP 포트
      - "15672:15672" # Management UI 포트
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5
```

### PowerShell 스크립트를 통한 테스트

**핵심 학습**: PowerShell 7.x 스크립트를 사용하여 서비스를 순차적으로 실행하고 메시지 전송을 테스트할 수 있습니다.

```powershell
# Docker Compose 시작
docker-compose -f Tutorials/CqrsObservability/docker-compose.yml up -d

# InventoryService 시작 (백그라운드)
Start-Process pwsh -ArgumentList "-File", "Tutorials/CqrsObservability/Src/InventoryService/Program.cs" -NoNewWindow

# OrderService 시작
Start-Process pwsh -ArgumentList "-File", "Tutorials/CqrsObservability/Src/OrderService/Program.cs" -NoNewWindow
```

---

## 성능 및 최적화

### 소스 생성기의 장점

**핵심 학습**: 소스 생성기를 사용하면 런타임 오버헤드 없이 컴파일 타임에 파이프라인을 생성할 수 있습니다.

**장점**:
- 런타임 리플렉션 최소화
- 타입 안전성 보장
- 디버깅 가능 (생성된 코드 확인)

**단점**:
- 디버깅 시 생성된 코드 확인 필요
- 빌드 시간 약간 증가

---

## 참고 자료

### 공식 문서
- [Wolverine 공식 문서](https://wolverinefx.net/)
- [Wolverine RabbitMQ 가이드](https://wolverinefx.net/guide/messaging/transports/rabbitmq/)
- [Wolverine OpenTelemetry 가이드](https://wolverinefx.net/guide/logging.html#open-telemetry)

### 프로젝트 내 참조
- `Tutorials/CqrsFunctional/Src/CqrsFunctional.Demo/Program.cs`: 관찰 가능성 설정 참고
- `Tutorials/CqrsFunctional/Src/CqrsFunctional.Demo/Domain/IProductRepository.cs`: FinT 인터페이스 예제
- `Tutorials/CqrsFunctional/Src/CqrsFunctional.Demo/Infrastructure/InMemoryProductRepository.cs`: 소스 생성기 사용 예제

---

## 결론

이 프로젝트를 통해 다음 기술적 지식을 습득했습니다:

1. **Wolverine 메시징 프레임워크**: Host 초기화 필수성, RabbitMQ 통합, OpenTelemetry 추적
2. **FinT 및 함수형 프로그래밍**: `RunAsync()` 패턴, `IO.liftAsync` 사용, `RunSafe().Flatten()` 패턴
3. **소스 생성기**: `[GeneratePipeline]` 애트리뷰트를 통한 관찰 가능성 자동화
4. **메시지 타입 공유**: 공유 프로젝트를 통한 네임스페이스 통일의 중요성
5. **IHost 초기화**: ServiceCollection을 Host에 전달하는 방법
6. **통합 테스트**: Testcontainers를 사용한 격리된 테스트 환경 구성
7. **LINQ 쿼리 표현식**: FinT 모나드를 LINQ로 체이닝하는 방법

이러한 지식들은 향후 마이크로서비스 아키텍처와 함수형 프로그래밍 패턴을 적용하는 데 유용할 것입니다.

