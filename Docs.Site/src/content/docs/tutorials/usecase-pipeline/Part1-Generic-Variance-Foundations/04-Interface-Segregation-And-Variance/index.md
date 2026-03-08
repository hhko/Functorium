---
title: "인터페이스 분리와 변성"
---

## 개요

3장에서 sealed struct의 한계를 인터페이스로 우회할 수 있다는 것을 확인했습니다. 그런데 하나의 인터페이스가 읽기와 쓰기를 모두 포함하면 변성을 선언할 수 없습니다. 해결책은 **인터페이스 분리 원칙(ISP)입니다.**

ISP를 변성과 결합하면, 각 인터페이스에 적절한 변성을 부여할 수 있습니다. 읽기 인터페이스에는 `out`(공변), 쓰기 인터페이스에는 `in`(반공변), 팩토리 인터페이스에는 CRTP(Curiously Recurring Template Pattern)를 적용합니다.

```
IReadable<out T>     → 공변 (읽기 전용)
IWritable<in T>      → 반공변 (쓰기 전용)
IFactory<TSelf>      → CRTP (static abstract 팩토리)
```

## 핵심 개념

### 1. 읽기 인터페이스 - 공변(out)

읽기만 하는 인터페이스는 `out` 키워드로 공변성을 선언합니다.

```csharp
public interface IReadable<out T>
{
    T Value { get; }
    bool IsValid { get; }
}
```

### 2. 쓰기 인터페이스 - 반공변(in)

쓰기만 하는 인터페이스는 `in` 키워드로 반공변성을 선언합니다.

```csharp
public interface IWritable<in T>
{
    void Write(T value);
}
```

### 3. 팩토리 인터페이스 - CRTP

C# 11의 `static abstract` 멤버를 활용한 CRTP 팩토리 패턴입니다. 구현 타입 자신을 타입 파라미터로 전달하여 팩토리 메서드의 반환 타입을 정확히 지정합니다.

```csharp
public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
{
    static abstract TSelf Create(string value);
    static abstract TSelf CreateEmpty();
}
```

### 4. 인터페이스 조합

여러 인터페이스를 조합하여 필요한 능력만 제약할 수 있습니다.

```csharp
// 읽기+쓰기 = 불변 (두 인터페이스를 동시 구현)
public interface IReadWrite<T> : IReadable<T>, IWritable<T>;
```

### 5. 이 패턴이 중요한 이유

이 패턴은 이후 장에서 설계할 **IFinResponse 계층**의 기초입니다. 각 인터페이스가 어떻게 매핑되는지 확인하세요:

| 이 장의 인터페이스 | IFinResponse 계층 | 역할 |
|-------------------|-------------------|------|
| `IReadable<out T>` | `IFinResponse<out A>` | 공변적 읽기 접근 |
| `IFactory<TSelf>` | `IFinResponseFactory<TSelf>` | CRTP 팩토리 (CreateFail) |

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. ISP를 적용하여 인터페이스를 읽기/쓰기/팩토리로 분리할 수 있다
2. 분리된 인터페이스에 적절한 변성(out/in)을 부여할 수 있다
3. CRTP 패턴으로 `static abstract` 팩토리를 구현할 수 있다
4. `where T : IFactory<T>` 제약을 활용하여 제네릭 팩토리 메서드를 작성할 수 있다
5. 이 패턴이 IFinResponse 계층 설계와 어떻게 연결되는지 이해할 수 있다

## FAQ

### Q1: 인터페이스를 왜 하나로 합치지 않고 읽기/쓰기/팩토리로 분리하나요?
**A**: 하나의 인터페이스가 읽기와 쓰기를 모두 포함하면 `out`(공변)도 `in`(반공변)도 선언할 수 없어 **불변**이 됩니다. 분리해야 각 인터페이스에 적절한 변성을 부여할 수 있고, Pipeline은 필요한 인터페이스만 제약으로 사용하여 **최소 권한 원칙**을 지킬 수 있습니다.

### Q2: CRTP 패턴의 `where TSelf : IFactory<TSelf>` 제약은 무엇을 보장하나요?
**A**: 이 제약은 `TSelf`가 반드시 `IFactory<TSelf>`를 구현한 타입이어야 함을 보장합니다. 이를 통해 `static abstract` 팩토리 메서드의 반환 타입이 **자기 자신의 정확한 타입**이 되어, 런타임 캐스팅 없이 올바른 타입의 인스턴스를 생성할 수 있습니다.

### Q3: 이 장에서 배운 패턴이 IFinResponse 계층에 어떻게 대응되나요?
**A**: `IReadable<out T>`는 `IFinResponse<out A>`로, `IFactory<TSelf>`는 `IFinResponseFactory<TSelf>`로 대응됩니다. 읽기 인터페이스에는 공변성을 적용하여 유연한 타입 대입을 지원하고, 팩토리 인터페이스에는 CRTP를 적용하여 리플렉션 없는 응답 생성을 지원합니다.

### Q4: `IReadWrite<T>`처럼 분리된 인터페이스를 조합할 때 변성은 어떻게 되나요?
**A**: `IReadWrite<T>`가 `IReadable<T>`와 `IWritable<T>`를 동시에 상속하면, T가 입력과 출력 양쪽에서 사용되므로 **불변**이 됩니다. 조합 인터페이스는 변성을 잃지만, Pipeline에서는 필요한 능력의 인터페이스만 개별적으로 제약하므로 변성을 유지할 수 있습니다.

Part 1에서 제네릭 변성의 기초를 다졌습니다. Part 2에서는 이 지식을 Mediator Pipeline에 적용했을 때 만나는 구체적인 문제를 정의합니다.

## 프로젝트 구조

```
04-Interface-Segregation-And-Variance/
├── InterfaceSegregationAndVariance/
│   ├── InterfaceSegregationAndVariance.csproj
│   ├── Interfaces.cs
│   └── Program.cs
├── InterfaceSegregationAndVariance.Tests.Unit/
│   ├── InterfaceSegregationAndVariance.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── InterfaceSegregationTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project InterfaceSegregationAndVariance

# 테스트 실행
dotnet test --project InterfaceSegregationAndVariance.Tests.Unit
```

