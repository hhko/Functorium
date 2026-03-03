---
title: "Part 2 - Chapter 7: EF Core Repository"
---

> **Part 2: Command 측 -- Repository 패턴** | [← 이전: 6장 →](../02-InMemory-Repository/) | [다음: 8장 →](../04-Unit-Of-Work/)

---

## 개요

`EfCoreRepositoryBase<TAggregate, TId, TModel>`는 EF Core 기반의 `IRepository` 구현체입니다.
Domain Model과 Persistence Model을 분리하고, `ToDomain`/`ToModel` 매핑을 통해 두 모델 간 변환을 처리합니다.
`PropertyMap`을 사용하면 도메인 Specification의 Expression을 EF Core 쿼리용으로 자동 변환할 수 있습니다.

---

## 학습 목표

- Domain Model과 Persistence Model 분리의 필요성을 이해합니다.
- `ToDomain`/`ToModel` 매핑 패턴을 학습합니다.
- `PropertyMap`을 통한 Specification Expression 자동 변환을 파악합니다.
- `ReadQuery()`의 `AsNoTracking` + Include 자동 적용 메커니즘을 이해합니다.

---

## 핵심 개념

### Domain Model vs Persistence Model

```
Product (Domain Model)          ProductModel (Persistence Model)
├── ProductId Id                ├── string Id        ← Ulid → string
├── string Name                 ├── string Name
├── decimal Price               ├── decimal Price
├── bool IsActive               ├── bool IsActive
├── UpdatePrice()               └── (no behavior)
└── DomainEvents
```

- **Domain Model**: 비즈니스 로직과 도메인 이벤트를 포함합니다.
- **Persistence Model**: DB 스키마에 맞는 순수 데이터 클래스입니다.
- 분리를 통해 DB 스키마 변경이 도메인 로직에 영향을 주지 않습니다.

### ToDomain / ToModel 매핑

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

### EfCoreRepositoryBase 필수 구현 항목

| 멤버 | 역할 |
|------|------|
| `DbContext` | EF Core DbContext |
| `DbSet` | 엔티티의 DbSet |
| `ToDomain(TModel)` | Persistence → Domain 변환 |
| `ToModel(TAggregate)` | Domain → Persistence 변환 |

### PropertyMap -- Specification Expression 변환

`PropertyMap`은 도메인 Specification의 Expression Tree를 Persistence Model 기준으로 자동 변환합니다.

```
1. Specification → Expression<Func<Product, bool>>     (도메인 기준)
2. PropertyMap.Translate() → Expression<Func<ProductModel, bool>>  (모델 기준)
3. IQueryable.Where() → SQL WHERE 절 생성
```

이를 통해 도메인 계층에서 작성한 Specification을 EF Core 쿼리에서 그대로 사용할 수 있습니다.

### ReadQuery() -- N+1 방지

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

---

## 한눈에 보는 정리

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
