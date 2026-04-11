---
title: "Covariance (out)"
---

## 개요

`Dog`은 `Animal`의 하위 타입입니다. 그렇다면 `IAnimalShelter<Dog>`은 `IAnimalShelter<Animal>`에 대입할 수 있을까요? 답은 인터페이스의 **변성 선언**에 달려 있습니다.

**공변성(Covariance)은** 제네릭 타입 파라미터가 상속 관계를 **같은 방향**으로 유지하는 성질입니다. C#에서는 `out` 키워드로 공변성을 선언합니다.

```
Dog : Animal  (Dog은 Animal의 하위 타입)
    ↓ 공변성 (같은 방향)
IAnimalShelter<Dog> → IAnimalShelter<Animal>  (대입 가능)
```

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `out` 키워드를 사용하여 공변 인터페이스를 선언할 수 있습니다
2. 공변 대입이 가능한 이유(출력 위치 제한)를 설명할 수 있습니다
3. `IEnumerable<out T>`가 공변인 이유를 이해할 수 있습니다
4. 공변성이 읽기 전용 접근과 어떻게 연결되는지 설명할 수 있습니다

## 핵심 개념

### 1. `out` 키워드

`out T`는 타입 파라미터 `T`가 **출력 위치에서만** 사용됨을 컴파일러에 선언합니다.

```csharp
public interface IAnimalShelter<out T> where T : Animal
{
    T GetAnimal(int index);       // OK: T가 반환 타입 (출력 위치)
    IEnumerable<T> GetAll();      // OK: T가 반환 타입 (출력 위치)
    // void Add(T animal);        // 컴파일 에러! T가 파라미터 (입력 위치)
}
```

### 2. 공변 대입

`Dog`이 `Animal`의 하위 타입이면, `IAnimalShelter<Dog>`을 `IAnimalShelter<Animal>`에 대입할 수 있습니다.

```csharp
var dogShelter = new DogShelter();
dogShelter.Add(new Dog("Buddy", "Golden Retriever"));

// 공변성: IAnimalShelter<Dog> → IAnimalShelter<Animal> 대입 가능
IAnimalShelter<Animal> animalShelter = dogShelter;
```

### 3. .NET의 공변 인터페이스

.NET에서 가장 대표적인 공변 인터페이스는 `IEnumerable<out T>`입니다.

```csharp
IEnumerable<Dog> dogs = new List<Dog> { new("Buddy", "Golden Retriever") };
IEnumerable<Animal> animals = dogs;  // IEnumerable<out T>이므로 OK
```

### 4. 왜 공변성이 유용한가?

공변성은 **읽기 전용** 접근을 보장합니다. `out T`로 선언하면 T 타입의 값을 **꺼내기만** 할 수 있으므로, 하위 타입의 컬렉션을 상위 타입으로 안전하게 참조할 수 있습니다.

## FAQ

### Q1: `out` 키워드를 붙이면 왜 입력 위치에서 `T`를 사용할 수 없나요?
**A**: `out T`는 "이 타입 파라미터로 값을 꺼내기만 한다"는 약속입니다. 만약 `void Add(T animal)` 같은 입력 위치를 허용하면, `IAnimalShelter<Animal>` 참조를 통해 `Cat`을 `DogShelter`에 추가하는 타입 안전성 위반이 발생할 수 있습니다. 컴파일러가 이를 원천적으로 차단합니다.

### Q2: `IEnumerable<out T>`가 공변인데, `List<T>`는 왜 공변이 아닌가요?
**A**: `List<T>`는 `Add(T item)` 메서드로 T를 입력 위치에서도 사용하기 때문입니다. 입력과 출력 양쪽에서 T를 사용하면 `out`도 `in`도 선언할 수 없어 **불변(Invariant)** 타입이 됩니다. `IEnumerable<T>`는 읽기 전용이므로 `out`을 선언할 수 있습니다.

### Q3: 공변성은 이 튜토리얼의 Pipeline 설계에서 어떻게 활용되나요?
**A**: Part 3에서 설계하는 `IFinResponse<out A>` 인터페이스가 공변성을 활용합니다. `out A` 덕분에 `IFinResponse<string>`을 `IFinResponse<object>`에 대입할 수 있어, Pipeline에서 다양한 응답 타입을 유연하게 처리할 수 있습니다.

## 프로젝트 구조

```
01-Covariance/
├── Covariance/
│   ├── Covariance.csproj
│   ├── Animal.cs
│   ├── IAnimalShelter.cs
│   └── Program.cs
├── Covariance.Tests.Unit/
│   ├── Covariance.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── CovarianceTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project Covariance

# 테스트 실행
dotnet test --project Covariance.Tests.Unit
```

---

공변성이 "꺼내기 전용"이라면, 반대로 "받기 전용"인 경우에는 어떤 변성이 적용될까요? `in` 키워드와 반공변성을 학습합니다.

→ [1.2장: 반공변성 (in)](../02-Contravariance/)

