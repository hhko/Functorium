---
title: "엔티티와 애그리거트 사양"
---

Functorium 프레임워크가 제공하는 엔티티(Entity)와 애그리거트(Aggregate) 관련 공개 타입의 API 사양입니다. 설계 원칙과 구현 패턴은 [Entity와 Aggregate 구현 가이드](../guides/domain/06b-entity-aggregate-core)를 참조하십시오.

## 요약

### 주요 타입

| 타입 | 네임스페이스 | 설명 |
|------|-------------|------|
| `IEntity` | `Functorium.Domains.Entities` | Entity 명명 규칙 상수 정의 |
| `IEntity<TId>` | `Functorium.Domains.Entities` | Entity 기본 인터페이스 (ID 기반 동등성 계약) |
| `IEntityId<T>` | `Functorium.Domains.Entities` | Ulid 기반 Entity ID 인터페이스 |
| `Entity<TId>` | `Functorium.Domains.Entities` | Entity 기반 추상 클래스 (동등성, 프록시 지원) |
| `AggregateRoot<TId>` | `Functorium.Domains.Entities` | Aggregate Root 기반 추상 클래스 (도메인 이벤트 관리) |
| `GenerateEntityIdAttribute` | `Functorium.Domains.Entities` | EntityId 소스 생성기 트리거 속성 |
| `IAuditable` | `Functorium.Domains.Entities` | 생성/수정 시각 추적 믹스인 |
| `IAuditableWithUser` | `Functorium.Domains.Entities` | 생성/수정 시각 + 사용자 추적 믹스인 |
| `IConcurrencyAware` | `Functorium.Domains.Entities` | 낙관적 동시성 제어 믹스인 |
| `ISoftDeletable` | `Functorium.Domains.Entities` | 소프트 삭제 믹스인 |
| `ISoftDeletableWithUser` | `Functorium.Domains.Entities` | 소프트 삭제 + 삭제자 추적 믹스인 |
| `IDomainService` | `Functorium.Domains.Services` | 도메인 서비스 마커 인터페이스 |

---

## IEntity / IEntity\<TId\>

Entity의 계약을 정의하는 인터페이스입니다.

### IEntity (비제네릭)

```csharp
public interface IEntity
{
    public static class ArchTestContract
    {
        public const string CreateMethodName = "Create";
        public const string CreateFromValidatedMethodName = "CreateFromValidated";
    }
}
```

### ArchTestContract 상수

아키텍처 테스트(ArchUnitNET) 스위트가 모든 Entity/AggregateRoot 구현체에 대해 enforce하는 네이밍 계약입니다. 프로덕션 로직은 참조하지 않습니다.

| 상수 | 값 | 설명 |
|------|----|------|
| `CreateMethodName` | `"Create"` | 새 Entity 생성 팩토리 메서드 이름 |
| `CreateFromValidatedMethodName` | `"CreateFromValidated"` | 검증 완료 데이터로 Entity를 복원하는 메서드 이름 (Repository/ORM용) |

### IEntity\<TId\>

```csharp
public interface IEntity<TId> : IEntity
    where TId : struct, IEntityId<TId>
{
    TId Id { get; }
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `Id` | `TId` | Entity의 고유 식별자 |

**제네릭 제약 조건:** `TId`는 `struct`이면서 `IEntityId<TId>`를 구현해야 합니다.

---

## IEntityId\<T\>

Ulid 기반 Entity ID의 인터페이스입니다. 시간 순서 정렬이 가능하며, `IEquatable<T>`, `IComparable<T>`, `IParsable<T>`를 상속합니다.

```csharp
public interface IEntityId<T> : IEquatable<T>, IComparable<T>, IParsable<T>
    where T : struct, IEntityId<T>
{
    Ulid Value { get; }

    static abstract T New();
    static abstract T Create(Ulid id);
    static abstract T Create(string id);
}
```

| 멤버 | 반환 타입 | 설명 |
|------|----------|------|
| `Value` | `Ulid` | Ulid 값 |
| `New()` | `T` | 새로운 EntityId 생성 (정적 추상) |
| `Create(Ulid id)` | `T` | Ulid로부터 EntityId 생성 (정적 추상) |
| `Create(string id)` | `T` | 문자열로부터 EntityId 생성 (정적 추상). 유효하지 않은 형식이면 `FormatException` 발생 |

---

## Entity\<TId\>

ID 기반 동등성 비교를 제공하는 Entity의 기반 추상 클래스입니다. ORM 프록시 타입(Castle, NHibernate, EF Core Proxies)도 처리합니다.

```csharp
[Serializable]
public abstract class Entity<TId> : IEntity<TId>, IEquatable<Entity<TId>>
    where TId : struct, IEntityId<TId>
```

### 속성

| 속성 | 타입 | 접근자 | 설명 |
|------|------|--------|------|
| `Id` | `TId` | `public get; protected init` | Entity의 고유 식별자 |

### 생성자

| 시그니처 | 접근 수준 | 설명 |
|----------|----------|------|
| `Entity()` | `protected` | 기본 생성자 (ORM/직렬화용) |
| `Entity(TId id)` | `protected` | ID를 지정하여 Entity 생성 |

### 메서드

| 메서드 | 반환 타입 | 접근 수준 | 설명 |
|--------|----------|----------|------|
| `Equals(object? obj)` | `bool` | `public` | ID 기반 동등성 비교 (프록시 타입 고려) |
| `Equals(Entity<TId>? other)` | `bool` | `public` | 타입 안전한 동등성 비교 |
| `GetHashCode()` | `int` | `public` | ID 기반 해시코드 |
| `operator ==(Entity<TId>?, Entity<TId>?)` | `bool` | `public static` | 동등성 연산자 |
| `operator !=(Entity<TId>?, Entity<TId>?)` | `bool` | `public static` | 부등성 연산자 |
| `CreateFromValidation<TEntity, TValue>(Validation<Error, TValue>, Func<TValue, TEntity>)` | `Fin<TEntity>` | `public static` | LanguageExt Validation을 사용한 팩토리 헬퍼 |
| `GetUnproxiedType(object obj)` | `Type` | `protected static` | ORM 프록시를 제거하고 실제 타입 반환 |

### 최소 사용 예제

```csharp
[GenerateEntityId]
public class Product : Entity<ProductId>
{
#pragma warning disable CS8618
    private Product() { }
#pragma warning restore CS8618

    private Product(ProductId id, ProductName name) : base(id)
    {
        Name = name;
    }

    public ProductName Name { get; private set; }

    public static Product Create(ProductName name)
        => new(ProductId.New(), name);

    public static Product CreateFromValidated(ProductId id, ProductName name)
        => new(id, name);
}
```

---

## AggregateRoot\<TId\>

도메인 이벤트 관리 기능을 제공하는 Aggregate Root의 기반 추상 클래스입니다. **Entity\<TId\>를** 상속하고 `IDomainEventDrain`(internal)을 구현합니다.

```csharp
public abstract class AggregateRoot<TId> : Entity<TId>, IDomainEventDrain
    where TId : struct, IEntityId<TId>
```

### 속성

| 속성 | 타입 | 접근자 | 설명 |
|------|------|--------|------|
| `DomainEvents` | `IReadOnlyList<IDomainEvent>` | `public get` | 도메인 이벤트 목록 (읽기 전용) |

### 생성자

| 시그니처 | 접근 수준 | 설명 |
|----------|----------|------|
| `AggregateRoot()` | `protected` | 기본 생성자 (ORM/직렬화용) |
| `AggregateRoot(TId id)` | `protected` | ID를 지정하여 Aggregate Root 생성 |

### 메서드

| 메서드 | 반환 타입 | 접근 수준 | 설명 |
|--------|----------|----------|------|
| `AddDomainEvent(IDomainEvent domainEvent)` | `void` | `protected` | 도메인 이벤트 추가 |
| `ClearDomainEvents()` | `void` | `public` | 모든 도메인 이벤트 제거 (`IDomainEventDrain` 구현) |

### 인터페이스 분리

**AggregateRoot\<TId\>는** 도메인 이벤트에 대해 두 인터페이스를 분리합니다.

| 인터페이스 | 접근 수준 | 역할 |
|-----------|----------|------|
| `IHasDomainEvents` | `public` | 이벤트 조회 전용 (`DomainEvents` 속성) |
| `IDomainEventDrain` | `internal` | 이벤트 정리 (`ClearDomainEvents()`) — 인프라 관심사 |

### 최소 사용 예제

```csharp
[GenerateEntityId]
public class Order : AggregateRoot<OrderId>
{
#pragma warning disable CS8618
    private Order() { }
#pragma warning restore CS8618

    private Order(OrderId id, Money totalAmount) : base(id)
    {
        TotalAmount = totalAmount;
        Status = OrderStatus.Pending;
    }

    public Money TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }

    public static Order Create(Money totalAmount)
    {
        var id = OrderId.New();
        var order = new Order(id, totalAmount);
        order.AddDomainEvent(new OrderCreatedEvent(id, totalAmount));
        return order;
    }

    public Fin<Unit> Confirm()
    {
        if (!Status.CanTransitionTo(OrderStatus.Confirmed))
            return Fin<Unit>.Fail(Error.New("Cannot confirm order"));

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
        return unit;
    }
}
```

---

## GenerateEntityIdAttribute

Entity 클래스에 적용하면 소스 생성기가 EntityId 관련 타입을 자동으로 생성합니다.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class GenerateEntityIdAttribute : Attribute;
```

### 생성되는 타입

`[GenerateEntityId]`를 `Product` 클래스에 적용하면 다음 타입이 생성됩니다.

| 생성 타입 | 종류 | 설명 |
|----------|------|------|
| `ProductId` | `readonly partial record struct` | Ulid 기반 EntityId (`IEntityId<ProductId>` 구현) |
| `ProductIdComparer` | `sealed class` | EF Core `ValueComparer<ProductId>` (변경 추적용) |
| `ProductIdConverter` | `sealed class` | EF Core `ValueConverter<ProductId, string>` (DB 저장 시 문자열 변환) |

### 생성되는 ProductId 멤버

| 멤버 | 타입/반환 | 설명 |
|------|----------|------|
| `Name` | `const string` | 타입 이름 상수 (`"ProductId"`) |
| `Namespace` | `const string` | 네임스페이스 상수 |
| `Empty` | `static readonly ProductId` | 빈 값 (`Ulid.Empty` 기반) |
| `Value` | `Ulid { get; init; }` | Ulid 값 |
| `New()` | `static ProductId` | 새 ID 생성 |
| `Create(Ulid id)` | `static ProductId` | Ulid로부터 생성 |
| `Create(string id)` | `static ProductId` | 문자열로부터 생성 (`FormatException` 가능) |
| `CompareTo(ProductId other)` | `int` | Ulid 기반 비교 |
| `<`, `>`, `<=`, `>=` | `bool` | 비교 연산자 |
| `Parse(string, IFormatProvider?)` | `static ProductId` | `IParsable<T>` 구현 |
| `TryParse(string?, IFormatProvider?, out ProductId)` | `static bool` | `IParsable<T>` 구현 |
| `ToString()` | `string` | Ulid 문자열 표현 |

생성된 EntityId에는 `[JsonConverter]`와 `[TypeConverter]` 속성이 자동 적용되어 JSON 직렬화와 타입 변환이 지원됩니다.

### 사용 예제

```csharp
// 새 ID 생성
var productId = ProductId.New();

// 문자열에서 변환
var parsed = ProductId.Create("01ARZ3NDEKTSV4RRFFQ69G5FAV");

// 비교
bool isNewer = productId > parsed;

// EF Core 설정
builder.Property(x => x.Id)
    .HasConversion(new ProductIdConverter())
    .Metadata.SetValueComparer(new ProductIdComparer());
```

---

## 믹스인 인터페이스

Entity 또는 Aggregate Root에 선택적으로 혼합하여 횡단 관심사를 추가하는 인터페이스입니다.

### IAuditable

생성/수정 시각을 추적합니다.

```csharp
public interface IAuditable
{
    DateTime CreatedAt { get; }
    Option<DateTime> UpdatedAt { get; }
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `CreatedAt` | `DateTime` | 생성 시각 |
| `UpdatedAt` | `Option<DateTime>` | 최종 수정 시각 (미수정 시 `None`) |

### IAuditableWithUser

**IAuditable을** 확장하여 사용자 정보를 추가로 추적합니다.

```csharp
public interface IAuditableWithUser : IAuditable
{
    Option<string> CreatedBy { get; }
    Option<string> UpdatedBy { get; }
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `CreatedBy` | `Option<string>` | 생성자 식별자 |
| `UpdatedBy` | `Option<string>` | 최종 수정자 식별자 |

### IConcurrencyAware

낙관적 동시성 제어를 위한 행 버전을 관리합니다. EF Core의 `[Timestamp]`/`IsRowVersion()`과 매핑됩니다.

```csharp
public interface IConcurrencyAware
{
    byte[] RowVersion { get; }
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `RowVersion` | `byte[]` | 낙관적 동시성 제어용 행 버전 |

### ISoftDeletable

소프트 삭제를 지원합니다. `IsDeleted`는 `DeletedAt`에서 파생되는 기본 구현(default interface method)을 제공합니다.

```csharp
public interface ISoftDeletable
{
    Option<DateTime> DeletedAt { get; }
    bool IsDeleted => DeletedAt.IsSome;
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `DeletedAt` | `Option<DateTime>` | 삭제 시각 (미삭제 시 `None`) |
| `IsDeleted` | `bool` | 삭제 여부 (`DeletedAt.IsSome`에서 파생, 기본 구현) |

### ISoftDeletableWithUser

**ISoftDeletable을** 확장하여 삭제자 정보를 추가로 추적합니다.

```csharp
public interface ISoftDeletableWithUser : ISoftDeletable
{
    Option<string> DeletedBy { get; }
}
```

| 속성 | 타입 | 설명 |
|------|------|------|
| `DeletedBy` | `Option<string>` | 삭제자 식별자 |

### 믹스인 적용 예제

```csharp
[GenerateEntityId]
public class Product : AggregateRoot<ProductId>, IAuditableWithUser, ISoftDeletable, IConcurrencyAware
{
    public DateTime CreatedAt { get; private set; }
    public Option<DateTime> UpdatedAt { get; private set; }
    public Option<string> CreatedBy { get; private set; }
    public Option<string> UpdatedBy { get; private set; }
    public Option<DateTime> DeletedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
}
```

---

## IDomainService

여러 Aggregate에 걸친 도메인 로직을 표현하는 마커 인터페이스입니다.

```csharp
public interface IDomainService { }
```

### 설계 규칙

| 규칙 | 설명 |
|------|------|
| Stateless | 호출 간 가변 상태를 유지하지 않음 (Evans Blue Book Ch.9) |
| 기본 패턴 | 순수 함수로 구현 (외부 I/O 없음) |
| Repository 의존 허용 | 대규모 교차 데이터 조회 시 Repository 인터페이스 의존 가능 |
| Port/Adapter 금지 | `IObservablePort` 의존성 없음 (Port/Adapter는 Usecase에서 사용) |
| 배치 위치 | Domain Layer |

### 사용 예제

```csharp
public sealed class PricingService : IDomainService
{
    public static Fin<Money> CalculateDiscount(
        Money originalPrice,
        DiscountRate rate,
        CustomerGrade grade)
    {
        // 여러 Aggregate의 값을 참조하는 순수 함수 로직
        var discount = originalPrice.Value * rate.Value * grade.Multiplier;
        return Money.Create(originalPrice.Value - discount);
    }
}
```

---

## 관련 문서

- [Entity와 Aggregate 구현 — 핵심 패턴](../guides/domain/06b-entity-aggregate-core) — 생성 패턴, 커맨드 메서드, 자식 Entity 관리
- [Aggregate 설계 원칙](../guides/domain/06a-aggregate-design) — Aggregate 경계와 설계 원칙
- [Entity와 Aggregate 구현 — 고급 패턴](../guides/domain/06c-entity-aggregate-advanced) — Cross-Aggregate 관계, 믹스인 실전 예제
- [도메인 이벤트 사양](../09-domain-events) — `IDomainEvent`, `DomainEvent`, Publisher/Collector
- [소스 생성기 사양](../10-source-generators) — EntityId 생성기 상세 사양
