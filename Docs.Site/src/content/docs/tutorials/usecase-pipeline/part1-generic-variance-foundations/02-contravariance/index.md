---
title: "Contravariance (in)"
---

## 개요

1장에서 `out` 키워드로 값을 "꺼내기만 하는" 공변성을 학습했습니다. 하지만 값을 **받기만** 하는 타입은 어떨까요?

**반공변성(Contravariance)은** 제네릭 타입 파라미터가 상속 관계를 **반대 방향**으로 유지하는 성질입니다. C#에서는 `in` 키워드로 반공변성을 선언합니다.

```
Dog : Animal  (Dog은 Animal의 하위 타입)
    ↓ 반공변성 (반대 방향)
IAnimalHandler<Animal> → IAnimalHandler<Dog>  (대입 가능)
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `in` 키워드를 사용하여 반공변 인터페이스를 선언할 수 있습니다
2. 반공변 대입이 가능한 이유(입력 위치 제한)를 설명할 수 있습니다
3. 공변성과 반공변성의 방향 차이를 구분할 수 있습니다
4. `Action<in T>`, `IComparer<in T>` 등의 반공변성을 이해할 수 있습니다

## 핵심 개념

### 1. `in` 키워드

`in T`는 타입 파라미터 `T`가 **입력 위치에서만** 사용됨을 컴파일러에 선언합니다.

```csharp
public interface IAnimalHandler<in T> where T : Animal
{
    void Handle(T animal);         // OK: T가 파라미터 (입력 위치)
    // T GetResult();              // 컴파일 에러! T가 반환 타입 (출력 위치)
}
```

### 2. 반공변 대입

`Dog`이 `Animal`의 하위 타입이면, `IAnimalHandler<Animal>`을 `IAnimalHandler<Dog>`에 대입할 수 있습니다. 방향이 **반대**입니다.

```csharp
var animalHandler = new AnimalHandler();

// 반공변성: IAnimalHandler<Animal> → IAnimalHandler<Dog> 대입 가능
IAnimalHandler<Dog> dogHandler = animalHandler;
dogHandler.Handle(new Dog("Buddy", "Golden Retriever"));
```

이것이 가능한 이유는 `AnimalHandler`가 모든 `Animal`을 처리할 수 있으므로, 당연히 `Dog`도 처리할 수 있기 때문입니다.

### 3. .NET의 반공변 타입

.NET에서 대표적인 반공변 타입들입니다. 모두 `in` 키워드로 입력 전용임을 선언하고 있습니다:

| 타입 | 선언 | 설명 |
|------|------|------|
| `Action<in T>` | `in T` | 입력만 받는 대리자 |
| `IComparer<in T>` | `in T` | 비교 대상을 입력으로 받음 |
| `IEqualityComparer<in T>` | `in T` | 동등 비교 대상을 입력으로 받음 |

```csharp
Action<Animal> animalAction = a => Console.WriteLine(a.Name);
Action<Dog> dogAction = animalAction;  // Action<in T> 반공변성
dogAction(new Dog("Buddy", "Golden Retriever"));
```

### 4. 핸들러 대체 원리

반공변성의 실용적 의미는 **핸들러 대체**입니다. 더 일반적인(상위 타입) 핸들러가 더 구체적인(하위 타입) 핸들러를 대체할 수 있습니다.

```
AnimalHandler는 모든 Animal을 처리 가능
    → Dog도 Animal이므로 AnimalHandler가 Dog을 처리 가능
    → IAnimalHandler<Animal>이 IAnimalHandler<Dog>을 대체 가능
```

## FAQ

### Q1: 반공변성에서 왜 상위 타입 핸들러가 하위 타입 핸들러를 대체할 수 있나요?
**A**: `AnimalHandler`는 모든 `Animal`을 처리할 수 있으므로 `Dog`도 당연히 처리 가능합니다. `in T`는 "T를 받기만 한다"는 약속이므로, 더 넓은 범위를 수용하는 상위 타입 핸들러가 하위 타입 전용 핸들러를 안전하게 대체할 수 있습니다.

### Q2: 공변성과 반공변성의 방향이 반대인 이유가 직관적으로 이해되지 않습니다.
**A**: **출력(꺼내기)은** 하위→상위가 안전합니다. `Dog`을 꺼내서 `Animal` 변수에 담는 것은 항상 안전하기 때문입니다. **입력(받기)은** 상위→하위가 안전합니다. 모든 `Animal`을 처리하는 핸들러에 `Dog`을 넘기는 것은 항상 안전하기 때문입니다. 방향이 반대인 이유는 입력과 출력의 타입 안전성 조건이 정반대이기 때문입니다.

### Q3: 반공변성은 Pipeline 설계에서 직접 사용되나요?
**A**: 이 튜토리얼의 Pipeline 설계에서 `in` 키워드를 직접 사용하지는 않습니다. 하지만 반공변성의 개념은 **왜 인터페이스를 읽기/쓰기로 분리해야 하는지**를 이해하는 데 핵심입니다. 4장의 인터페이스 분리 원칙(ISP)에서 이 지식이 활용됩니다.

## 프로젝트 구조

```
02-Contravariance/
├── Contravariance/
│   ├── Contravariance.csproj
│   ├── Animal.cs
│   ├── IAnimalHandler.cs
│   └── Program.cs
├── Contravariance.Tests.Unit/
│   ├── Contravariance.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── ContravarianceTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project Contravariance

# 테스트 실행
dotnet test --project Contravariance.Tests.Unit
```

---

`out`도 `in`도 선언할 수 없는 타입은 어떻게 될까요? `List<T>`의 불변성과 sealed struct의 제약 한계를 살펴봅니다.

→ [1.3장: 불변성과 제약](../03-Invariance-And-Constraints/)

