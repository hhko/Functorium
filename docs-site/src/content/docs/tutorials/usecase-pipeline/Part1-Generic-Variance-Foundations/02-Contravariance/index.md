---
title: "반공변성 (in)"
---

## 개요

**반공변성(Contravariance)은** 제네릭 타입 파라미터가 상속 관계를 **반대 방향**으로 유지하는 성질입니다. C#에서는 `in` 키워드로 반공변성을 선언합니다.

```
Dog : Animal  (Dog은 Animal의 하위 타입)
    ↓ 반공변성 (반대 방향)
IAnimalHandler<Animal> → IAnimalHandler<Dog>  (대입 가능)
```

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

.NET에서 대표적인 반공변 타입들입니다:

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

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `in` 키워드를 사용하여 반공변 인터페이스를 선언할 수 있다
2. 반공변 대입이 가능한 이유(입력 위치 제한)를 설명할 수 있다
3. 공변성과 반공변성의 방향 차이를 구분할 수 있다
4. `Action<in T>`, `IComparer<in T>` 등의 반공변성을 이해할 수 있다

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

[← 이전: 1장 공변성 (out)](../01-Covariance/) | [다음: 3장 불변성과 제약 →](../03-Invariance-And-Constraints/)
