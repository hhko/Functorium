# Chapter 7: 파라미터 검증

메서드의 파라미터 개수와 타입을 아키텍처 테스트로 검증하는 방법을 학습합니다. 팩토리 메서드의 파라미터 시그니처를 강제하여 일관된 API 설계를 보장할 수 있습니다.

## 학습 목표

- `RequireParameterCount`로 정확한 파라미터 개수를 검증하는 방법
- `RequireParameterCountAtLeast`로 최소 파라미터 개수를 검증하는 방법
- `RequireFirstParameterTypeContaining`으로 첫 번째 파라미터 타입을 검증하는 방법
- `RequireAnyParameterTypeContaining`으로 특정 타입의 파라미터 존재 여부를 검증하는 방법

## 도메인 코드

### Address 클래스

3개의 문자열 파라미터를 받는 팩토리 메서드를 가집니다.

```csharp
public sealed class Address
{
    public string City { get; }
    public string Street { get; }
    public string ZipCode { get; }

    private Address(string city, string street, string zipCode)
    {
        City = city;
        Street = street;
        ZipCode = zipCode;
    }

    public static Address Create(string city, string street, string zipCode)
        => new(city, street, zipCode);
}
```

### Coordinate 클래스

2개의 `double` 파라미터를 받는 팩토리 메서드를 가집니다.

```csharp
public sealed class Coordinate
{
    public double Latitude { get; }
    public double Longitude { get; }

    private Coordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static Coordinate Create(double latitude, double longitude)
        => new(latitude, longitude);
}
```

## 테스트 코드

### 정확한 파라미터 개수 검증

```csharp
[Fact]
public void AddressCreate_ShouldHave_ThreeParameters()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Address")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireParameterCount(3)),
            verbose: true)
        .ThrowIfAnyFailures("Address Parameter Count Rule");
}
```

### 최소 파라미터 개수 검증

```csharp
[Fact]
public void FactoryMethods_ShouldHave_AtLeastOneParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireParameterCountAtLeast(1)),
            verbose: true)
        .ThrowIfAnyFailures("Factory Method Minimum Parameter Rule");
}
```

### 첫 번째 파라미터 타입 검증

```csharp
[Fact]
public void AddressCreate_ShouldHave_StringFirstParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Address")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireFirstParameterTypeContaining("String")),
            verbose: true)
        .ThrowIfAnyFailures("Address First Parameter Type Rule");
}
```

### 특정 타입 파라미터 존재 검증

```csharp
[Fact]
public void CoordinateCreate_ShouldHave_DoubleParameter()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ParameterValidation.Domains")
        .And()
        .HaveNameEndingWith("Coordinate")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireAnyParameterTypeContaining("Double")),
            verbose: true)
        .ThrowIfAnyFailures("Coordinate Double Parameter Rule");
}
```

## 핵심 개념

| API | 설명 |
|-----|------|
| `RequireParameterCount(n)` | 정확히 n개의 파라미터를 가져야 함 |
| `RequireParameterCountAtLeast(n)` | 최소 n개 이상의 파라미터를 가져야 함 |
| `RequireFirstParameterTypeContaining(fragment)` | 첫 번째 파라미터의 타입 이름에 문자열이 포함되어야 함 |
| `RequireAnyParameterTypeContaining(fragment)` | 하나 이상의 파라미터 타입 이름에 문자열이 포함되어야 함 |

---

[이전: Chapter 6 - 반환 타입 검증](../02-Return-Type-Validation/README.md) | [다음: Chapter 8 - 프로퍼티와 필드 검증](../04-Property-And-Field-Validation/README.md)
