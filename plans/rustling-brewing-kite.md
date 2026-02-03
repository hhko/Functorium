# 도메인 이벤트 트랜잭션 후 발행 패턴 - 재고 차감 예제 추가

## 1. 현재 상태

### 기존 예제 (이미 구현됨)
- `CreateProductCommand` - 상품 생성 후 `CreatedEvent` 발행
- `UpdateProductCommand` - 상품 수정 후 `UpdatedEvent` 발행
- `OnProductCreated`, `OnProductUpdated` - 이벤트 핸들러

### 미구현 항목
| 항목 | 상태 |
|------|------|
| `StockDeductedEvent` 정의 | 완료 (Product.cs) |
| `DeductStock()` 메서드 | 완료 (Product.cs) |
| `OnStockDeducted` 핸들러 | **미구현** (MSG0005 경고) |
| `DeductStockCommand` 유스케이스 | **미구현** |

---

## 2. 구현 계획

### Step 1: OnStockDeducted 핸들러 추가

**파일**: `Tests.Hosts/01-SingleHost/LayeredArch.Application/Usecases/Products/OnStockDeducted.cs`

```csharp
using Functorium.Applications.Events;
using LayeredArch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Product.StockDeductedEvent 핸들러 - 재고 차감 로깅.
/// </summary>
public sealed class OnStockDeducted : IDomainEventHandler<Product.StockDeductedEvent>
{
    private readonly ILogger<OnStockDeducted> _logger;

    public OnStockDeducted(ILogger<OnStockDeducted> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.StockDeductedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Stock deducted: ProductId={ProductId}, Quantity={Quantity}",
            notification.ProductId,
            notification.Quantity);

        return ValueTask.CompletedTask;
    }
}
```

### Step 2: DeductStockCommand 유스케이스 추가

**파일**: `Tests.Hosts/01-SingleHost/LayeredArch.Application/Usecases/Products/DeductStockCommand.cs`

```csharp
using LayeredArch.Domain.Entities;
using LayeredArch.Domain.ValueObjects;
using LayeredArch.Domain.Repositories;
using Functorium.Applications.Events;
using Functorium.Applications.Linq;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// 재고 차감 Command - 트랜잭션 후 이벤트 발행 패턴 예제
/// </summary>
public sealed class DeductStockCommand
{
    public sealed record Request(
        string ProductId,
        int Quantity) : ICommandRequest<Response>;

    public sealed record Response(
        string ProductId,
        int RemainingStock);

    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("상품 ID는 필수입니다")
                .Must(id => ProductId.TryParse(id, null, out _))
                .WithMessage("유효하지 않은 상품 ID 형식입니다");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("차감 수량은 0보다 커야 합니다");
        }
    }

    public sealed class Usecase(
        IProductRepository productRepository,
        IDomainEventPublisher eventPublisher)
        : ICommandUsecase<Request, Response>
    {
        private readonly IProductRepository _productRepository = productRepository;
        private readonly IDomainEventPublisher _eventPublisher = eventPublisher;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            var productId = ProductId.Create(request.ProductId);
            var quantityResult = Quantity.Create(request.Quantity);

            if (quantityResult.IsFail)
            {
                return quantityResult.Match(
                    Succ: _ => throw new InvalidOperationException(),
                    Fail: error => FinResponse.Fail<Response>(error));
            }

            var quantity = (Quantity)quantityResult;

            // 트랜잭션 후 이벤트 발행 패턴:
            // 1. 조회 → 2. 도메인 로직 → 3. 영속화 → 4. 이벤트 발행
            FinT<IO, Response> usecase =
                from product in _productRepository.GetById(productId)                   // 1. 조회
                from _       in product.DeductStock(quantity).ToFinT()                  // 2. 도메인 로직 (이벤트 추가됨)
                from updated in _productRepository.Update(product)                      // 3. 영속화
                from __      in _eventPublisher.PublishEvents(updated, cancellationToken) // 4. 이벤트 발행
                select new Response(
                    updated.Id.ToString(),
                    updated.StockQuantity);

            Fin<Response> response = await usecase.Run().RunAsync();
            return response.ToFinResponse();
        }
    }
}
```

---

## 3. 수정 대상 파일

| 파일 | 작업 |
|------|------|
| `LayeredArch.Application/Usecases/Products/OnStockDeducted.cs` | 신규 생성 |
| `LayeredArch.Application/Usecases/Products/DeductStockCommand.cs` | 신규 생성 |

---

## 4. 검증

```bash
# 1. 빌드 (MSG0005 경고 해소 확인)
dotnet build Functorium.slnx

# 2. 테스트
dotnet test --solution Functorium.slnx

# 3. 앱 실행 후 API 테스트
# POST /products (상품 생성)
# POST /products/{id}/deduct-stock (재고 차감) - 엔드포인트 추가 필요시 별도 작업
```

---

## 5. 핵심 패턴 요약

```
┌─────────────────────────────────────────────────────────────┐
│  트랜잭션 후 이벤트 발행 패턴 (Publish After Transaction)    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  from product in repository.GetById(id)      // 1. 조회     │
│  from _       in product.DoSomething()       // 2. 도메인   │
│  from updated in repository.Update(product)  // 3. 영속화   │
│  from __      in eventPublisher.PublishEvents(...)  // 4. 발행│
│  select response                                            │
│                                                             │
│  ※ 영속화 실패 시 이벤트 발행되지 않음 (원자성 보장)          │
│  ※ 이벤트 발행 실패 시 전체 usecase 실패                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```
