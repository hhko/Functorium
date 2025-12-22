# 테스트 시나리오

## 학습 목표

- 7개 테스트 카테고리 이해
- 각 시나리오별 테스트 케이스
- 테스트 설계 원칙

---

## 테스트 카테고리 개요

AdapterPipelineGenerator는 27개의 테스트 시나리오를 7개 카테고리로 구성합니다.

| 카테고리 | 테스트 수 | 검증 내용 |
|----------|-----------|----------|
| 1. 기본 생성 | 1개 | Attribute 생성 |
| 2. 기본 어댑터 | 3개 | 클래스 생성 |
| 3. 파라미터 | 8개 | 입력 파라미터 처리 |
| 4. 반환 타입 | 6개 | 출력 타입 처리 |
| 5. 생성자 | 4개 | 생성자 파라미터 |
| 6. 인터페이스 | 3개 | IAdapter 구현 |
| 7. 네임스페이스 | 2개 | 네임스페이스 처리 |

---

## 1. 기본 생성 테스트

### GeneratePipelineAttribute 자동 생성

```csharp
/// <summary>
/// 소스 생성기가 [GeneratePipeline] Attribute를 자동으로 생성하는지 확인합니다.
/// </summary>
[Fact]
public Task AdapterPipelineGenerator_ShouldGenerate_GeneratePipelineAttribute()
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

### 단일 메서드 어댑터

```csharp
/// <summary>
/// IAdapter를 구현하고 단일 메서드를 가진 어댑터에 대해
/// 파이프라인 클래스가 생성되는지 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithSingleMethod()
{
    string input = """
        [GeneratePipeline]
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
        [GeneratePipeline]
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
/// IAdapter만 구현하고 메서드가 없는 경우 파이프라인이 생성되지 않아야 합니다.
/// </summary>
[Fact]
public Task Should_NotGenerate_PipelineClass_WhenNoMethods()
{
    string input = """
        [GeneratePipeline]
        public class EmptyAdapter : IAdapter
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
        [GeneratePipeline]
        public class ZeroParamAdapter : IAdapter
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
        [GeneratePipeline]
        public class TwoParamAdapter : IAdapter
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
        [GeneratePipeline]
        public class ThreeParamAdapter : IAdapter
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
        [GeneratePipeline]
        public class CollectionParamAdapter : IAdapter
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
        [GeneratePipeline]
        public class TupleAdapter : IAdapter
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

### 단순 반환 타입

```csharp
/// <summary>
/// FinT<IO, int>, FinT<IO, string> 등 단순 타입 추출을 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithSimpleReturnType()
{
    string input = """
        [GeneratePipeline]
        public class SimpleAdapter : IAdapter
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
        [GeneratePipeline]
        public class CollectionAdapter : IAdapter
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
        [GeneratePipeline]
        public class ComplexAdapter : IAdapter
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
        [GeneratePipeline]
        public class TupleAdapter : IAdapter
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

### Primary Constructor

```csharp
/// <summary>
/// C# 12+ Primary Constructor를 가진 클래스 처리를 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithPrimaryConstructor()
{
    string input = """
        [GeneratePipeline]
        public class PrimaryCtorAdapter(string connectionString) : IAdapter
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
        [GeneratePipeline]
        public class MultiCtorAdapter : IAdapter
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
        [GeneratePipeline]
        public class ConflictAdapter(ILogger<ConflictAdapter> logger) : IAdapter
        {
            // logger → baseLogger로 변환 필요
        }
        """;
    // ...
}
```

---

## 6. 인터페이스 시나리오

### IAdapter 직접 구현

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithDirectIAdapterImplementation()
{
    string input = """
        [GeneratePipeline]
        public class DirectAdapter : IAdapter
        {
            public virtual FinT<IO, int> GetValue() => ...;
        }
        """;
    // ...
}
```

### IAdapter 상속 인터페이스

```csharp
/// <summary>
/// IUserRepository : IAdapter 형태의 상속 인터페이스를 확인합니다.
/// </summary>
[Fact]
public Task Should_Generate_PipelineClass_WithInheritedIAdapterInterface()
{
    string input = """
        public interface IUserRepository : IAdapter
        {
            FinT<IO, string> GetUserById(int id);
        }

        [GeneratePipeline]
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
        [GeneratePipeline]
        public class MultiInterfaceAdapter : IAdapter, IDisposable
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

### 단순 네임스페이스

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithSimpleNamespace()
{
    string input = """
        namespace MyApp;

        [GeneratePipeline]
        public class SimpleAdapter : IAdapter { ... }
        """;
    // 생성 파일: MyApp.SimpleAdapterPipeline.g.cs
}
```

### 깊은 네임스페이스

```csharp
[Fact]
public Task Should_Generate_PipelineClass_WithDeepNamespace()
{
    string input = """
        namespace Company.Domain.Adapters.Infrastructure.Repositories;

        [GeneratePipeline]
        public class DeepAdapter : IAdapter { ... }
        """;
    // 생성 파일: Company.Domain.Adapters.Infrastructure.Repositories.DeepAdapterPipeline.g.cs
}
```

---

## 테스트 설계 원칙

### 1. 하나의 시나리오만 테스트

```csharp
// ✅ 좋은 예
Should_Generate_Count_ForListParameter()
Should_Generate_Length_ForArrayParameter()

// ❌ 나쁜 예
Should_Generate_CountAndLength_ForCollections()
```

### 2. 경계값 테스트

```csharp
// LoggerMessage.Define 경계 (6개)
WithTwoParameters()    // 6개 → Define
WithThreeParameters()  // 7개 → LogDebug
```

### 3. 예외 상황 테스트

```csharp
// 메서드 없는 어댑터
Should_NotGenerate_PipelineClass_WhenNoMethods()

// 튜플 (컬렉션 미인식)
Should_NotGenerate_Count_ForTupleContainingCollection()
```

### 4. 명확한 테스트 이름

```
{Generator}_{Should/ShouldNot}_{Action}_{Condition}

예시:
AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithPrimaryConstructor
AdapterPipelineGenerator_ShouldNotGenerate_Count_ForTupleReturnType
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

---

## 요약

| 원칙 | 적용 |
|------|------|
| 단일 책임 | 테스트당 하나의 시나리오 |
| 경계값 | LoggerMessage.Define 6개 제한 |
| 예외 처리 | 빈 클래스, 튜플 등 |
| 명명 규칙 | Should_{Action}_{Condition} |

---

## 다음 단계

다음 장에서는 학습 내용을 요약합니다.

➡️ [09장. 결론](../09-conclusion/)
