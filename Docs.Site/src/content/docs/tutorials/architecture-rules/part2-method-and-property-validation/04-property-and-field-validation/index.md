---
title: "Property and Field Validation"
---

## Overview

도메인 클래스에 `public set`이 몰래 추가되면 immutability이 깨집니다. `Product.Price`에 setter를 넣어도 컴파일은 성공하고, 기존 테스트도 읽기만 하므로 통과합니다. 문제는 운영 환경에서 누군가 `product.Price = -100`을 호출할 때 드러나죠. In this chapter, 클래스의 프로퍼티와 필드에 대한 아키텍처 규칙을 검증하여, **public setter** 금지, 인스턴스 필드 금지, 원시 타입만 허용하는 등의 규칙을 테스트로 강제하는 방법을 학습합니다.

> **"immutability은 getter-only 프로퍼티와 private 생성자만으로는 부족합니다. 아키텍처 테스트로 지속적으로 검증해야 합니다."**

## Learning Objectives

### 핵심 학습 목표
1. **필수 프로퍼티 존재 검증**
   - `RequireProperty(name)`으로 도메인 모델의 핵심 프로퍼티 보장
   - 프로퍼티가 실수로 제거되거나 이름이 변경되면 즉시 감지

2. **immutability 강제**
   - `RequireNoPublicSetters()`로 도메인 클래스의 public setter 금지
   - `RequireNoInstanceFields()`로 인스턴스 필드 사용 제한 (backing field 자동 제외)

3. **프로퍼티 타입 제한**
   - `RequireOnlyPrimitiveProperties()`로 원시 타입만 허용
   - 추가 허용 타입을 지정하는 확장 옵션

### 실습을 통해 확인할 내용
- **Product**: `Name`, `Price` 프로퍼티 존재 및 public setter 금지 검증
- **OrderLine**: 인스턴스 필드 금지 및 원시 타입 프로퍼티 검증
- **ProductViewModel**: domain rule 적용 범위에서 제외되는 예시

## 도메인 코드

### Product / OrderLine 클래스

getter-only 프로퍼티와 private 생성자로 immutability을 보장하는 도메인 클래스입니다.

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

ViewModel은 도메인 클래스와 달리 public setter를 가집니다. 이 클래스는 `Domains` 네임스페이스가 아닌 `ViewModels` 네임스페이스에 위치하므로 domain rule의 적용을 받지 않습니다.

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

도메인 클래스에서 public setter를 금지하여 immutability을 강제합니다.

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

## Summary at a Glance

The following table 프로퍼티/필드 검증 API와 각각이 보호하는 설계 원칙을 요약합니다.

### 프로퍼티/필드 검증 API 요약
| API | 보호하는 설계 원칙 | 위반 시 의미 |
|-----|---------------------|--------------|
| `RequireProperty(name)` | 도메인 모델 완전성 | 핵심 프로퍼티가 누락됨 |
| `RequireNoPublicSetters()` | immutability (Immutability) | 외부에서 상태 변경 가능 |
| `RequireNoInstanceFields()` | 캡슐화 | 프로퍼티 대신 필드로 상태 노출 |
| `RequireOnlyPrimitiveProperties(additionalAllowed)` | 타입 안전성 | 복잡한 타입이 도메인에 침투 |

### 도메인 vs 비domain rule 적용
| Aspect | 도메인 클래스 (`Domains` NS) | ViewModel (`ViewModels` NS) |
|------|------------------------------|------------------------------|
| **Public Setter** | 금지 | 허용 |
| **인스턴스 필드** | 금지 | 제한 없음 |
| **프로퍼티 타입** | 원시 타입만 | 제한 없음 |
| **적용 방식** | 네임스페이스 필터로 자동 분리 | 규칙 적용 범위 밖 |

## FAQ

### Q1: RequireNoPublicSetters는 init setter도 금지하나요?
**A**: `init` setter는 객체 초기화 시에만 값을 설정할 수 있으므로 immutability을 해치지 않습니다. `RequireNoPublicSetters()`는 `public set`만 검증하며, `init` setter는 허용합니다. 이는 C# record 타입이나 `required init` 패턴과 호환됩니다.

### Q2: RequireNoInstanceFields에서 backing field가 자동 제외되는 원리는?
**A**: C# 컴파일러는 auto-property(`public string Name { get; }`)에 대해 `<Name>k__BackingField` 같은 이름의 backing field를 자동 생성합니다. `RequireNoInstanceFields()`는 이런 컴파일러 생성 필드를 이름 패턴으로 감지하여 검증 대상에서 제외합니다. 개발자가 직접 선언한 인스턴스 필드만 위반으로 보고합니다.

### Q3: RequireOnlyPrimitiveProperties에서 추가 허용 타입은 어떻게 지정하나요?
**A**: `RequireOnlyPrimitiveProperties("System.DateTime", "System.Guid")` 처럼 추가 허용할 타입의 전체 이름을 문자열로 전달합니다. 기본 원시 타입(`string`, `int`, `decimal`, `double`, `bool` 등)에 더해 지정한 타입도 허용됩니다.

### Q4: ViewModel에는 왜 domain rule을 적용하지 않나요?
**A**: ViewModel은 UI 바인딩을 위한 데이터 전달 객체로, 양방향 바인딩에 `public set`이 필요합니다. 도메인 클래스의 immutability 규칙을 ViewModel에 적용하면 UI 프레임워크와의 호환성이 깨집니다. `ResideInNamespace("...Domains")` 필터로 도메인 네임스페이스만 대상으로 지정하면 자연스럽게 분리됩니다.

### Q5: 여러 검증을 하나의 테스트에서 조합할 수 있나요?
**A**: 네, `@class.RequireProperty("Name").RequireNoPublicSetters().RequireNoInstanceFields()`처럼 체이닝할 수 있습니다. 하지만 검증 목적이 다르면 테스트를 분리하는 것이 좋습니다. 테스트가 실패했을 때 어떤 규칙이 위반되었는지 바로 파악할 수 있기 때문입니다.

---

프로퍼티와 필드 검증으로 도메인 클래스의 immutability을 지속적으로 보장할 수 있게 되었습니다. 다음 Part에서는 `RequireImmutable()`의 6차원 immutability 검증, 중첩 클래스, 인터페이스 검증, 커스텀 규칙 합성 등 고급 기법을 학습합니다.

→ [Part 3의 1장: immutability 규칙](../../Part3-Advanced-Validation/01-Immutability-Rule/)
