---
title: "엔티티 인터페이스"
---
## 개요

Entity마다 "언제 생성됐는지", "언제 수정됐는지", "누가 삭제했는지"를 매번 직접 구현하면 어떻게 될까요? 같은 코드가 모든 Entity에 반복되고, 누락이나 불일치가 생기기 쉽습니다.

Functorium은 이런 공통 관심사를 **인터페이스로 분리**하여 제공합니다. `IAuditable`은 생성/수정 시각을, `ISoftDeletable`은 논리적 삭제를, `ISoftDeletableWithUser`는 삭제자 추적까지 지원합니다. 이 장에서는 이 인터페이스들을 구현하는 Product Entity를 만들어 각 인터페이스의 역할을 실습합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `IAuditable`로 `CreatedAt`, `UpdatedAt`을 통한 생성/수정 시각 추적을 **구현할 수 있습니다**
2. `ISoftDeletable` / `ISoftDeletableWithUser`로 물리적 삭제 대신 논리적 삭제 패턴을 **적용할 수 있습니다**
3. 여러 인터페이스를 조합하여 Entity의 관심사를 **선언적으로 표현할 수 있습니다**

### 실습을 통해 확인할 내용
- **Product**: `IAuditable`과 `ISoftDeletableWithUser`를 동시에 구현하는 Entity
- **UpdatePrice()**: 가격 변경 시 `UpdatedAt` 자동 갱신
- **Delete() / Restore()**: 소프트 삭제와 복원 동작

---

## 핵심 개념

### 왜 필요한가?

생성 시각, 수정 시각, 소프트 삭제는 대부분의 Entity가 필요로 하는 공통 관심사입니다. 인터페이스로 분리하면 "이 Entity는 감사 추적을 지원한다"는 사실을 타입 시스템으로 선언할 수 있고, 인프라 계층에서 자동으로 처리할 수 있습니다.

### IAuditable 인터페이스

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; }
    Option<DateTime> UpdatedAt { get; }
}
```

- `CreatedAt`: Entity 생성 시각 (한 번만 설정)
- `UpdatedAt`: 최종 수정 시각 (`Option<DateTime>`으로 미수정 상태 표현)

`UpdatedAt`이 `DateTime?`이 아닌 `Option<DateTime>`인 이유가 궁금하신가요? null 참조 오류를 방지하고, 패턴 매칭으로 안전하게 처리할 수 있기 때문입니다.

### ISoftDeletable / ISoftDeletableWithUser 인터페이스

물리적 DELETE 대신 "삭제됨" 표시를 남기는 패턴입니다. 데이터를 보존하면서도 삭제된 것처럼 동작하게 만들 수 있습니다.

```csharp
public interface ISoftDeletable
{
    Option<DateTime> DeletedAt { get; }
    bool IsDeleted => DeletedAt.IsSome;  // 기본 구현
}

public interface ISoftDeletableWithUser : ISoftDeletable
{
    Option<string> DeletedBy { get; }
}
```

- `DeletedAt`: 삭제 시각 (None이면 삭제되지 않은 상태)
- `IsDeleted`: `DeletedAt`에서 파생되는 편의 속성
- `DeletedBy`: 삭제를 수행한 사용자 식별자

### 소프트 삭제 패턴

실제로 사용하면 이렇게 동작합니다:

```csharp
// 삭제
product.Delete("admin@example.com");
// DeletedAt = Some(2025-01-01T12:00:00), DeletedBy = Some("admin@example.com")

// 복원
product.Restore();
// DeletedAt = None, DeletedBy = None, UpdatedAt = Some(...)
```

삭제해도 데이터가 남아 있으므로 언제든 복원할 수 있고, 누가 삭제했는지 추적할 수 있습니다.

---

## 프로젝트 설명

### 프로젝트 구조
```
EntityInterfaces/
├── Program.cs                  # IAuditable, ISoftDeletableWithUser 데모
├── ProductId.cs                # Ulid 기반 식별자
├── Product.cs                  # IAuditable + ISoftDeletableWithUser 구현
└── EntityInterfaces.csproj

EntityInterfaces.Tests.Unit/
├── ProductTests.cs             # 시각 추적, 소프트 삭제 테스트
├── Using.cs
├── xunit.runner.json
└── EntityInterfaces.Tests.Unit.csproj
```

### 핵심 코드

#### Product.cs

`IAuditable`과 `ISoftDeletableWithUser`를 동시에 구현하여, 하나의 Entity에서 시각 추적과 소프트 삭제를 모두 지원합니다. 각 메서드가 관련 속성을 어떻게 갱신하는지 살펴보세요.

```csharp
public sealed class Product : Entity<ProductId>, IAuditable, ISoftDeletableWithUser
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }
    public Option<DateTime> DeletedAt { get; private set; }
    public Option<string> DeletedBy { get; private set; }
    public bool IsDeleted => DeletedAt.IsSome;

    public static Product Create(string name, decimal price) =>
        new(ProductId.New(), name, price);

    public void UpdatePrice(decimal newPrice)
    {
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(string deletedBy)
    {
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    public void Restore()
    {
        DeletedAt = None;
        DeletedBy = None;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

`UpdatePrice()`는 가격 변경과 함께 `UpdatedAt`을 갱신하고, `Restore()`도 상태 변경이므로 `UpdatedAt`을 갱신합니다. 이렇게 하면 "마지막 수정 시점"이 항상 정확하게 유지됩니다.

---

## 한눈에 보는 정리

### Entity 인터페이스 계층

Functorium이 제공하는 Entity 인터페이스의 전체 계층을 정리하면 다음과 같습니다.

| 인터페이스 | 속성 | 용도 |
|-----------|------|------|
| `IAuditable` | `CreatedAt`, `UpdatedAt` | 생성/수정 시각 추적 |
| `IAuditableWithUser` | + `CreatedBy`, `UpdatedBy` | 생성자/수정자 추적 |
| `ISoftDeletable` | `DeletedAt`, `IsDeleted` | 소프트 삭제 |
| `ISoftDeletableWithUser` | + `DeletedBy` | 삭제자 추적 |

### Option<DateTime> 사용 이유

왜 `DateTime?` 대신 `Option<DateTime>`을 사용하는지 비교해 보세요.

| 표현 | 의미 |
|------|------|
| `None` | 아직 발생하지 않음 (미수정, 미삭제) |
| `Some(DateTime)` | 해당 시점에 발생함 |
| `DateTime?` 대비 장점 | `null` 참조 오류 방지, 패턴 매칭 지원 |

---

## FAQ

### Q1: 왜 물리적 삭제 대신 소프트 삭제를 사용하나요?
**A**: 소프트 삭제는 데이터를 보존하므로 감사(audit) 추적, 실수로 인한 삭제 복구, 관련 데이터의 참조 무결성 유지에 유리합니다. Repository 계층에서 `IsDeleted` 필터를 자동 적용하면 애플리케이션 코드에서는 삭제된 데이터를 의식하지 않아도 됩니다.

### Q2: IsDeleted가 인터페이스의 기본 구현인데, Product에서 다시 선언하는 이유는?
**A**: C#의 기본 인터페이스 멤버(DIM)는 **인터페이스 타입으로 캐스팅해야** 접근 가능합니다. `product.IsDeleted`처럼 직접 접근하려면 클래스에서 명시적으로 선언해야 합니다. 이는 사용 편의성을 위한 선택입니다.

### Q3: Restore() 시 UpdatedAt을 갱신하는 이유는?
**A**: 복원도 Entity의 상태 변경이므로 `UpdatedAt`을 갱신합니다. 이를 통해 "이 Entity가 마지막으로 변경된 시점"을 정확히 추적할 수 있습니다.

### Q4: IAuditableWithUser는 언제 사용하나요?
**A**: 멀티테넌트 환경이나 감사 로그가 중요한 시스템에서 "누가 생성/수정했는가"를 추적해야 할 때 사용합니다. 이 장에서는 간결함을 위해 `IAuditable`만 사용합니다.

---

도메인 모델의 기초를 완성했습니다. Entity, Aggregate Root, 도메인 이벤트, 그리고 공통 인터페이스까지 — 이제 이 모델을 저장하고 꺼내올 차례입니다. 모든 Repository가 같은 CRUD 메서드를 반복 정의해야 할까요? Part 2에서는 **Repository 추상화**를 통해 이 문제를 해결합니다.
