---
title: "Test Scenario"
---

## 개요

소스 생성기의 신뢰성은 곧 생성된 코드의 신뢰성입니다. ObservablePortGenerator가 잘못된 코드를 생성하면 런타임이 아닌 컴파일 타임에 오류가 발생하므로, 사용자는 원인을 파악하기 어렵습니다. 이를 방지하기 위해 31개의 테스트 시나리오를 8개 카테고리로 체계적으로 구성합니다. 각 카테고리는 "무엇을 테스트하는가"뿐만 아니라 "왜 이 시나리오가 필요한가"를 기준으로 설계되었습니다.

## 테스트 설계 원칙

테스트 시나리오를 살펴보기 전에, ObservablePortGenerator 테스트가 따르는 네 가지 설계 원칙을 먼저 이해하면 각 테스트의 의도를 파악하기 쉽습니다.

**단일 시나리오 원칙.** 한 테스트는 하나의 기능만 검증합니다. `Count`와 `Length`를 하나의 테스트에 묶지 않고 분리하면, 실패 시 원인을 즉시 특정할 수 있습니다.

**경계값 테스트.** `LoggerMessage.Define`의 6개 파라미터 제한처럼, 동작이 분기되는 임계값 양쪽을 모두 테스트합니다. 2개 파라미터(총 6개, Define 사용)와 3개 파라미터(총 7개, LogDebug 폴백)를 별도 시나리오로 검증합니다.

**예외 상황 테스트.** 메서드가 없는 어댑터, 튜플 내부의 컬렉션처럼 "생성하지 않아야 하는" 경우도 명시적으로 검증합니다. `ShouldNotContain` assertion으로 의도하지 않은 코드가 생성되지 않음을 확인합니다.

**명확한 명명 규칙.** `Should_{Action}_{Condition}` 패턴을 따라, 테스트 이름만으로 검증 대상과 조건을 알 수 있습니다.

## 학습 목표

### 핵심 학습 목표
1. **8개 테스트 카테고리 이해**
   - 기본 생성부터 진단까지, 각 카테고리의 검증 범위
2. **각 시나리오별 테스트 케이스**
   - 정상 경로와 예외 경로를 포함하는 31개 시나리오
3. **테스트 설계 원칙의 실제 적용**
   - 위 원칙이 각 테스트에 어떻게 반영되어 있는지 확인

---

## 테스트 카테고리 개요

ObservablePortGenerator는 31개의 테스트 시나리오를 8개 카테고리로 구성합니다.

| 카테고리 | 테스트 수 | 검증 내용 |
|----------|-----------|----------|
| 1. 기본 생성 | 1개 | Attribute 생성 |
| 2. 기본 어댑터 | 3개 | 클래스 생성 |
| 3. 파라미터 | 8개 | 입력 파라미터 처리 |
| 4. 반환 타입 | 6개 | 출력 타입 처리 |
| 5. 생성자 | 4개 | 생성자 파라미터 |
| 6. 인터페이스 | 3개 | IObservablePort 구현 |
| 7. 네임스페이스 | 2개 | 네임스페이스 처리 |
| 8. 진단 | 4개 | Diagnostic 보고 |

---

## 1. 기본 생성 테스트

소스 생성기가 올바르게 등록되었는지 확인하는 가장 기본적인 테스트입니다. `[GenerateObservablePort]` Attribute 자체가 소스 생성기에 의해 자동 제공되므로, 빈 입력에서도 Attribute 코드가 생성되어야 합니다.

### GenerateObservablePortAttribute 자동 생성

```csharp
/// <summary>
/// 소스 생성기가 [GenerateObservablePort] Attribute를 자동으로 생성하는지 확인합니다.
/// </summary>
[Fact]
public Task ObservablePortGenerator_ShouldGenerate_GenerateObservablePortAttribute()
{
    // 빈 입력으로도 Attribute 코드가 생성됨
    string input = string.Empty;

    string? actual = _sut.Generate(input);

    return Verify(actual);
}
```

**검증 내용**: 소스 생성기가 마커 Attribute를 자동으로 제공

---

## 2. 기본 어댑터 시나리오

이 카테고리는 ObservablePortGenerator의 핵심 기능인 "어댑터 클래스로부터 Observable 클래스 생성"을 검증합니다. 단일 메서드, 다중 메서드, 그리고 메서드가 없는 경우를 모두 테스트하여 생성기의 기본 동작 범위를 확인합니다.

### 단일 메서드 어댑터

```csharp
/// <summary>
/// IPort를 구현하고 단일 메서드를 가진 어댑터에 대해
/// 파이프라인 클래스가 생성되는지 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithSingleMethod()
{
    string input = """
        [GenerateObservablePort]
        public class TestAdapter : ITestAdapter
        {
            public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

### 다중 메서드 어댑터

```csharp
/// <summary>
/// 여러 메서드를 가진 어댑터에 대해 모든 메서드가 오버라이드되는지 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithMultipleMethods()
{
    string input = """
        [GenerateObservablePort]
        public class MultiMethodAdapter : IMultiMethodAdapter
        {
            public virtual FinT<IO, int> GetValue() => ...;
            public virtual FinT<IO, string> GetName() => ...;
            public virtual FinT<IO, bool> IsValid() => ...;
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

### 메서드 없는 어댑터

```csharp
/// <summary>
/// IPort만 구현하고 메서드가 없는 경우 파이프라인이 생성되지 않아야 합니다.
/// </summary>
[Fact]
public Task Should_NotGenerate_PipelineClass_WhenNoMethods()
{
    string input = """
        [GenerateObservablePort]
        public class EmptyAdapter : IObservablePort
        {
            public string RequestCategory => "Test";
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

---

## 3. 파라미터 시나리오

메서드 파라미터는 로깅 필드 수에 직접 영향을 미치므로, `LoggerMessage.Define`의 6개 제한과 맞물려 가장 많은 테스트 케이스가 필요한 카테고리입니다. 경계값(2개 vs 3개 파라미터)과 컬렉션 파라미터의 Count 필드 추가, 그리고 튜플처럼 컬렉션으로 오인될 수 있는 타입의 예외 처리를 검증합니다.

### 파라미터 수와 LoggerMessage.Define

| 파라미터 수 | 총 필드 | 사용 방식 |
|------------|---------|-----------|
| 0개 | 4개 | LoggerMessage.Define |
| 2개 | 6개 | LoggerMessage.Define |
| 3개 | 7개 | logger.LogDebug() |

```csharp
// 0개 파라미터 테스트
[Fact]
public Task Should_Generate_LoggerMessageDefine_WithZeroParameters()
{
    string input = """
        [GenerateObservablePort]
        public class ZeroParamAdapter : IObservablePort
        {
            public virtual FinT<IO, int> GetValue() => ...;
        }
        """;
    // ...
}

// 2개 파라미터 테스트 (경계값)
[Fact]
public Task Should_Generate_LoggerMessageDefine_WithTwoParameters()
{
    string input = """
        [GenerateObservablePort]
        public class TwoParamAdapter : IObservablePort
        {
            public virtual FinT<IO, string> GetData(int id, string name) => ...;
        }
        """;
    // ...
}

// 3개 파라미터 테스트 (폴백)
[Fact]
public Task Should_Generate_LogDebugFallback_WithThreeParameters()
{
    string input = """
        [GenerateObservablePort]
        public class ThreeParamAdapter : IObservablePort
        {
            public virtual FinT<IO, string> GetData(int id, string name, bool flag) => ...;
        }
        """;
    // ...
}
```

### 컬렉션 파라미터

```csharp
/// <summary>
/// 컬렉션 타입 파라미터에 대해 Count 필드가 추가되는지 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_CollectionCountFields()
{
    string input = """
        [GenerateObservablePort]
        public class CollectionParamAdapter : IObservablePort
        {
            public virtual FinT<IO, int> ProcessItems(List<string> items) => ...;
        }
        """;
    // ...
}
```

### 튜플 파라미터 (컬렉션 미인식)

```csharp
/// <summary>
/// 튜플은 컬렉션으로 인식되지 않으므로 Count 필드가 생성되지 않습니다.
/// </summary>
[Fact]
public Task Should_NotGenerate_Count_ForTupleParameter()
{
    string input = """
        [GenerateObservablePort]
        public class TupleAdapter : IObservablePort
        {
            // 튜플 내부에 List가 있어도 Count 미생성
            public virtual FinT<IO, int> Process((int Id, List<string> Tags) user) => ...;
        }
        """;
    // ...
}
```

---

## 4. 반환 타입 시나리오

반환 타입은 `TypeExtractor`의 제네릭 파싱과 `CollectionTypeHelper`의 컬렉션 감지가 동시에 적용되는 영역입니다. 단순 타입에서 중첩 제네릭, 배열, 튜플까지 다양한 패턴을 테스트하여 타입 추출과 Count/Length 생성이 올바르게 동작하는지 확인합니다.

### 단순 반환 타입

```csharp
/// <summary>
/// FinT<IO, int>, FinT<IO, string> 등 단순 타입 추출을 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithSimpleReturnType()
{
    string input = """
        [GenerateObservablePort]
        public class SimpleAdapter : IObservablePort
        {
            public virtual FinT<IO, int> GetNumber() => ...;
            public virtual FinT<IO, string> GetText() => ...;
            public virtual FinT<IO, bool> GetFlag() => ...;
        }
        """;
    // ...
}
```

### 컬렉션 반환 타입

```csharp
/// <summary>
/// List<T>, T[] 반환 타입에서 Count/Length 필드가 생성되는지 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithCollectionReturnType()
{
    string input = """
        [GenerateObservablePort]
        public class CollectionAdapter : IObservablePort
        {
            public virtual FinT<IO, List<User>> GetUsers() => ...;
            public virtual FinT<IO, string[]> GetNames() => ...;
        }
        """;
    // ...
}
```

### 복잡한 제네릭

```csharp
/// <summary>
/// Dictionary<K, List<V>> 같은 중첩 제네릭 추출을 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithComplexGenericReturnType()
{
    string input = """
        [GenerateObservablePort]
        public class ComplexAdapter : IObservablePort
        {
            public virtual FinT<IO, Dictionary<string, List<int>>> GetComplexData() => ...;
        }
        """;
    // ...
}
```

### 튜플 반환 타입

```csharp
/// <summary>
/// (int Id, string Name) 튜플 반환에서 Count가 생성되지 않음을 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithTupleReturnType()
{
    string input = """
        [GenerateObservablePort]
        public class TupleAdapter : IObservablePort
        {
            public virtual FinT<IO, (int Id, string Name)> GetUserInfo() => ...;
            public virtual FinT<IO, (int Id, List<string> Tags)> GetUserWithTags() => ...;
        }
        """;
    // ...
}
```

---

## 5. 생성자 시나리오

생성자 처리는 `ConstructorParameterExtractor`와 `ParameterNameResolver`의 협력으로 이루어집니다. Primary Constructor, 다중 생성자 중 최적 선택, 그리고 `logger`와 같은 예약 이름 충돌 해결을 각각 독립적으로 테스트합니다.

### Primary Constructor

```csharp
/// <summary>
/// C# 12+ Primary Constructor를 가진 클래스 처리를 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithPrimaryConstructor()
{
    string input = """
        [GenerateObservablePort]
        public class PrimaryCtorAdapter(string connectionString) : IObservablePort
        {
            public virtual FinT<IO, string> GetConnectionString() => ...;
        }
        """;
    // ...
}
```

### 다중 생성자

```csharp
/// <summary>
/// 여러 생성자 중 가장 많은 파라미터를 가진 것이 선택되는지 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithMultipleConstructors()
{
    string input = """
        [GenerateObservablePort]
        public class MultiCtorAdapter : IObservablePort
        {
            public MultiCtorAdapter() { }
            public MultiCtorAdapter(string connStr) { }
            public MultiCtorAdapter(string connStr, int timeout) { }  // 선택됨
        }
        """;
    // ...
}
```

### 파라미터명 충돌

```csharp
/// <summary>
/// logger 파라미터가 baseLogger로 리네임되는지 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithParameterNameConflict()
{
    string input = """
        [GenerateObservablePort]
        public class ConflictAdapter(ILogger<ConflictAdapter> logger) : IObservablePort
        {
            // logger → baseLogger로 변환 필요
        }
        """;
    // ...
}
```

---

## 6. 인터페이스 시나리오

ObservablePortGenerator는 `IObservablePort`를 구현하는 클래스를 대상으로 동작합니다. 직접 구현, 상속 인터페이스를 통한 간접 구현, 그리고 여러 인터페이스를 동시에 구현하는 경우 모두 올바르게 감지되어야 합니다.

### IObservablePort 직접 구현

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithDirectIPortImplementation()
{
    string input = """
        [GenerateObservablePort]
        public class DirectAdapter : IObservablePort
        {
            public virtual FinT<IO, int> GetValue() => ...;
        }
        """;
    // ...
}
```

### IObservablePort 상속 인터페이스

```csharp
/// <summary>
/// IUserRepository : IObservablePort 형태의 상속 인터페이스를 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithInheritedIPortInterface()
{
    string input = """
        public interface IUserRepository : IObservablePort
        {
            FinT<IO, string> GetUserById(int id);
        }

        [GenerateObservablePort]
        public class UserRepository : IUserRepository { ... }
        """;
    // ...
}
```

### 다중 인터페이스

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithMultipleInterfaces()
{
    string input = """
        [GenerateObservablePort]
        public class MultiInterfaceAdapter : IObservablePort, IDisposable
        {
            public virtual FinT<IO, int> GetValue() => ...;
            public void Dispose() { }
        }
        """;
    // ...
}
```

---

## 7. 네임스페이스 시나리오

생성된 코드는 원본 클래스와 동일한 네임스페이스에 배치되어야 합니다. 단순 네임스페이스와 깊은 네임스페이스 모두에서 파일명과 namespace 선언이 올바르게 생성되는지 확인합니다.

### 단순 네임스페이스

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithSimpleNamespace()
{
    string input = """
        namespace MyApp;

        [GenerateObservablePort]
        public class SimpleAdapter : IObservablePort { ... }
        """;
    // 생성 파일: MyApp.SimpleObservablePort.g.cs
}
```

### 깊은 네임스페이스

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithDeepNamespace()
{
    string input = """
        namespace Company.Domain.Adapters.Infrastructure.Repositories;

        [GenerateObservablePort]
        public class DeepAdapter : IObservablePort { ... }
        """;
    // 생성 파일: Company.Domain.Adapters.Infrastructure.Repositories.DeepObservablePort.g.cs
}
```

---

## 8. 진단 시나리오

소스 생성기는 코드를 생성하는 것뿐 아니라, 잘못된 사용 패턴을 감지하여 **Diagnostic 메시지를** 보고하는 역할도 합니다. 생성자 파라미터에 `ActivitySource`, `IMeterFactory` 등 관찰 가능성 인프라 타입이 중복 선언되면, 생성된 Observable 클래스의 생성자와 충돌합니다. 이를 컴파일 타임에 경고하는 4개 시나리오를 검증합니다.

### 중복 파라미터 타입 감지

```csharp
[Fact]
public void Should_ReportDiagnostic_WhenDuplicateParameterTypes()
{
    // ActivitySource를 이미 갖고 있는 생성자 → 진단 경고
    string input = """
        [GenerateObservablePort]
        public class DuplicateAdapter(ActivitySource activitySource) : IObservablePort { ... }
        """;
    // FUNCTORIUM001 진단 보고 검증
}
```

### 중복 MeterFactory 감지

```csharp
[Fact]
public void Should_ReportDiagnostic_WhenDuplicateMeterFactoryParameter()
{
    // IMeterFactory를 이미 갖고 있는 생성자 → 진단 경고
}
```

### 진단 위치 정확성

```csharp
[Fact]
public void Should_ReportDiagnostic_WithCorrectLocation()
{
    // 진단 메시지의 Location이 해당 클래스 선언 위치를 가리키는지 검증
}
```

### 정상 케이스 (진단 없음)

```csharp
[Fact]
public void Should_NotReportDiagnostic_WhenNoParameterDuplication()
{
    // 중복 없는 정상 생성자 → 진단 0개
}
```

---

## 테스트 커버리지

| 카테고리 | 정상 케이스 | 예외 케이스 |
|----------|------------|------------|
| 기본 생성 | 1 | - |
| 기본 어댑터 | 2 | 1 |
| 파라미터 | 6 | 2 |
| 반환 타입 | 4 | 2 |
| 생성자 | 3 | 1 |
| 인터페이스 | 3 | - |
| 네임스페이스 | 2 | - |
| 진단 | 1 | 3 |

---

## 한눈에 보는 정리

31개 테스트 시나리오는 ObservablePortGenerator의 모든 코드 생성 경로를 체계적으로 검증합니다. 각 카테고리는 독립적인 관심사를 다루며, 정상 경로뿐만 아니라 메서드 없는 어댑터, 튜플 내 컬렉션 같은 예외 경로도 포함합니다. 앞서 정의한 네 가지 설계 원칙(단일 시나리오, 경계값, 예외 상황, 명확한 명명)이 모든 테스트에 일관되게 적용되어, 소스 생성기의 변경이 기존 동작에 미치는 영향을 즉시 파악할 수 있습니다.

---

## FAQ

### Q1: 31개 테스트 시나리오에서 가장 빠뜨리기 쉬운 케이스는 무엇인가요?
**A**: 튜플 내부에 컬렉션이 포함된 경우(`FinT<IO, (int Id, List<string> Tags)>`)와 `LoggerMessage.Define`의 경계값(파라미터 2개 vs 3개)이 가장 빠뜨리기 쉽습니다. 전자는 Count를 생성하면 컴파일 오류가 발생하고, 후자는 한 개 차이로 고성능 경로와 폴백 경로가 달라지므로 양쪽 모두 테스트해야 합니다.

### Q2: `ShouldNotContain` assertion은 언제 사용하나요?
**A**: "생성하지 않아야 하는" 코드를 검증할 때 사용합니다. 예를 들어 튜플 반환 타입에서 `response.result.count` 필드가 생성되지 않아야 하거나, 메서드가 없는 어댑터에서 메서드 오버라이드가 생성되지 않아야 할 때 `actual.ShouldNotContain("response.result.count")`로 명시적으로 검증합니다. 스냅샷 테스트만으로는 "없어야 할 것이 없다"를 확인하기 어렵기 때문입니다.

### Q3: 테스트 이름에 `Should_{Action}_{Condition}` 패턴을 사용하는 이유는 무엇인가요?
**A**: 테스트가 실패했을 때 이름만으로 "무엇이 어떤 조건에서 실패했는지"를 즉시 파악할 수 있기 때문입니다. `Should_Generate_LogDebugFallback_WithThreeParameters`라는 이름은 "3개 파라미터일 때 LogDebug 폴백이 생성되어야 한다"를 명확히 전달하므로, 실패 원인 추적이 빠릅니다.

---

Part 3에서 다룬 고급 주제들(생성자, 제네릭, 컬렉션, LoggerMessage 제한, 테스트)을 모두 학습했습니다. 다음 Part에서는 다양한 실용적 예제를 통해 Source Generator 개발 절차를 학습합니다.

→ [Part 4. 개발 절차서](../../Part4-Cookbook/01-Development-Workflow/)
