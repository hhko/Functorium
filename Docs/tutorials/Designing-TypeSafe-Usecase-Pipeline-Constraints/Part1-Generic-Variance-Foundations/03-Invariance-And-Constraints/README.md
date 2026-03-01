# 3장: 불변성과 제약

## 개요

**불변성(Invariance)은** 제네릭 타입 파라미터가 상속 관계를 **유지하지 않는** 성질입니다. `out`이나 `in` 키워드 없이 선언된 제네릭 타입은 불변입니다.

```
Dog : Animal  (Dog은 Animal의 하위 타입)
    ✗ 불변 (대입 불가)
List<Dog> → List<Animal>  (컴파일 에러!)
```

## 핵심 개념

### 1. List\<T\>는 불변

`List<T>`는 `T`를 입력(Add)과 출력(인덱서) 양쪽에서 사용하므로, `out`이나 `in`을 선언할 수 없습니다.

```csharp
// 컴파일 에러! List<T>는 불변
// List<Animal> animals = new List<Dog>();

// 하지만 IEnumerable<out T>로는 가능 (공변)
List<Dog> dogs = [new Dog("Buddy")];
IEnumerable<Animal> animals = dogs;  // OK
```

### 2. sealed struct의 제약 한계

LanguageExt의 `Fin<T>`는 **sealed struct**입니다. C#에서 sealed struct는 `where` 제약 조건으로 사용할 수 없습니다.

```csharp
// 이것은 불가능합니다!
// where TResponse : Fin<T>  // 컴파일 에러!

// Fin<T>는 직접 파라미터 타입으로만 사용 가능
public static string ProcessFin(Fin<string> fin) =>
    fin.Match(
        Succ: value => $"Success: {value}",
        Fail: error => $"Fail: {error}");
```

### 3. 인터페이스 제약으로 우회

sealed struct 제약의 한계를 **인터페이스**로 우회할 수 있습니다. 인터페이스는 `where` 제약 조건으로 사용 가능합니다.

```csharp
public interface IResult
{
    bool IsSucc { get; }
    bool IsFail { get; }
}

// 인터페이스 제약은 가능
public static string ProcessResult<T>(T result) where T : IResult
{
    return result.IsSucc ? "Success" : "Fail";
}
```

이 패턴은 이후 장에서 `IFinResponse` 인터페이스 계층을 설계할 때의 핵심 아이디어가 됩니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `List<T>`가 불변인 이유를 설명할 수 있다
2. sealed struct가 `where` 제약으로 사용 불가능한 이유를 이해할 수 있다
3. 인터페이스 제약으로 sealed struct의 한계를 우회하는 방법을 알 수 있다
4. 공변/반공변/불변의 차이를 종합적으로 비교할 수 있다

## 프로젝트 구조

```
03-Invariance-And-Constraints/
├── InvarianceAndConstraints/
│   ├── InvarianceAndConstraints.csproj
│   ├── InvarianceExamples.cs
│   └── Program.cs
├── InvarianceAndConstraints.Tests.Unit/
│   ├── InvarianceAndConstraints.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── InvarianceAndConstraintsTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project InvarianceAndConstraints

# 테스트 실행
dotnet test --project InvarianceAndConstraints.Tests.Unit
```

---

[← 이전: 2장 반공변성 (in)](../02-Contravariance/README.md) | [다음: 4장 인터페이스 분리와 변성 조합 →](../04-Interface-Segregation-And-Variance/README.md)
