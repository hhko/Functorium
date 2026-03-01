# 1장: 공변성 (out)

## 개요

**공변성(Covariance)은** 제네릭 타입 파라미터가 상속 관계를 **같은 방향**으로 유지하는 성질입니다. C#에서는 `out` 키워드로 공변성을 선언합니다.

```
Dog : Animal  (Dog은 Animal의 하위 타입)
    ↓ 공변성 (같은 방향)
IAnimalShelter<Dog> → IAnimalShelter<Animal>  (대입 가능)
```

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

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `out` 키워드를 사용하여 공변 인터페이스를 선언할 수 있다
2. 공변 대입이 가능한 이유(출력 위치 제한)를 설명할 수 있다
3. `IEnumerable<out T>`가 공변인 이유를 이해할 수 있다
4. 공변성이 읽기 전용 접근과 어떻게 연결되는지 설명할 수 있다

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

[← Part 0: 서론](../../Part0-Introduction/01-why-this-tutorial.md) | [다음: 2장 반공변성 (in) →](../02-Contravariance/README.md)
