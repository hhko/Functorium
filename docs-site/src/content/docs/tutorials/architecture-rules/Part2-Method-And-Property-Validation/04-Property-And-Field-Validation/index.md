---
title: "Chapter 8: 프로퍼티와 필드 검증"
---

클래스의 프로퍼티와 필드에 대한 아키텍처 규칙을 검증하는 방법을 학습합니다. 도메인 클래스의 불변성을 보장하기 위해 **public setter** 금지, 인스턴스 필드 금지, 원시 타입만 허용하는 등의 규칙을 강제할 수 있습니다.

## 학습 목표

- `RequireProperty`로 필수 프로퍼티의 존재를 검증하는 방법
- `RequireNoPublicSetters()`로 불변 설계를 강제하는 방법
- `RequireNoInstanceFields()`로 필드 사용을 제한하는 방법
- `RequireOnlyPrimitiveProperties()`로 프로퍼티 타입을 제한하는 방법

## 도메인 코드

### Product / OrderLine 클래스

getter-only 프로퍼티와 private 생성자로 불변성을 보장하는 도메인 클래스입니다.

```csharp
public sealed class Product
{
    public string Name { get; }
    public decimal Price { get; }
    public int Quantity { get; }

    private Product(string name, decimal price, int quantity)
    {
        Name = name;
        Price = price;
        Quantity = quantity;
    }

    public static Product Create(string name, decimal price, int quantity)
        => new(name, price, quantity);
}
```

### ProductViewModel 클래스

ViewModel은 도메인 클래스와 달리 public setter를 가집니다. 이 클래스는 `Domains` 네임스페이스가 아닌 `ViewModels` 네임스페이스에 위치하므로 도메인 규칙의 적용을 받지 않습니다.

```csharp
public class ProductViewModel
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}
```

## 테스트 코드

### 필수 프로퍼티 존재 검증

`RequireProperty`로 특정 프로퍼티가 반드시 존재하는지 검증합니다.

```csharp
[Fact]
public void Product_ShouldHave_NameAndPriceProperties()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("PropertyAndFieldValidation.Domains")
        .And()
        .HaveNameEndingWith("Product")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireProperty("Name")
            .RequireProperty("Price"),
            verbose: true)
        .ThrowIfAnyFailures("Product Property Rule");
}
```

### Public Setter 금지

도메인 클래스에서 public setter를 금지하여 불변성을 강제합니다.

```csharp
[Fact]
public void DomainClasses_ShouldNotHave_PublicSetters()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("PropertyAndFieldValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNoPublicSetters(),
            verbose: true)
        .ThrowIfAnyFailures("No Public Setter Rule");
}
```

### 인스턴스 필드 금지

도메인 클래스에서 인스턴스 필드 사용을 금지합니다. 컴파일러가 자동 생성하는 backing field는 자동으로 제외됩니다.

```csharp
[Fact]
public void DomainClasses_ShouldNotHave_InstanceFields()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("PropertyAndFieldValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNoInstanceFields(),
            verbose: true)
        .ThrowIfAnyFailures("No Instance Field Rule");
}
```

### 원시 타입만 허용

도메인 클래스의 프로퍼티가 원시 타입(`string`, `int`, `decimal`, `double` 등)만 사용하는지 검증합니다.

```csharp
[Fact]
public void DomainClasses_ShouldHave_OnlyPrimitiveProperties()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("PropertyAndFieldValidation.Domains")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireOnlyPrimitiveProperties(),
            verbose: true)
        .ThrowIfAnyFailures("Primitive Property Rule");
}
```

## 핵심 개념

| API | 설명 |
|-----|------|
| `RequireProperty(name)` | 지정된 이름의 프로퍼티가 반드시 존재해야 함 |
| `RequireNoPublicSetters()` | public setter가 없어야 함 (불변성 강제) |
| `RequireNoInstanceFields()` | 인스턴스 필드가 없어야 함 (backing field 제외) |
| `RequireOnlyPrimitiveProperties(additionalAllowed)` | 원시 타입 프로퍼티만 허용 (추가 허용 타입 지정 가능) |

---

[이전: Chapter 7 - 파라미터 검증](../03-Parameter-Validation/) | [다음: Chapter 9 - 불변성 규칙](../../Part3-Advanced-Validation/01-Immutability-Rule/)
