---
title: "4장: 인터페이스 분리와 변성 조합"
---

## 개요

**인터페이스 분리 원칙(ISP, Interface Segregation Principle)을** 변성과 결합하면, 각 인터페이스에 적절한 변성을 부여할 수 있습니다. 읽기 인터페이스에는 `out`(공변), 쓰기 인터페이스에는 `in`(반공변), 팩토리 인터페이스에는 CRTP(Curiously Recurring Template Pattern)를 적용합니다.

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

이 패턴은 이후 장에서 설계할 **IFinResponse 계층**의 기초입니다:

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

---

[← 이전: 3장 불변성과 제약](../03-Invariance-And-Constraints/) | [다음: Part 2 문제 정의 →](../../Part2-Problem-Definition/01-Mediator-Pipeline-Structure/)
