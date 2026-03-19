---
title: "엔티티와 식별자"
---
## 개요

같은 이름, 같은 가격의 상품 두 개가 있습니다. 이 둘은 같은 상품인가요? 속성이 모두 동일해도 각각 별도로 관리해야 한다면, **무엇으로 구별해야 할까요?**

DDD에서 Entity는 **고유한 식별자(Identity)로 구별되는 도메인 객체**입니다. 같은 속성 값을 가지더라도 ID가 다르면 다른 Entity이고, 속성이 달라도 ID가 같으면 같은 Entity입니다. 이 장에서는 Functorium의 `Entity<TId>`와 Ulid 기반 `IEntityId<TId>`를 사용하여 이 문제를 해결합니다.

---

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `Entity<TId>`가 ID 기반 동등성 비교를 자동으로 제공하는 원리를 **설명할 수 있습니다**
2. `IEntityId<TId>` 인터페이스로 Ulid 기반 식별자를 **생성, 복원, 비교할 수 있습니다**
3. Entity와 Value Object의 동등성 차이를 **구분할 수 있습니다**

### 실습을 통해 확인할 내용
- **ProductId**: Ulid 기반 Entity 식별자 구현 (`[GenerateEntityId]`가 생성하는 코드의 수동 구현)
- **Product**: `Entity<ProductId>`를 상속하여 ID 기반 동등성을 자동으로 제공받는 도메인 Entity

---

## 핵심 개념

### 왜 필요한가?

속성만으로 Entity를 비교하면 어떤 일이 벌어질까요?

```csharp
// 같은 이름, 같은 가격이지만 ID가 다르면 다른 Entity
var product1 = Product.Create("노트북", 1_500_000m);
var product2 = Product.Create("노트북", 1_500_000m);
product1 == product2  // false - ID가 다름

// 같은 ID면 같은 Entity
var id = ProductId.New();
var productA = Product.CreateFromValidated(id, "마우스", 25_000m);
var productB = Product.CreateFromValidated(id, "마우스", 25_000m);
productA == productB  // true - ID가 같음
```

두 상품의 이름과 가격이 완전히 같아도, 각각 다른 ID를 가지면 서로 다른 상품입니다. 반대로 ID가 같으면 속성이 바뀌어도 같은 상품이죠. 이것이 **Entity의 Identity** 개념입니다.

### IEntityId<TId> 인터페이스

그렇다면 ID를 어떤 타입으로 만들어야 할까요? Functorium은 Ulid(Universally Unique Lexicographically Sortable Identifier) 기반의 강타입 ID를 제공합니다.

```csharp
public interface IEntityId<T> : IEquatable<T>, IComparable<T>
    where T : struct, IEntityId<T>
{
    Ulid Value { get; }
    static abstract T New();           // 새 ID 생성
    static abstract T Create(Ulid id); // Ulid에서 복원
    static abstract T Create(string id); // 문자열에서 복원
}
```

Ulid를 사용하면 다음과 같은 이점이 있습니다:
- **시간 순서 정렬** 가능 (앞 48비트가 타임스탬프)
- **문자열 표현**이 짧고 URL-safe (26자)
- **UUID 호환** 가능

### [GenerateEntityId] 소스 생성기

실무에서는 Entity ID를 직접 구현하지 않고 `[GenerateEntityId]` 소스 생성기를 사용합니다. 이 장에서는 학습을 위해 소스 생성기가 만드는 코드를 수동으로 구현합니다.

---

## 프로젝트 설명

### 프로젝트 구조
```
EntityAndIdentity/
├── Program.cs                  # 데모 실행
├── ProductId.cs                # Ulid 기반 Entity ID
├── Product.cs                  # 상품 Entity
└── EntityAndIdentity.csproj

EntityAndIdentity.Tests.Unit/
├── ProductTests.cs             # ID 동등성, Entity 동등성 테스트
├── Using.cs
├── xunit.runner.json
└── EntityAndIdentity.Tests.Unit.csproj
```

### 핵심 코드

#### ProductId.cs

`IEntityId<ProductId>`를 구현하면 Ulid 기반의 강타입 식별자가 완성됩니다. `New()`로 새 ID를 만들고, `Create()`로 기존 값에서 복원하세요.

```csharp
public readonly record struct ProductId : IEntityId<ProductId>
{
    public Ulid Value { get; }

    private ProductId(Ulid value) => Value = value;

    public static ProductId New() => new(Ulid.NewUlid());
    public static ProductId Create(Ulid id) => new(id);
    public static ProductId Create(string id) => new(Ulid.Parse(id));

    public bool Equals(ProductId other) => Value == other.Value;
    public int CompareTo(ProductId other) => Value.CompareTo(other.Value);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}
```

`readonly record struct`를 사용하여 불변성을 보장하고, 모든 비교 연산을 내부 `Ulid` 값에 위임합니다.

#### Product.cs

이제 이 ID를 사용하는 Entity를 만들어 보겠습니다. `Entity<ProductId>`를 상속하면 ID 기반 동등성 비교가 자동으로 제공됩니다.

```csharp
public sealed class Product : Entity<ProductId>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product(ProductId id, string name, decimal price)
    {
        Id = id;
        Name = name;
        Price = price;
    }

    public static Product Create(string name, decimal price)
    {
        return new Product(ProductId.New(), name, price);
    }

    public static Product CreateFromValidated(ProductId id, string name, decimal price)
    {
        return new Product(id, name, price);
    }
}
```

`Create()`는 새 ID를 자동 생성하고, `CreateFromValidated()`는 기존 ID로 Entity를 복원합니다. 생성자를 `private`으로 숨겨 반드시 팩토리 메서드를 통해 생성하도록 강제하는 점에 주목하세요.

---

## 한눈에 보는 정리

### Entity vs Value Object

Entity와 Value Object는 동등성 판단 기준이 다릅니다. 아래 표에서 핵심 차이를 확인하세요.

| 구분 | Entity | Value Object |
|------|--------|-------------|
| **동등성** | ID 기반 | 값 기반 |
| **가변성** | 상태 변경 가능 | 불변 |
| **수명** | 고유한 생명주기 | Entity에 종속 |
| **예시** | Product, Order | Money, Address |

### IEntityId<TId> 주요 메서드

ID 타입이 제공하는 메서드와 그 용도를 정리하면 다음과 같습니다.

| 메서드 | 설명 |
|--------|------|
| `New()` | 새로운 Ulid 기반 ID 생성 |
| `Create(Ulid)` | Ulid 값에서 ID 복원 |
| `Create(string)` | 문자열에서 ID 복원 (DB/API 역직렬화) |
| `Value` | 내부 Ulid 값 접근 |

---

## FAQ

### Q1: 왜 Guid가 아닌 Ulid를 사용하나요?
**A**: Ulid는 Guid와 달리 **시간 순서 정렬**이 가능합니다. 앞 48비트가 밀리초 타임스탬프이므로 DB 인덱스 성능이 좋고, 생성 순서대로 자연스럽게 정렬됩니다. 또한 26자 문자열 표현이 UUID의 36자보다 짧습니다.

### Q2: `readonly record struct`를 사용하는 이유는?
**A**: Entity ID는 불변이어야 하므로 `readonly struct`를 사용합니다. `record struct`는 `Equals`, `GetHashCode`, `ToString`을 자동 생성하지만, `IEntityId<T>`의 요구사항에 맞게 명시적으로 구현합니다.

### Q3: `Create`와 `CreateFromValidated`의 차이는?
**A**: `Create`는 새 ID를 자동 생성하는 일반 팩토리 메서드입니다. `CreateFromValidated`는 이미 존재하는 ID로 Entity를 복원하는 메서드로, Repository에서 DB 데이터를 Entity로 변환할 때 사용합니다.

### Q4: Entity<TId>에서 `protected init`을 사용하는 이유는?
**A**: `Id` 속성은 생성 시에만 설정되어야 하므로 `init`을 사용합니다. `protected`는 파생 클래스에서만 설정할 수 있도록 제한하여, 외부에서 ID를 변경하는 것을 방지합니다.

---

Entity의 정체성을 ID로 다루는 법을 배웠습니다. 그런데 외부 코드에서 Entity 내부 상태를 직접 수정하면 어떻게 될까요? 다음 장에서는 Aggregate Root를 통해 일관성 경계를 보호하는 방법을 살펴봅니다.

→ [2장: Aggregate Root](../02-Aggregate-Root/)
