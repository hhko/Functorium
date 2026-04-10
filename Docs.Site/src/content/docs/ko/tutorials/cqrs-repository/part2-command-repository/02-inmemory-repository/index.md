---
title: "인메모리 리포지토리"
---
## 개요

DB 없이 Repository를 테스트할 수 있을까요?
단위 테스트에서 매번 실제 DB를 띄우면 테스트가 느려지고, 환경 의존성 때문에 CI에서 실패하기 쉽습니다.
`InMemoryRepositoryBase<TAggregate, TId>`는 `ConcurrentDictionary` 기반의 `IRepository` 구현체로,
서브클래스가 `Store` 프로퍼티 하나만 제공하면 8개 CRUD 메서드가 자동으로 동작합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `InMemoryRepositoryBase`의 구조와 서브클래스 구현 패턴을 설명할 수 있습니다.
2. `ConcurrentDictionary` 기반 저장소에서 CRUD가 어떻게 동작하는지 설명할 수 있습니다.
3. `IDomainEventCollector`를 통한 도메인 이벤트 수집 메커니즘을 설명할 수 있습니다.
4. `FinT<IO, T>` 결과를 실행하고 검증하는 테스트 코드를 작성할 수 있습니다.

---

## 핵심 개념

### InMemoryRepositoryBase 구조

`InMemoryRepositoryBase`의 전체 구조를 먼저 살펴보세요. 서브클래스가 제공해야 할 것은 `Store` 프로퍼티 하나뿐입니다.

```
InMemoryRepositoryBase<TAggregate, TId>
├── abstract Store (ConcurrentDictionary)  ← 서브클래스가 제공
├── IDomainEventCollector                  ← 생성자로 주입
└── 8 CRUD methods                         ← 자동 구현
```

### 서브클래스 구현 패턴

서브클래스는 어떻게 구현할까요? `Store` 프로퍼티만 override하면 CRUD가 완성됩니다.

```csharp
public sealed class InMemoryProductRepository
    : InMemoryRepositoryBase<Product, ProductId>
{
    private static readonly ConcurrentDictionary<ProductId, Product> _store = new();

    public InMemoryProductRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    protected override ConcurrentDictionary<ProductId, Product> Store => _store;
}
```

`_store`를 `static`으로 선언한 점에 주목하세요. DI 컨테이너에서 Scoped로 등록해도 저장소는 프로세스 수명 동안 공유됩니다.

### FinT\<IO, T\> 실행 패턴

Repository 메서드는 `FinT<IO, T>`를 반환하지만, 이것만으로는 실행되지 않습니다. 다음 3단계를 거쳐야 결과를 얻을 수 있습니다.

```csharp
// 1. Repository 메서드 호출 → FinT<IO, T> 반환 (아직 실행되지 않음)
FinT<IO, Product> operation = repository.Create(product);

// 2. Run() → IO 실행, RunAsync() → Task로 변환
Fin<Product> result = await operation.Run().RunAsync();

// 3. 결과 확인
if (result.IsSucc)
    Console.WriteLine(result.ThrowIfFail().Name);
```

`Run()`이 IO 모나드를 실행하고, `RunAsync()`가 비동기 Task로 변환합니다. 이 두 단계를 거쳐야 실제 저장소 작업이 수행됩니다.

### IDomainEventCollector

Repository의 `Create`/`Update` 메서드는 `IDomainEventCollector.Track()`을 호출하여
Aggregate의 도메인 이벤트를 수집합니다. 테스트 시에는 이벤트 수집이 불필요하므로 `NoOpDomainEventCollector`를 사용합니다.

---

## 프로젝트 설명

### 프로젝트 구조

```
02-InMemory-Repository/
├── InMemoryRepository/
│   ├── InMemoryRepository.csproj
│   ├── Program.cs                      # CRUD 데모
│   ├── ProductId.cs                    # Ulid 기반 식별자
│   ├── Product.cs                      # Aggregate + 도메인 이벤트
│   └── InMemoryProductRepository.cs    # InMemoryRepositoryBase 구현
├── InMemoryRepository.Tests.Unit/
│   ├── InMemoryRepository.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── InMemoryProductRepositoryTests.cs
└── README.md
```

### 핵심 코드

**InMemoryProductRepository** -- Store만 제공하면 CRUD 완성:

```csharp
public sealed class InMemoryProductRepository
    : InMemoryRepositoryBase<Product, ProductId>
{
    private static readonly ConcurrentDictionary<ProductId, Product> _store = new();

    public InMemoryProductRepository(IDomainEventCollector eventCollector)
        : base(eventCollector) { }

    protected override ConcurrentDictionary<ProductId, Product> Store => _store;
}
```

테스트에서는 `FinT`를 실행한 뒤 결과를 바로 검증합니다.

**테스트에서 FinT 실행**:

```csharp
var result = await repository.Create(product).Run().RunAsync();
result.IsSucc.ShouldBeTrue();
```

---

## 한눈에 보는 정리

다음 테이블은 InMemory Repository의 핵심 구성 요소를 정리합니다.

| 항목 | 설명 |
|------|------|
| 베이스 클래스 | `InMemoryRepositoryBase<TAggregate, TId>` |
| 저장소 | `ConcurrentDictionary<TId, TAggregate>` |
| 필수 구현 | `Store` 프로퍼티 1개 |
| 이벤트 수집 | `IDomainEventCollector.Track()` |
| 실행 패턴 | `.Run().RunAsync()` |
| 용도 | 테스트, 프로토타이핑 |

---

## FAQ

### Q1: static ConcurrentDictionary를 사용하는 이유는 무엇인가요?
**A**: InMemory Repository는 프로세스 수명 동안 데이터를 유지해야 하므로 `static`으로 선언합니다. DI 컨테이너에서 Scoped로 등록해도 저장소는 공유됩니다.

### Q2: Create에서 이미 존재하는 ID로 생성하면 어떻게 되나요?
**A**: `InMemoryRepositoryBase`의 기본 구현은 `Store[id] = aggregate`로 덮어씁니다. 중복 체크가 필요하면 `Create` 메서드를 override할 수 있습니다.

### Q3: NoOpDomainEventCollector는 실제 운영에서도 사용하나요?
**A**: 아닙니다. 운영 환경에서는 `DomainEventCollector` 구현체가 DI로 주입됩니다. `NoOpDomainEventCollector`는 순수하게 테스트 목적입니다.

---

InMemory 구현으로 DB 없이도 빠르게 Repository를 테스트할 수 있게 되었습니다. 그런데 프로덕션에서는 EF Core를 사용해야 합니다. 도메인 모델을 그대로 DbSet에 매핑하면 어떤 문제가 생길까요? 다음 장에서는 Domain Model과 Persistence Model을 분리하는 EF Core Repository를 구현합니다.

→ [3장: EF Core Repository](../03-EfCore-Repository/)
