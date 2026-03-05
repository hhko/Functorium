---
title: "엔티티 인터페이스"
---
## 개요

Functorium은 Entity에 공통적으로 필요한 관심사(생성/수정 시각 추적, 소프트 삭제)를 **인터페이스로 분리**하여 제공합니다. `IAuditable`은 생성/수정 시각을, `ISoftDeletable`은 논리적 삭제를, `ISoftDeletableWithUser`는 삭제자 추적까지 지원합니다. 이 장에서는 이 인터페이스들을 구현하는 Product Entity를 만들어 각 인터페이스의 역할을 실습합니다.

---

## 학습 목표

### 핵심 학습 목표
1. **IAuditable** - `CreatedAt`, `UpdatedAt`을 통한 생성/수정 시각 추적
2. **ISoftDeletable / ISoftDeletableWithUser** - 물리적 삭제 대신 논리적(소프트) 삭제 패턴
3. **인터페이스 조합** - 여러 인터페이스를 조합하여 Entity의 관심사를 선언적으로 표현

### 실습을 통해 확인할 내용
- **Product**: `IAuditable`과 `ISoftDeletableWithUser`를 동시에 구현하는 Entity
- **UpdatePrice()**: 가격 변경 시 `UpdatedAt` 자동 갱신
- **Delete() / Restore()**: 소프트 삭제와 복원 동작

---

## 핵심 개념

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

### ISoftDeletable / ISoftDeletableWithUser 인터페이스

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

물리적 DELETE 대신 `DeletedAt`을 설정하여 데이터를 보존합니다:

```csharp
// 삭제
product.Delete("admin@example.com");
// DeletedAt = Some(2025-01-01T12:00:00), DeletedBy = Some("admin@example.com")

// 복원
product.Restore();
// DeletedAt = None, DeletedBy = None, UpdatedAt = Some(...)
```

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

---

## 한눈에 보는 정리

### Entity 인터페이스 계층
| 인터페이스 | 속성 | 용도 |
|-----------|------|------|
| `IAuditable` | `CreatedAt`, `UpdatedAt` | 생성/수정 시각 추적 |
| `IAuditableWithUser` | + `CreatedBy`, `UpdatedBy` | 생성자/수정자 추적 |
| `ISoftDeletable` | `DeletedAt`, `IsDeleted` | 소프트 삭제 |
| `ISoftDeletableWithUser` | + `DeletedBy` | 삭제자 추적 |

### Option<DateTime> 사용 이유
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
