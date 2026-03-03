---
title: "Chapter 10: 중첩 클래스 검증 (Nested Class Validation)"
---

## 소개

**Command/Query 패턴에서는** 각 명령에 `Request`와 `Response` 중첩 클래스를 정의하는 것이 일반적입니다.
이 챕터에서는 `RequireNestedClass()`와 `RequireNestedClassIfExists()`를 사용하여 중첩 클래스의 존재와 구조를 검증하는 방법을 학습합니다.

## 학습 목표

- `RequireNestedClass()`로 필수 중첩 클래스 검증
- `RequireNestedClassIfExists()`로 선택적 중첩 클래스 검증
- 중첩 클래스에 대한 추가 규칙 체이닝

## 도메인 코드

### CreateOrder - Command 패턴

```csharp
public sealed class CreateOrder
{
    public sealed class Request
    {
        public string CustomerName { get; }
        public string ProductName { get; }

        private Request(string customerName, string productName)
        {
            CustomerName = customerName;
            ProductName = productName;
        }

        public static Request Create(string customerName, string productName)
            => new(customerName, productName);
    }

    public sealed class Response
    {
        public string OrderId { get; }
        public bool Success { get; }

        private Response(string orderId, bool success)
        {
            OrderId = orderId;
            Success = success;
        }

        public static Response Create(string orderId, bool success)
            => new(orderId, success);
    }
}
```

### GetOrderById - Query 패턴

```csharp
public sealed class GetOrderById
{
    public sealed class Request
    {
        public string OrderId { get; }
        private Request(string orderId) => OrderId = orderId;
        public static Request Create(string orderId) => new(orderId);
    }

    public sealed class Response
    {
        public string OrderId { get; }
        public string CustomerName { get; }

        private Response(string orderId, string customerName)
        {
            OrderId = orderId;
            CustomerName = customerName;
        }

        public static Response Create(string orderId, string customerName)
            => new(orderId, customerName);
    }
}
```

두 클래스 모두 `Request`와 `Response` 중첩 클래스를 포함하며, 각각 불변 패턴을 따릅니다.

## 테스트 코드

### 필수 중첩 클래스 검증

`RequireNestedClass()`는 지정된 이름의 중첩 클래스가 반드시 존재해야 하며, 존재하지 않으면 위반을 보고합니다.
두 번째 매개변수로 중첩 클래스에 대한 추가 검증을 수행할 수 있습니다.

```csharp
[Fact]
public void CommandClasses_ShouldHave_SealedRequestAndResponse()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ApplicationNamespace)
        .And()
        .AreNotNested()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNestedClass("Request", nested => nested
                .RequireSealed())
            .RequireNestedClass("Response", nested => nested
                .RequireSealed()),
            verbose: true)
        .ThrowIfAnyFailures("Command Nested Class Rule");
}
```

`.AreNotNested()`를 사용하여 최상위 클래스만 대상으로 합니다.
중첩 클래스 자체가 검증 대상이 되는 것을 방지합니다.

### 중첩 클래스의 불변성 검증

```csharp
[Fact]
public void CommandClasses_ShouldHave_ImmutableNestedClasses()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ApplicationNamespace)
        .And()
        .AreNotNested()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNestedClass("Request", nested => nested
                .RequireSealed()
                .RequireImmutable())
            .RequireNestedClass("Response", nested => nested
                .RequireSealed()
                .RequireImmutable()),
            verbose: true)
        .ThrowIfAnyFailures("Command Nested Immutability Rule");
}
```

중첩 클래스 검증 콜백 안에서 `RequireImmutable()`을 체이닝할 수 있습니다.

### 선택적 중첩 클래스 검증

`RequireNestedClassIfExists()`는 중첩 클래스가 있을 때만 검증하고, 없으면 통과합니다.

```csharp
[Fact]
public void CommandClasses_ShouldOptionallyHave_Validator()
{
    ArchRuleDefinition.Classes()
        .That()
        .ResideInNamespace(ApplicationNamespace)
        .And()
        .AreNotNested()
        .ValidateAllClasses(Architecture, @class => @class
            .RequireNestedClassIfExists("Validator", nested => nested
                .RequireSealed()),
            verbose: true)
        .ThrowIfAnyFailures("Optional Validator Nested Class Rule");
}
```

## 핵심 정리

| 메서드 | 동작 |
|--------|------|
| `RequireNestedClass(name, validation)` | 중첩 클래스 필수, 없으면 위반 |
| `RequireNestedClassIfExists(name, validation)` | 중첩 클래스 존재 시에만 검증 |
| `.AreNotNested()` | 최상위 클래스만 필터링 |
| 콜백 체이닝 | 중첩 클래스에 `RequireSealed()`, `RequireImmutable()` 등 적용 |

---

[이전: Chapter 9 - Immutability Rule](../01-Immutability-Rule/) | [다음: Chapter 11 - Interface Validation](../03-Interface-Validation/)
