---
title: "Nested Class Validation"
---

## Overview

Command에 `Request`가 빠져 있다면? Query에 `Response`가 없다면? Mediator 파이프라인이 runtime에 실패하고 나서야 "중첩 클래스를 깜빡했다"는 사실을 알게 됩니다. 이런 구조적 누락은 컴파일러가 잡아주지 않습니다.

이 챕터에서는 `RequireNestedClass()`와 `RequireNestedClassIfExists()`를 사용하여 중첩 클래스의 존재와 구조를 **compile time이 아닌 테스트 타임에** 자동으로 검증하는 방법을 학습합니다.

> **"구조적 규칙은 코드 리뷰에서 잡기엔 너무 쉽게 빠집니다. 테스트가 '이 Command에 Request가 없습니다'라고 알려주면, 누락은 커밋 전에 발견됩니다."**

## Learning Objectives

### 핵심 학습 목표

1. **`RequireNestedClass()`로 필수 중첩 클래스 검증**
   - 지정된 이름의 중첩 클래스가 반드시 존재해야 하는 규칙
   - 두 번째 매개변수로 중첩 클래스에 대한 추가 규칙 체이닝

2. **`RequireNestedClassIfExists()`로 선택적 중첩 클래스 검증**
   - 중첩 클래스가 있을 때만 검증하고, 없으면 통과하는 패턴
   - Validator처럼 선택적 요소에 적합한 검증 전략

3. **`.AreNotNested()` 필터의 역할**
   - 최상위 클래스만 대상으로 하여 중첩 클래스 자체가 검증 대상이 되는 것을 방지

### 실습을 통해 확인할 내용
- **CreateOrder**: Command 패턴 — sealed `Request`와 `Response` 중첩 클래스 포함
- **GetOrderById**: Query 패턴 — 동일한 중첩 클래스 구조
- **선택적 Validator**: 존재할 때만 sealed 여부를 검증하는 패턴

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

### 중첩 클래스의 immutability 검증

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

## Summary at a Glance

The following table 중첩 클래스 검증 메서드의 동작 차이를 compares.

### 중첩 클래스 검증 메서드 비교

| 메서드 | 중첩 클래스 없을 때 | 중첩 클래스 있을 때 | 사용 시나리오 |
|--------|---------------------|---------------------|---------------|
| **`RequireNestedClass(name, validation)`** | 위반 보고 | 콜백 규칙 검증 | Request, Response 등 필수 요소 |
| **`RequireNestedClassIfExists(name, validation)`** | 통과 (무시) | 콜백 규칙 검증 | Validator 등 선택적 요소 |

The following table 이 챕터에서 사용한 주요 필터와 검증 규칙을 정리합니다.

### 필터 및 검증 규칙 요약

| Aspect | 역할 |
|------|------|
| `.AreNotNested()` | 최상위 클래스만 필터링 (중첩 클래스 제외) |
| `RequireSealed()` | 중첩 클래스가 sealed인지 검증 |
| `RequireImmutable()` | 중첩 클래스의 immutability 검증 |
| 콜백 체이닝 | 중첩 클래스에 여러 규칙을 순차적으로 적용 |

## FAQ

### Q1: `RequireNestedClass()`에서 중첩 클래스가 없으면 어떤 메시지가 출력되나요?
**A**: `RuleViolation`으로 "Class 'CreateOrder' must have nested class 'Request'"와 같은 구체적인 위반 메시지가 보고됩니다. 어떤 클래스에 어떤 중첩 클래스가 누락되었는지 바로 알 수 있습니다.

### Q2: `.AreNotNested()`를 빼면 어떻게 되나요?
**A**: `Request`, `Response` 같은 중첩 클래스 자체도 검증 대상이 됩니다. 그러면 `Request` 안에서 다시 `Request` 중첩 클래스를 찾으려 하므로 의도하지 않은 위반이 보고됩니다.

### Q3: 중첩 클래스에 대한 콜백에서 또 다른 `RequireNestedClass()`를 호출할 수 있나요?
**A**: 네, 중첩 검증은 재귀적으로 사용할 수 있습니다. 예를 들어 `RequireNestedClass("Request", nested => nested.RequireNestedClass("Metadata", ...))`처럼 다단계 중첩 구조도 검증할 수 있습니다.

### Q4: `RequireNestedClassIfExists()`는 언제 사용하나요?
**A**: Validator, Mapper, Profile처럼 모든 Command에 필수는 아니지만, 있을 경우 특정 규칙을 따라야 하는 선택적 중첩 클래스에 적합합니다. "있으면 규칙을 지켜라, 없어도 괜찮다"는 유연한 검증을 provides.

---

중첩 클래스의 존재와 구조를 자동으로 검증하면, runtime 실패 대신 테스트 실패로 구조적 누락을 조기에 발견할 수 있습니다. Next chapter에서는 인터페이스의 네이밍 규칙과 메서드 시그니처를 검증하는 방법을 examines.

→ [3장: 인터페이스 검증](../03-Interface-Validation/)
