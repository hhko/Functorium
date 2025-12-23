# 요약

## 학습 목표

- 핵심 개념 복습
- 설계 패턴 정리
- 구현 체크리스트

---

## 핵심 개념 요약

### 소스 생성기란?

**컴파일 타임**에 C# 코드를 자동으로 생성하는 Roslyn 기반 도구입니다.

```
소스 코드 → 컴파일러 → 소스 생성기 → 추가 코드 → 최종 어셈블리
                         ↓
                    [GeneratePipeline]
                    public class UserRepository
                         ↓
                    UserRepositoryPipeline.g.cs
```

### 왜 소스 생성기인가?

| 대안 | 단점 |
|------|------|
| 수동 작성 | 반복 작업, 오류 가능성 |
| T4 템플릿 | 런타임 코드 생성, 디버깅 어려움 |
| Reflection | 런타임 성능 비용 |
| **소스 생성기** | **컴파일 타임, 타입 안전, IDE 지원** |

---

## Roslyn API 핵심

### IIncrementalGenerator

```csharp
public interface IIncrementalGenerator
{
    void Initialize(IncrementalGeneratorInitializationContext context);
}
```

### 심볼 타입

| 심볼 | 용도 |
|------|------|
| `INamedTypeSymbol` | 클래스, 인터페이스 정보 |
| `IMethodSymbol` | 메서드 시그니처 |
| `IParameterSymbol` | 파라미터 정보 |
| `IPropertySymbol` | 프로퍼티 정보 |

### ForAttributeWithMetadataName

```csharp
context.SyntaxProvider.ForAttributeWithMetadataName(
    "Namespace.GeneratePipelineAttribute",
    predicate: (node, _) => node is ClassDeclarationSyntax,
    transform: (ctx, _) => ExtractInfo(ctx))
```

---

## AdapterPipelineGenerator 설계

### 템플릿 메서드 패턴

```csharp
public abstract class IncrementalGeneratorBase<TValue> : IIncrementalGenerator
{
    // 템플릿 메서드
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = registerSourceProvider(context);
        context.RegisterSourceOutput(provider.Collect(), generate);
    }
}
```

### 전략 패턴 (IAdapter)

```csharp
// 전략 인터페이스
public interface IAdapter
{
    string RequestCategory { get; }
}

// 각 Repository는 전략 구현체
public class UserRepository : IAdapter { }
public class OrderRepository : IAdapter { }
```

### 생성 흐름

```
1. [GeneratePipeline] 속성 감지
2. IAdapter 인터페이스 확인
3. 메서드 시그니처 추출
4. Pipeline 클래스 생성
5. 관찰 가능성 코드 주입
```

---

## 관찰 가능성 코드 패턴

### 생성되는 코드 구조

```csharp
public class UserRepositoryPipeline : UserRepository
{
    // 1. 필드 (Logging, Tracing, Metrics)
    private readonly ILogger<UserRepositoryPipeline> _logger;
    private readonly IAdapterTrace _adapterTrace;
    private readonly IAdapterMetric _adapterMetric;

    // 2. LoggerMessage.Define delegate
    private static readonly Action<ILogger, ...> _logRequest = ...;

    // 3. 생성자 (의존성 주입)
    public UserRepositoryPipeline(...) { }

    // 4. 메서드 오버라이드 (관찰 가능성 주입)
    public override FinT<IO, User> GetUserAsync(int id) =>
        FinT.lift<IO, User>(
            from activityContext in IO.lift(() => CreateActivity("GetUserAsync"))
            from _ in IO.lift(() => LogRequest(...))
            from result in FinTToIO(base.GetUserAsync(id))
            from __ in IO.lift(() => LogResponse(...))
            select result
        );
}
```

---

## 유틸리티 클래스

### TypeExtractor

```csharp
// FinT<IO, T>에서 T 추출
TypeExtractor.ExtractSecondTypeParameter("FinT<IO, List<User>>")
// → "List<User>"
```

### CollectionTypeHelper

```csharp
// 컬렉션 타입 확인
CollectionTypeHelper.IsCollectionType("List<User>")  // true
CollectionTypeHelper.IsTupleType("(int, string)")    // true

// Count 표현식 생성
CollectionTypeHelper.GetCountExpression("result", "List<User>")
// → "result?.Count ?? 0"
```

### ConstructorParameterExtractor

```csharp
// 생성자 파라미터 추출
var parameters = ConstructorParameterExtractor.ExtractParameters(classSymbol);
```

### ParameterNameResolver

```csharp
// 파라미터 이름 충돌 해결
ParameterNameResolver.ResolveNames(parameters);
// "logger" → "baseLogger"
```

---

## 코드 생성 원칙

### 결정적 출력

| 원칙 | 구현 |
|------|------|
| global:: 접두사 | 네임스페이스 충돌 방지 |
| 정렬된 순서 | `.OrderBy()` 사용 |
| 타임스탬프 제외 | 재현 가능한 빌드 |

### LoggerMessage.Define 제한

| 필드 수 | 방식 |
|---------|------|
| ≤ 6개 | `LoggerMessage.Define` |
| > 6개 | `logger.LogDebug()` |

---

## 테스트 전략

### 스냅샷 테스트

```csharp
[Fact]
public Task Should_Generate_PipelineClass()
{
    string? actual = _sut.Generate(input);
    return Verify(actual);  // .verified.txt와 비교
}
```

### 테스트 카테고리

| 카테고리 | 테스트 수 |
|----------|-----------|
| 기본 생성 | 1개 |
| 기본 어댑터 | 3개 |
| 파라미터 | 8개 |
| 반환 타입 | 6개 |
| 생성자 | 4개 |
| 인터페이스 | 3개 |
| 네임스페이스 | 2개 |

---

## 구현 체크리스트

### 프로젝트 설정

- [ ] `netstandard2.0` 타겟 프레임워크
- [ ] `IsRoslynComponent = true`
- [ ] `EnforceExtendedAnalyzerRules = true`
- [ ] Microsoft.CodeAnalysis.CSharp 패키지

### 소스 생성기 구현

- [ ] `IIncrementalGenerator` 구현
- [ ] `[Generator]` 속성 적용
- [ ] `ForAttributeWithMetadataName` 사용
- [ ] 마커 Attribute 자동 생성

### 코드 생성

- [ ] `global::` 접두사 사용
- [ ] `SymbolDisplayFormat` 일관성
- [ ] 결정적 출력 보장
- [ ] 네임스페이스 처리

### 테스트

- [ ] `CSharpCompilation` 테스트 환경
- [ ] Verify 스냅샷 테스트
- [ ] 시나리오별 테스트 커버리지

---

## 핵심 파일 참조

| 파일 | 역할 |
|------|------|
| `AdapterPipelineGenerator.cs` | 메인 소스 생성기 |
| `IncrementalGeneratorBase.cs` | 템플릿 메서드 패턴 |
| `TypeExtractor.cs` | 제네릭 타입 추출 |
| `CollectionTypeHelper.cs` | 컬렉션 타입 처리 |
| `SymbolDisplayFormats.cs` | 타입 문자열 포맷 |
| `SourceGeneratorTestRunner.cs` | 테스트 유틸리티 |

---

## 다음 단계

다음 섹션에서는 추가 학습 방향을 안내합니다.

➡️ [02. 다음 단계](02-next-steps.md)
