# 부록 C: 테스트 시나리오 카탈로그

## 테스트 시나리오 목록 (27개)

### 1. 기본 생성 테스트 (1개)

| # | 테스트 이름 | 검증 내용 |
|---|------------|----------|
| 1 | `ShouldGenerate_GeneratePipelineAttribute` | Attribute 자동 생성 |

---

### 2. 기본 어댑터 시나리오 (3개)

| # | 테스트 이름 | 검증 내용 |
|---|------------|----------|
| 2 | `ShouldGenerate_PipelineClass_WithSingleMethod` | 단일 메서드 처리 |
| 3 | `ShouldGenerate_PipelineClass_WithMultipleMethods` | 다중 메서드 처리 |
| 4 | `ShouldNotGenerate_PipelineClass_WhenNoMethods` | 메서드 없을 때 미생성 |

---

### 3. 파라미터 시나리오 (8개)

| # | 테스트 이름 | 검증 내용 |
|---|------------|----------|
| 5 | `ShouldGenerate_LoggerMessageDefine_WithZeroParameters` | 0개 파라미터 (Define) |
| 6 | `ShouldGenerate_LoggerMessageDefine_WithTwoParameters` | 2개 파라미터 (Define) |
| 7 | `ShouldGenerate_LogDebugFallback_WithThreeParameters` | 3개 파라미터 (폴백) |
| 8 | `ShouldGenerate_CollectionCountFields_WithCollectionParameters` | 컬렉션 Count 필드 |
| 9 | `ShouldGenerate_PipelineClass_WithNullableParameters` | Nullable 파라미터 |
| 10 | `ShouldGenerate_PipelineClass_WithTupleInputParameter` | 튜플 입력 파라미터 |
| 11 | `ShouldGenerate_PipelineClass_WithTupleInputContainingCollection` | 컬렉션 포함 튜플 |
| 12 | `ShouldGenerate_PipelineClass_WithTupleInputContainingArray` | 배열 포함 튜플 |

---

### 4. 반환 타입 시나리오 (6개)

| # | 테스트 이름 | 검증 내용 |
|---|------------|----------|
| 13 | `ShouldGenerate_PipelineClass_WithSimpleReturnType` | int, string, bool |
| 14 | `ShouldGenerate_PipelineClass_WithCollectionReturnType` | List<T>, T[] |
| 15 | `ShouldGenerate_PipelineClass_WithComplexGenericReturnType` | Dictionary<K, List<V>> |
| 16 | `ShouldGenerate_PipelineClass_WithSimpleTupleReturnType` | (int, string) |
| 17 | `ShouldGenerate_PipelineClass_WithTupleContainingCollection` | (int, List<T>) |
| 18 | `ShouldGenerate_PipelineClass_WithTupleContainingArray` | (string, int[]) |

---

### 5. 생성자 시나리오 (4개)

| # | 테스트 이름 | 검증 내용 |
|---|------------|----------|
| 19 | `ShouldGenerate_PipelineClass_WithPrimaryConstructor` | C# 12 Primary Constructor |
| 20 | `ShouldGenerate_PipelineClass_WithMultipleConstructors` | 다중 생성자 선택 |
| 21 | `ShouldGenerate_PipelineClass_WithParameterNameConflict` | logger → baseLogger |
| 22 | `ShouldGenerate_PipelineClass_WithBaseClassConstructor` | 부모 생성자 전달 |

---

### 6. 인터페이스 시나리오 (3개)

| # | 테스트 이름 | 검증 내용 |
|---|------------|----------|
| 23 | `ShouldGenerate_PipelineClass_WithDirectIAdapterImplementation` | IAdapter 직접 구현 |
| 24 | `ShouldGenerate_PipelineClass_WithInheritedIAdapterInterface` | IAdapter 상속 인터페이스 |
| 25 | `ShouldGenerate_PipelineClass_WithMultipleInterfaces` | IAdapter + IDisposable |

---

### 7. 네임스페이스 시나리오 (2개)

| # | 테스트 이름 | 검증 내용 |
|---|------------|----------|
| 26 | `ShouldGenerate_PipelineClass_WithSimpleNamespace` | MyApp |
| 27 | `ShouldGenerate_PipelineClass_WithDeepNamespace` | Company.Domain.Adapters |

---

## 카테고리별 요약

```
총 27개 테스트

┌─────────────────────────┬──────┐
│ 카테고리                 │ 수   │
├─────────────────────────┼──────┤
│ 1. 기본 생성             │  1   │
│ 2. 기본 어댑터           │  3   │
│ 3. 파라미터              │  8   │
│ 4. 반환 타입             │  6   │
│ 5. 생성자                │  4   │
│ 6. 인터페이스            │  3   │
│ 7. 네임스페이스          │  2   │
├─────────────────────────┼──────┤
│ 합계                     │ 27   │
└─────────────────────────┴──────┘
```

---

## 테스트 입력 패턴

### 최소 구조

```csharp
string input = """
    using Functorium.Adapters.SourceGenerator;
    using Functorium.Applications.Observabilities;
    using LanguageExt;

    namespace TestNamespace;

    public interface ITestAdapter : IAdapter
    {
        FinT<IO, ReturnType> MethodName(ParamType param);
    }

    [GeneratePipeline]
    public class TestAdapter : ITestAdapter
    {
        public string RequestCategory => "Test";
        public virtual FinT<IO, ReturnType> MethodName(ParamType param) => ...;
    }
    """;
```

### 파라미터 시나리오

```csharp
// 0개 파라미터
FinT<IO, int> GetValue()

// 2개 파라미터 (LoggerMessage.Define 경계)
FinT<IO, string> GetData(int id, string name)

// 3개 파라미터 (폴백)
FinT<IO, string> GetData(int id, string name, bool flag)

// 컬렉션 파라미터
FinT<IO, int> ProcessItems(List<string> items)

// Nullable 파라미터
FinT<IO, string> GetData(int? id, string? name)

// 튜플 파라미터
FinT<IO, string> ProcessUser((int Id, string Name) user)
```

### 반환 타입 시나리오

```csharp
// 단순 타입
FinT<IO, int> GetNumber()
FinT<IO, string> GetText()
FinT<IO, bool> GetFlag()

// 컬렉션
FinT<IO, List<User>> GetUsers()
FinT<IO, string[]> GetNames()

// 복잡한 제네릭
FinT<IO, Dictionary<string, List<int>>> GetComplexData()

// 튜플
FinT<IO, (int Id, string Name)> GetUserInfo()
FinT<IO, (int Id, List<string> Tags)> GetUserWithTags()
```

### 생성자 시나리오

```csharp
// Primary Constructor
public class TestAdapter(string connectionString) : IAdapter { }

// 다중 생성자
public class TestAdapter : IAdapter
{
    public TestAdapter() { }
    public TestAdapter(string connStr) { }
    public TestAdapter(string connStr, int timeout) { }  // 선택됨
}

// 이름 충돌
public class TestAdapter(ILogger<TestAdapter> logger) : IAdapter { }
// → baseLogger로 변환
```

---

## .verified.txt 파일 목록

```
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_GeneratePipelineAttribute.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithSingleMethod.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithMultipleMethods.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldNotGenerate_PipelineClass_WhenNoMethods.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_LoggerMessageDefine_WithZeroParameters.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_LoggerMessageDefine_WithTwoParameters.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_LogDebugFallback_WithThreeParameters.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_CollectionCountFields_WithCollectionParameters.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithNullableParameters.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleInputParameter.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleInputContainingCollection.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleInputContainingArray.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithSimpleReturnType.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithCollectionReturnType.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithComplexGenericReturnType.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithSimpleTupleReturnType.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleContainingCollection.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithTupleContainingArray.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithPrimaryConstructor.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithMultipleConstructors.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithParameterNameConflict.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithBaseClassConstructor.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithDirectIAdapterImplementation.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithInheritedIAdapterInterface.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithMultipleInterfaces.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithSimpleNamespace.verified.txt
AdapterPipelineGeneratorTests.AdapterPipelineGenerator_ShouldGenerate_PipelineClass_WithDeepNamespace.verified.txt
```

---

➡️ [부록 D: 트러블슈팅](D-troubleshooting.md)
