---
title: "EF Core 리포지토리"
---
## 개요

도메인 모델을 그대로 `DbSet`에 매핑하면 어떻게 될까요?
DB 컬럼이 추가될 때마다 도메인 클래스를 수정해야 하고, ORM 어노테이션이 비즈니스 로직에 침투합니다.
`EfCoreRepositoryBase<TAggregate, TId, TModel>`은 이 문제를 해결합니다.
Domain Model과 Persistence Model을 분리하고, `ToDomain`/`ToModel` 매핑을 통해 두 모델 간 변환을 처리합니다.
`PropertyMap`을 사용하면 도메인 Specification의 Expression을 EF Core 쿼리용으로 자동 변환할 수 있습니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Domain Model과 Persistence Model을 왜 분리해야 하는지 설명할 수 있습니다.
2. `ToDomain`/`ToModel` 매핑을 직접 구현할 수 있습니다.
3. `PropertyMap`이 Specification Expression을 어떻게 변환하는지 설명할 수 있습니다.
4. `ReadQuery()`의 `AsNoTracking` + Include 자동 적용 메커니즘을 설명할 수 있습니다.

---

## 핵심 개념

### 왜 모델을 분리해야 할까?

도메인 모델을 EF Core `DbSet`에 직접 매핑한다고 생각해 보세요.

```csharp
// Domain Model에 ORM 어노테이션이 침투한 안티패턴
[Table("Products")]
public class Product : AggregateRoot<ProductId>
{
    [Column("product_id")]
    public ProductId Id { get; }      // Ulid인데 DB는 string을 원함

    [MaxLength(200)]
    public string Name { get; }       // 비즈니스 로직과 무관한 DB 제약

    public void UpdatePrice(decimal newPrice) { ... }
}
```

DB 스키마가 바뀔 때마다 도메인 클래스를 수정해야 하고, 비즈니스 로직과 영속화 관심사가 뒤섞입니다. 모델을 분리하면 각 계층이 독립적으로 진화할 수 있습니다.

### Domain Model vs Persistence Model

다음 구조를 보면 두 모델의 차이가 명확합니다. Domain Model은 행위와 이벤트를 가지고, Persistence Model은 순수 데이터만 담습니다.

```
Product (Domain Model)          ProductModel (Persistence Model)
├── ProductId Id                ├── string Id        ← Ulid → string
├── string Name                 ├── string Name
├── decimal Price               ├── decimal Price
├── bool IsActive               ├── bool IsActive
├── UpdatePrice()               └── (no behavior)
└── DomainEvents
```

- **Domain Model은** 비즈니스 로직과 도메인 이벤트를 포함합니다.
- **Persistence Model은** DB 스키마에 맞는 순수 데이터 클래스입니다.
- 분리를 통해 DB 스키마 변경이 도메인 로직에 영향을 주지 않습니다.

### ToDomain / ToModel 매핑

두 모델 사이를 어떻게 변환할까요? `ToDomain`과 `ToModel` 메서드가 이 역할을 합니다.

```csharp
// Persistence → Domain (DB 조회 시)
Product ToDomain(ProductModel model)
{
    return new Product(
        ProductId.Create(model.Id),  // string → Ulid-based ID
        model.Name,
        model.Price,
        model.IsActive);
}

// Domain → Persistence (DB 저장 시)
ProductModel ToModel(Product aggregate)
{
    return new ProductModel
    {
        Id = aggregate.Id.ToString(),  // Ulid-based ID → string
        Name = aggregate.Name,
        Price = aggregate.Price,
        IsActive = aggregate.IsActive,
    };
}
```

DB에서 읽을 때는 `ToDomain`으로 도메인 객체를 복원하고, 저장할 때는 `ToModel`로 DB 형식에 맞게 변환합니다.

### EfCoreRepositoryBase 필수 구현 항목

서브클래스가 구현해야 하는 멤버는 다음 4개입니다. InMemory Repository가 `Store` 하나였던 것에 비해 매핑 로직이 추가됩니다.

| 멤버 | 역할 |
|------|------|
| `DbContext` | EF Core DbContext |
| `DbSet` | 엔티티의 DbSet |
| `ToDomain(TModel)` | Persistence → Domain 변환 |
| `ToModel(TAggregate)` | Domain → Persistence 변환 |

### PropertyMap -- Specification Expression 변환

도메인 계층에서 작성한 Specification을 EF Core 쿼리에 그대로 사용하려면, Expression Tree를 Persistence Model 기준으로 변환해야 합니다. `PropertyMap`이 이 변환을 자동으로 처리합니다.

```
1. Specification → Expression<Func<Product, bool>>     (도메인 기준)
2. PropertyMap.Translate() → Expression<Func<ProductModel, bool>>  (모델 기준)
3. IQueryable.Where() → SQL WHERE 절 생성
```

이를 통해 도메인 계층의 Specification을 수정하지 않고도 EF Core 쿼리에서 사용할 수 있습니다.

### ReadQuery() -- N+1 방지

모든 읽기 쿼리에 `AsNoTracking`과 `Include`를 자동 적용하여 성능과 일관성을 보장합니다.

```csharp
protected IQueryable<TModel> ReadQuery()
    => applyIncludes(DbSet.AsNoTracking());
```

- `AsNoTracking`: 읽기 전용 쿼리로 Change Tracker 오버헤드를 제거합니다.
- `applyIncludes`: 생성자에서 선언한 Include가 모든 읽기 쿼리에 자동 적용됩니다.

---

## 프로젝트 설명

### 프로젝트 구조

```
03-EfCore-Repository/
├── EfCoreRepository/
│   ├── EfCoreRepository.csproj
│   ├── Program.cs              # 매핑 데모
│   ├── ProductId.cs            # Ulid 기반 식별자
│   ├── Product.cs              # Domain Model
│   ├── ProductModel.cs         # Persistence Model
│   └── ProductMapper.cs        # ToDomain/ToModel 매핑
├── EfCoreRepository.Tests.Unit/
│   ├── EfCoreRepository.Tests.Unit.csproj
│   ├── Using.cs
│   ├── xunit.runner.json
│   └── ProductMapperTests.cs
└── README.md
```

### 핵심 코드

실제 매핑 코드를 살펴보세요. `ToDomain`과 `ToModel`이 어떻게 ID 타입을 변환하는지 주목하세요.

**ProductMapper** -- Domain과 Persistence 모델 간 변환:

```csharp
public static Product ToDomain(ProductModel model)
{
    return new Product(
        ProductId.Create(model.Id),
        model.Name,
        model.Price,
        model.IsActive);
}

public static ProductModel ToModel(Product aggregate)
{
    return new ProductModel
    {
        Id = aggregate.Id.ToString(),
        Name = aggregate.Name,
        Price = aggregate.Price,
        IsActive = aggregate.IsActive,
    };
}
```

`ProductId.Create(model.Id)`로 string을 Ulid 기반 ID로 복원하고, `aggregate.Id.ToString()`으로 DB에 저장 가능한 string으로 변환합니다.

---

## 한눈에 보는 정리

다음 테이블은 EF Core Repository의 핵심 구성 요소를 요약합니다.

| 항목 | 설명 |
|------|------|
| 베이스 클래스 | `EfCoreRepositoryBase<TAggregate, TId, TModel>` |
| 모델 분리 | Domain Model + Persistence Model |
| 매핑 | `ToDomain()` / `ToModel()` |
| Specification 변환 | `PropertyMap.Translate()` |
| 읽기 최적화 | `ReadQuery()` = `AsNoTracking` + Include |
| ID 전략 | Ulid → string (DB 호환) |

---

## FAQ

**Q: 왜 Domain Model과 Persistence Model을 분리하나요?**
A: DB 스키마 변경(컬럼 추가, 타입 변경)이 도메인 로직에 영향을 주지 않도록 합니다. 또한 도메인 모델에 ORM 어노테이션이 침투하는 것을 방지합니다.

**Q: PropertyMap이 없으면 Specification을 사용할 수 없나요?**
A: `PropertyMap` 없이도 `IRepository`의 기본 CRUD는 동작합니다. 하지만 `BuildQuery(spec)`이나 `ExistsBySpec(spec)`처럼 Specification 기반 쿼리를 사용하려면 `PropertyMap`이 필수입니다.

**Q: IHasStringId는 무엇인가요?**
A: `EfCoreRepositoryBase`에서 `ByIdPredicate`/`ByIdsPredicate`의 기본 구현을 제공하기 위한 인터페이스입니다. Persistence Model이 `string Id` 프로퍼티를 가지도록 강제합니다.

---

EF Core Repository를 통해 도메인과 영속성을 분리했습니다. 그런데 주문 생성과 재고 차감이 하나의 트랜잭션으로 묶여야 한다면 어떻게 해야 할까요? 다음 장에서는 여러 Repository의 변경을 원자적으로 커밋하는 Unit of Work 패턴을 살펴봅니다.
