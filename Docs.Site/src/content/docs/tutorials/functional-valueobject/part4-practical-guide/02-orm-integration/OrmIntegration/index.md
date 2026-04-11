---
title: "ORM 통합 패턴"
---
## Overview

Entity Framework Core와 value object를 통합하는 패턴을 학습합니다. OwnsOne, OwnsMany, Value Converter 패턴을 다룹니다.

---

## Learning Objectives

- EF Core의 `OwnsOne` 패턴으로 value object 매핑
- `OwnsMany` 패턴으로 컬렉션 value object 매핑
- `ValueConverter`를 사용한 단순 변환
- value object의 데이터베이스 저장/로드

---

## 실행 방법

```bash
cd Docs/tutorials/Functional-ValueObject/04-practical-guide/02-ORM-Integration/OrmIntegration
dotnet run
```

---

## 예상 출력

```
=== ORM 통합 패턴 ===

1. OwnsOne 패턴 - 복합 값 객체 매핑
────────────────────────────────────────
   저장된 사용자: 홍길동
   이메일: hong@example.com
   주소: 서울 강남구 테헤란로 123 (06234)

2. Value Converter 패턴 - 단일 값 객체 변환
────────────────────────────────────────
   상품 코드: EL-001234
   가격: 50,000 KRW

3. OwnsMany 패턴 - 컬렉션 값 객체 매핑
────────────────────────────────────────
   주문자: 김철수
   주문 항목:
      - 상품 A: 2개 x 10,000원
      - 상품 B: 1개 x 25,000원
```

---

## 핵심 코드 설명

### 1. OwnsOne 패턴

```csharp
modelBuilder.Entity<User>()
    .OwnsOne(u => u.Address, address =>
    {
        address.Property(a => a.City).HasColumnName("City");
        address.Property(a => a.Street).HasColumnName("Street");
        address.Property(a => a.PostalCode).HasColumnName("PostalCode");
    });
```

**테이블 구조:**
```
Users
├── Id (PK)
├── Name
├── Email       ← OwnsOne (단일 컬럼)
├── City        ← OwnsOne Address
├── Street      ← OwnsOne Address
└── PostalCode  ← OwnsOne Address
```

### 2. Value Converter 패턴

```csharp
modelBuilder.Entity<Product>()
    .Property(p => p.Code)
    .HasConversion(
        code => code.Value,              // 저장 시
        value => ProductCode.CreateFromValidated(value)  // 로드 시
    );
```

### 3. OwnsMany 패턴

```csharp
modelBuilder.Entity<Order>()
    .OwnsMany(o => o.LineItems, lineItem =>
    {
        lineItem.Property(l => l.ProductName);
        lineItem.Property(l => l.Quantity);
        lineItem.Property(l => l.UnitPrice);
    });
```

**테이블 구조:**
```
Orders                  OrderLineItems
├── Id (PK)            ├── OrderId (FK)
└── CustomerName       ├── ProductName
                       ├── Quantity
                       └── UnitPrice
```

---

## 패턴 선택 가이드

| 패턴 | When to Use | Pros | Cons |
|------|----------|------|------|
| OwnsOne | 복합 value object | 같은 테이블에 저장 | 컬럼 수 증가 |
| OwnsMany | 컬렉션 value object | 정규화된 구조 | 별도 테이블 필요 |
| ValueConverter | 단일 value object | 간단한 변환 | 복합 타입 불가 |

## FAQ

### Q1: `OwnsOne`과 `ValueConverter` 중 어떤 것을 사용해야 하나요?
**A**: 단일 값을 가진 value object(`Email`, `ProductCode` 등)에는 `ValueConverter`가 간단합니다. 여러 속성으로 구성된 복합 value object(`Address`, `Money` 등)에는 `OwnsOne`을 사용하여 각 속성을 별도 컬럼에 매핑합니다.

### Q2: `ValueConverter`에서 로드 시 `CreateFromValidated`를 사용하는 이유는 무엇인가요?
**A**: 데이터베이스에 저장된 값은 이미 검증을 통과한 값이므로, 다시 검증할 필요가 없습니다. `Create` 메서드를 사용하면 불필요한 검증 비용이 발생하고 `Fin<T>` 언래핑도 필요합니다. `CreateFromValidated`는 검증을 건너뛰고 바로 인스턴스를 생성합니다.

### Q3: `OwnsMany`를 사용하면 별도 테이블이 생기는데, 성능 문제는 없나요?
**A**: JOIN이 필요하므로 단일 테이블보다 조회 성능이 약간 떨어질 수 있습니다. 하지만 EF Core가 자동으로 관계를 관리해주고, 컬렉션 크기가 크지 않다면 실무에서 문제가 되는 경우는 드뭅니다. 성능이 중요한 조회에는 별도의 읽기 모델을 사용하는 것이 일반적입니다.

---

## Next Steps

CQRS 통합 패턴을 학습합니다.

→ [4.3 CQRS 통합](../../03-CQRS-Integration/CqrsIntegration/)
