# Mediator Singleton + Repository Scoped Factory 패턴 적용 계획

## 목표
Mediator와 Pipeline을 Singleton으로 등록하고, IProductRepository는 Factory 패턴을 통해 Scoped로 유지

---

## Mediator 라이브러리 분석 결과

### martinothamar/Mediator의 Handler 해결 방식

**생성 코드 분석 (Mediator.g.cs):**

```csharp
// CommandHandlerWrapper (Singleton으로 등록됨)
internal sealed class CommandHandlerWrapper<TRequest, TResponse>
{
    public ValueTask<TResponse> Handle(Mediator mediator, TRequest request, CancellationToken ct)
    {
        // ⚠️ 핵심: mediator.Services를 사용하여 Handler 해결
        var concreteHandler = mediator.Services.GetRequiredService<ICommandHandler<TRequest, TResponse>>();
        var pipelineBehaviours = mediator.Services.GetServices<IPipelineBehavior<TRequest, TResponse>>();
        // ...
    }
}
```

**문제점:**
- `mediator.Services`는 Mediator 생성 시점의 IServiceProvider
- Singleton Mediator → Root IServiceProvider 캡처
- Root Provider에서 Scoped 서비스 해결 시 **DI 에러 발생**:
  ```
  "Cannot consume scoped service from singleton"
  ```

### 결론: 방안 1 (Mediator Singleton) 불가능

martinothamar/Mediator는 **Singleton Mediator + Scoped Handler 조합을 지원하지 않음**

---

## 권장 방안: 방안 2 (Repository Factory 패턴)

### 구조 변경

```
현재:
  Mediator (Scoped) → Pipeline (Scoped) → Handler (Scoped) → Repository (Scoped)

변경 후:
  Mediator (Singleton) → Pipeline (Singleton) → Handler (Singleton) → RepositoryFactory (Singleton)
                                                                           ↓
                                                                    Repository (Scoped - 런타임 해결)
```

### 핵심 아이디어
- Handler가 Repository를 직접 주입받지 않고 **Factory**를 주입받음
- Factory는 `IServiceProvider`를 통해 **현재 HTTP 요청의 Scope**에서 Repository 해결
- WebApi에서는 HttpContext마다 Scope가 생성되므로 Factory가 올바른 Scoped 인스턴스 반환

---

## 구현 단계

### Step 1: Factory 인터페이스 생성
**파일:** `Tutorials/Cqrs04Endpoint/Src/Cqrs04Endpoint.WebApi/Domain/IProductRepositoryFactory.cs`

```csharp
namespace Cqrs04Endpoint.WebApi.Domain;

public interface IProductRepositoryFactory
{
    IProductRepository Create();
}
```

### Step 2: Factory 구현체 생성
**파일:** `Tutorials/Cqrs04Endpoint/Src/Cqrs04Endpoint.WebApi/Infrastructure/ProductRepositoryFactory.cs`

```csharp
namespace Cqrs04Endpoint.WebApi.Infrastructure;

public class ProductRepositoryFactory : IProductRepositoryFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ProductRepositoryFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IProductRepository Create()
    {
        // WebApi에서는 현재 HttpContext의 Scope에서 해결됨
        return _serviceProvider.GetRequiredService<IProductRepository>();
    }
}
```

### Step 3: Handler 수정 (4개 파일)

**수정 대상:**
- `Usecases/CreateProductCommand.cs`
- `Usecases/GetProductByIdQuery.cs`
- `Usecases/GetAllProductsQuery.cs`
- `Usecases/UpdateProductCommand.cs`

**변경 패턴 (Handle 시작 시 한 번만 Create() 호출):**
```csharp
// 변경 전
internal sealed class Usecase(
    ILogger<Usecase> logger,
    IProductRepository productRepository)
    : IQueryUsecase<Request, Response>
{
    private readonly IProductRepository _productRepository = productRepository;

    public ValueTask<Response> Handle(Request request, CancellationToken ct)
    {
        FinT<IO, Response> usecase =
            from product in _productRepository.GetById(request.ProductId)
            select new Response(...);

        return usecase.RunAsync().AsValueTask();
    }
}

// 변경 후
internal sealed class Usecase(
    ILogger<Usecase> logger,
    IProductRepositoryFactory repositoryFactory)
    : IQueryUsecase<Request, Response>
{
    private readonly IProductRepositoryFactory _repositoryFactory = repositoryFactory;

    public ValueTask<Response> Handle(Request request, CancellationToken ct)
    {
        // Handle 시작 시 한 번만 Factory 호출
        var repository = _repositoryFactory.Create();

        FinT<IO, Response> usecase =
            from product in repository.GetById(request.ProductId)
            select new Response(...);

        return usecase.RunAsync().AsValueTask();
    }
}
```

**핵심 변경:**
1. 생성자 파라미터: `IProductRepository` → `IProductRepositoryFactory`
2. 필드: `_productRepository` → `_repositoryFactory`
3. Handle 메서드 시작 시: `var repository = _repositoryFactory.Create();` 추가
4. LINQ 표현식 내: `_productRepository` → `repository` (지역 변수)

### Step 4: Program.cs 수정
**파일:** `Tutorials/Cqrs04Endpoint/Src/Cqrs04Endpoint.WebApi/Program.cs`

```csharp
// 변경 전
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsecaseMetricsPipeline<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsecaseTracingPipeline<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsecaseLoggingPipeline<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsecaseValidationPipeline<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UsecaseExceptionPipeline<,>));

// 변경 후
builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Singleton);
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseMetricsPipeline<,>));
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseTracingPipeline<,>));
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseLoggingPipeline<,>));
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseValidationPipeline<,>));
builder.Services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(UsecaseExceptionPipeline<,>));

// Factory 등록 (Singleton - IServiceProvider는 Scope-aware)
builder.Services.AddSingleton<IProductRepositoryFactory, ProductRepositoryFactory>();

// Repository는 여전히 Scoped
builder.Services.RegisterScopedAdapterPipeline<IProductRepository, InMemoryProductRepositoryPipeline>();
```

### Step 5: Handler 라이프타임 변경
Handler도 Singleton으로 변경 (더 이상 Scoped 의존성 없음)

```csharp
// Mediator가 생성한 Handler 등록은 자동으로 Singleton이 됨
// (AddMediator의 ServiceLifetime 설정에 따름)
```

---

## 수정 대상 파일 요약

| 파일 | 변경 유형 |
|------|----------|
| `Domain/IProductRepositoryFactory.cs` | 신규 생성 |
| `Infrastructure/ProductRepositoryFactory.cs` | 신규 생성 |
| `Usecases/CreateProductCommand.cs` | 수정 |
| `Usecases/GetProductByIdQuery.cs` | 수정 |
| `Usecases/GetAllProductsQuery.cs` | 수정 |
| `Usecases/UpdateProductCommand.cs` | 수정 |
| `Program.cs` | 수정 |

---

## 장단점

### 장점
- Mediator, Pipeline, Handler 인스턴스 재사용 (요청당 생성 비용 제거)
- 메모리 효율성 향상
- GC 부담 감소

### 단점
- Factory 패턴 추가로 코드 복잡도 증가
- Handler 코드 수정 필요 (4개 파일)
- 암시적 의존성 (DI 그래프에서 Repository 의존성이 명확하지 않음)

---

## 검증 계획

1. **빌드 테스트**: 컴파일 에러 없음 확인
2. **단위 테스트**: 기존 테스트 통과 확인
3. **통합 테스트**:
   - 동시 요청 시 Repository 인스턴스 격리 확인
   - 요청마다 새로운 Scoped Repository 생성 확인
4. **메모리 프로파일링**: 인스턴스 생성 횟수 비교
