# 4.2 ORM 통합 패턴
> **Part 4: 실전 가이드** | [← 이전: 4.1 Functorium 프레임워크](../../01-Functorium-Framework/FunctoriumFramework/README.md) | [목차](../../../README.md) | [다음: 4.3 CQRS 통합 →](../../03-CQRS-Integration/CqrsIntegration/README.md)

---

## 개요

Entity Framework Core와 값 객체를 통합하는 패턴을 학습합니다. OwnsOne, OwnsMany, Value Converter 패턴을 다룹니다.

---

## 학습 목표

- EF Core의 `OwnsOne` 패턴으로 값 객체 매핑
- `OwnsMany` 패턴으로 컬렉션 값 객체 매핑
- `ValueConverter`를 사용한 단순 변환
- 값 객체의 데이터베이스 저장/로드

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

| 패턴 | 사용 시기 | 장점 | 단점 |
|------|----------|------|------|
| OwnsOne | 복합 값 객체 | 같은 테이블에 저장 | 컬럼 수 증가 |
| OwnsMany | 컬렉션 값 객체 | 정규화된 구조 | 별도 테이블 필요 |
| ValueConverter | 단일 값 객체 | 간단한 변환 | 복합 타입 불가 |

---

## 다음 단계

CQRS 통합 패턴을 학습합니다.

→ [4.3 CQRS 통합](../../03-CQRS-Integration/CqrsIntegration/README.md)
