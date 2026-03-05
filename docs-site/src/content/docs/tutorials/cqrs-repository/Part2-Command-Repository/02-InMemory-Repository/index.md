---
title: "인메모리 리포지토리"
---
## 개요

`InMemoryRepositoryBase<TAggregate, TId>`는 `ConcurrentDictionary` 기반의 `IRepository` 구현체입니다.
서브클래스는 `Store` 프로퍼티만 제공하면 8개 CRUD 메서드가 자동으로 동작합니다.
테스트 환경에서 실제 DB 없이 Repository 동작을 검증할 때 유용합니다.

---

## 학습 목표

- `InMemoryRepositoryBase`의 구조와 사용법을 이해합니다.
- `ConcurrentDictionary` 기반 저장소의 CRUD 동작을 학습합니다.
- `IDomainEventCollector`를 통한 도메인 이벤트 수집 메커니즘을 파악합니다.
- `FinT<IO, T>` 결과를 실행하고 검증하는 방법을 익힙니다.

---

## 핵심 개념

### InMemoryRepositoryBase 구조

```
InMemoryRepositoryBase<TAggregate, TId>
├── abstract Store (ConcurrentDictionary)  ← 서브클래스가 제공
├── IDomainEventCollector                  ← 생성자로 주입
└── 8 CRUD methods                         ← 자동 구현
```

### 서브클래스 구현 패턴

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

서브클래스는 `Store` 프로퍼티만 override하면 됩니다.

### FinT\<IO, T\> 실행 패턴

```csharp
// 1. Repository 메서드 호출 → FinT<IO, T> 반환 (아직 실행되지 않음)
FinT<IO, Product> operation = repository.Create(product);

// 2. Run() → IO 실행, RunAsync() → Task로 변환
Fin<Product> result = await operation.Run().RunAsync();

// 3. 결과 확인
if (result.IsSucc)
    Console.WriteLine(result.ThrowIfFail().Name);
```

### IDomainEventCollector

Repository의 `Create`/`Update` 메서드는 `IDomainEventCollector.Track()`을 호출하여
Aggregate의 도메인 이벤트를 수집합니다. 테스트 시에는 `NoOpDomainEventCollector`를 사용합니다.

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

**테스트에서 FinT 실행**:

```csharp
var result = await repository.Create(product).Run().RunAsync();
result.IsSucc.ShouldBeTrue();
```

---

## 한눈에 보는 정리

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

**Q: static ConcurrentDictionary를 사용하는 이유는 무엇인가요?**
A: InMemory Repository는 프로세스 수명 동안 데이터를 유지해야 하므로 `static`으로 선언합니다. DI 컨테이너에서 Scoped로 등록해도 저장소는 공유됩니다.

**Q: Create에서 이미 존재하는 ID로 생성하면 어떻게 되나요?**
A: `InMemoryRepositoryBase`의 기본 구현은 `Store[id] = aggregate`로 덮어씁니다. 중복 체크가 필요하면 `Create` 메서드를 override할 수 있습니다.

**Q: NoOpDomainEventCollector는 실제 운영에서도 사용하나요?**
A: 아닙니다. 운영 환경에서는 `DomainEventCollector` 구현체가 DI로 주입됩니다. `NoOpDomainEventCollector`는 순수하게 테스트 목적입니다.
