# Chapter 6: 반환 타입 검증

메서드의 반환 타입을 아키텍처 테스트로 검증하는 방법을 학습합니다. **`Fin<T>`와** 같은 함수형 결과 타입의 사용을 강제하거나, 팩토리 메서드가 자신의 클래스 타입을 반환하는지 검증할 수 있습니다.

## 학습 목표

- `RequireReturnType(typeof(Fin<>))`로 오픈 제네릭 반환 타입을 검증하는 방법
- `RequireReturnTypeOfDeclaringClass()`로 자기 타입 반환을 검증하는 방법
- `RequireReturnTypeContaining("Fin")`으로 반환 타입 이름의 일부를 검증하는 방법

## 도메인 코드

### Email / PhoneNumber 클래스

`Create` 메서드가 `Fin<T>`를 반환하여 생성 실패를 안전하게 표현합니다.

```csharp
public sealed class Email
{
    public string Value { get; }
    private Email(string value) => Value = value;

    public static Fin<Email> Create(string value)
        => string.IsNullOrWhiteSpace(value) || !value.Contains('@')
            ? Fin.Fail<Email>(Error.New("Invalid email"))
            : Fin.Succ(new Email(value));
}
```

### Customer 클래스

`CreateFromValidated`는 이미 검증된 값을 받아 자기 자신의 타입(`Customer`)을 직접 반환합니다.

```csharp
public sealed class Customer
{
    public string Name { get; }
    public Email Email { get; }

    private Customer(string name, Email email)
    {
        Name = name;
        Email = email;
    }

    public static Customer CreateFromValidated(string name, Email email)
        => new(name, email);
}
```

## 테스트 코드

### 오픈 제네릭 반환 타입 검증

`typeof(Fin<>)`를 전달하면 `Fin<Email>`, `Fin<PhoneNumber>` 등 모든 닫힌 제네릭 타입과 매칭됩니다.

```csharp
[Fact]
public void CreateMethods_ShouldReturn_FinOpenGeneric()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Email").Or().HaveNameEndingWith("PhoneNumber")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireReturnType(typeof(Fin<>))),
            verbose: true)
        .ThrowIfAnyFailures("Fin Return Type Rule");
}
```

### 자기 타입 반환 검증

`RequireReturnTypeOfDeclaringClass()`는 메서드의 반환 타입이 선언 클래스와 동일한지 검증합니다.

```csharp
[Fact]
public void CreateFromValidated_ShouldReturn_DeclaringClass()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Customer")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("CreateFromValidated", m => m
                .RequireReturnTypeOfDeclaringClass()),
            verbose: true)
        .ThrowIfAnyFailures("Factory Return Type Rule");
}
```

### 반환 타입 이름 포함 검증

`RequireReturnTypeContaining`은 반환 타입의 전체 이름에 지정된 문자열이 포함되어 있는지 검증합니다.

```csharp
[Fact]
public void CreateMethods_ShouldReturn_TypeContainingFin()
{
    ArchRuleDefinition
        .Classes()
        .That()
        .ResideInNamespace("ReturnTypeValidation.Domains")
        .And()
        .HaveNameEndingWith("Email").Or().HaveNameEndingWith("PhoneNumber")
        .ValidateAllClasses(Architecture, @class => @class
            .RequireMethod("Create", m => m
                .RequireReturnTypeContaining("Fin")),
            verbose: true)
        .ThrowIfAnyFailures("Fin Return Type Containing Rule");
}
```

## 핵심 개념

| API | 설명 |
|-----|------|
| `RequireReturnType(typeof(Fin<>))` | 오픈 제네릭 타입과 매칭 (접두사 비교) |
| `RequireReturnType(typeof(string))` | 정확한 타입 매칭 |
| `RequireReturnTypeOfDeclaringClass()` | 반환 타입이 선언 클래스와 동일한지 검증 |
| `RequireReturnTypeContaining("Fin")` | 반환 타입의 전체 이름에 문자열 포함 여부 검증 |

---

[이전: Chapter 5 - 메서드 검증](../01-Method-Validation/README.md) | [다음: Chapter 7 - 파라미터 검증](../03-Parameter-Validation/README.md)
