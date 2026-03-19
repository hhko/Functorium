---
title: "요약"
---

## 개요

이 튜토리얼을 통해 우리는 "반복되는 관찰 가능성 보일러플레이트를 어떻게 제거할 수 있을까?"라는 질문에서 출발하여, Roslyn 기반 소스 생성기로 컴파일 타임에 Logging, Tracing, Metrics 코드를 자동 생성하는 완전한 솔루션을 구축했습니다. Syntax Tree와 Semantic Model을 탐색하는 기초부터, 템플릿 메서드 패턴으로 생성기를 설계하고, 스냅샷 테스트로 생성 결과를 검증하는 전 과정을 직접 구현했습니다. 이 요약에서는 그 여정의 핵심을 되짚어봅니다.

## 학습 목표

### 핵심 학습 목표
1. **핵심 개념 복습**
   - 소스 생성기의 동작 원리와 Roslyn API 핵심 요소를 되짚습니다
2. **설계 패턴 정리**
   - ObservablePortGenerator에 적용된 템플릿 메서드·전략 패턴을 구조적으로 정리합니다
3. **구현 체크리스트 확인**
   - 실전 프로젝트에서 소스 생성기를 도입할 때 빠뜨리기 쉬운 설정과 검증 항목을 확인합니다

---

## 핵심 개념 요약

### 소스 생성기란?

수작업으로 반복 코드를 작성하면 오류가 발생하기 쉽고, Reflection 기반 접근은 런타임 성능 비용을 수반합니다. 소스 생성기는 이 문제를 **컴파일 타임으로** 옮겨서 해결합니다. Roslyn 파이프라인에 플러그인으로 참여하여, 개발자가 작성한 소스 코드를 분석하고 추가 C# 코드를 자동으로 생성하는 도구입니다.

```
소스 코드 → 컴파일러 → 소스 생성기 → 추가 코드 → 최종 어셈블리
                         ↓
                    [GenerateObservablePort]
                    public class UserRepository
                         ↓
                    UserRepositoryObservable.g.cs
```

### 왜 소스 생성기인가?

수동 작성은 반복 작업과 오류 가능성이 높고, T4 템플릿은 런타임 코드 생성이라 디버깅이 어렵습니다. Reflection은 런타임 성능 비용을 수반합니다. **소스 생성기는** 컴파일 타임에 동작하면서 타입 안전성과 IDE 지원을 모두 갖춘 유일한 대안입니다.

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

Roslyn의 Semantic Model은 코드의 의미를 심볼로 표현합니다. `INamedTypeSymbol`로 클래스와 인터페이스 정보를 조회하고, `IMethodSymbol`로 메서드 시그니처를, `IParameterSymbol`과 `IPropertySymbol`로 파라미터와 프로퍼티 정보를 추출합니다. ObservablePortGenerator는 이 심볼들을 조합하여 원본 클래스의 구조를 완전히 파악한 뒤 관찰 가능성 코드를 생성합니다.

### ForAttributeWithMetadataName

```csharp
context.SyntaxProvider.ForAttributeWithMetadataName(
    "Namespace.GenerateObservablePortAttribute",
    predicate: (node, _) => node is ClassDeclarationSyntax,
    transform: (ctx, _) => ExtractInfo(ctx))
```

---

## ObservablePortGenerator 설계

소스 생성기의 핵심 설계 과제는 "다양한 클래스에 대해 일관된 관찰 가능성 코드를 생성하면서도, 각 클래스의 고유한 구조를 정확히 반영하는 것"입니다. 이를 위해 템플릿 메서드 패턴과 전략 패턴을 조합했습니다.

### 템플릿 메서드 패턴

```csharp
public abstract class IncrementalGeneratorBase<TValue> : IIncrementalGenerator
{
    // 템플릿 메서드
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = _registerSourceProvider(context);
        context.RegisterSourceOutput(provider.Collect(), _generate);
    }
}
```

### 전략 패턴 (IObservablePort)

```csharp
// 전략 인터페이스
public interface IObservablePort
{
    string RequestCategory { get; }
}

// 각 Repository는 전략 구현체
public class UserRepository : IObservablePort { }
public class OrderRepository : IObservablePort { }
```

### 생성 흐름

```
1. [GenerateObservablePort] 속성 감지
2. IObservablePort 인터페이스 확인
3. 메서드 시그니처 추출
4. Observable 클래스 생성
5. 관찰 가능성 코드 주입
```

---

## 관찰 가능성 코드 패턴

설계 패턴이 "어떻게 생성하는가"에 대한 답이라면, 이 섹션은 "무엇이 생성되는가"에 대한 답입니다. 생성된 Observable 클래스는 원본 클래스를 상속하면서 Logging, Tracing, Metrics 세 가지 관찰 가능성 축을 모두 포함합니다.

### 생성되는 코드 구조

```csharp
public class UserRepositoryObservable : UserRepository
{
    // 1. 필드 (Logging, Tracing, Metrics)
    private readonly ActivitySource _activitySource;
    private readonly ILogger<UserRepositoryObservable> _logger;
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _responseCounter;
    private readonly Histogram<double> _durationHistogram;

    // 2. LoggerMessage.Define delegate
    private static readonly Action<ILogger, ...> _logAdapterRequestDebug_... = ...;

    // 3. 생성자 (의존성 주입)
    public UserRepositoryObservable(
        ActivitySource activitySource,
        ILogger<UserRepositoryObservable> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions) { }

    // 4. 메서드 오버라이드 (관찰 가능성 주입)
    public new FinT<IO, User> GetUserAsync(int id) =>
        global::LanguageExt.FinT.lift<IO, User>(
            IO.lift(() => ExecuteWithSpan(
                RequestHandler,
                nameof(GetUserAsync),
                FinTToIO(base.GetUserAsync(id)),
                () => LogGetUserAsyncRequest(id),
                LogGetUserAsyncResponseSuccess,
                LogGetUserAsyncResponseFailure)));
}
```

---

## 유틸리티 클래스

소스 생성기는 다양한 타입 시그니처를 처리해야 합니다. 제네릭 타입에서 내부 타입을 추출하거나, 컬렉션 여부를 판별하거나, 생성자 파라미터 이름 충돌을 해결하는 등의 작업은 각각 전용 유틸리티 클래스로 분리하여 생성기 본체의 복잡도를 관리했습니다.

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

소스 생성기가 만드는 코드는 빌드할 때마다 동일한 결과를 내야 합니다. 비결정적 출력은 불필요한 diff를 만들고 소스 제어를 혼란스럽게 합니다.

### 결정적 출력

**결정적 출력을** 보장하기 위해 세 가지 원칙을 적용합니다. 첫째, `global::` 접두사로 네임스페이스 충돌을 방지합니다. 둘째, `.OrderBy()`를 사용하여 생성 순서를 항상 일정하게 유지합니다. 셋째, 타임스탬프를 제외하여 재현 가능한 빌드를 보장합니다.

### LoggerMessage.Define 제한

`LoggerMessage.Define`은 최대 6개의 파라미터만 지원하는 제약이 있습니다. 따라서 파라미터가 6개 이하인 경우 `LoggerMessage.Define`을 사용하고, 7개 이상인 경우 `logger.LogDebug()`로 폴백합니다. 생성기는 메서드의 파라미터 수를 분석하여 이 분기를 자동으로 처리합니다.

---

## 테스트 전략

소스 생성기의 출력은 문자열 기반 코드이므로, 기대하는 결과와 정확히 일치하는지 검증하는 것이 핵심입니다. 스냅샷 테스트는 생성된 코드를 `.verified.txt` 파일과 비교하여 의도하지 않은 변경을 즉시 감지합니다.

### 스냅샷 테스트

```csharp
[Fact]
public Task Should_Generate_ObservableClass()
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
| 진단 | 4개 |

> **참고**: 위 31개는 `ObservablePortGeneratorTests`의 생성기 스냅샷 테스트입니다. 이와 별도로 런타임 Observability 구조 검증 테스트(`ObservablePortObservabilityTests`, `ObservablePortLoggingStructureTests`, `ObservablePortMetricsStructureTests`, `ObservablePortTracingStructureTests`)가 태그 구조, 로깅 필드, 메트릭 태그, Tracing 태그의 규격 준수를 검증합니다.

---

## 구현 체크리스트

실전에서 소스 생성기 프로젝트를 시작할 때, 프로젝트 설정부터 테스트까지 빠뜨리기 쉬운 항목들을 정리했습니다.

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
| `ObservablePortGenerator.cs` | 메인 소스 생성기 |
| `IncrementalGeneratorBase.cs` | 템플릿 메서드 패턴 |
| `TypeExtractor.cs` | 제네릭 타입 추출 |
| `CollectionTypeHelper.cs` | 컬렉션 타입 처리 |
| `SymbolDisplayFormats.cs` | 타입 문자열 포맷 |
| `SourceGeneratorTestRunner.cs` | 테스트 유틸리티 |

---

## FAQ

### Q1: ObservablePortGenerator를 다른 프로젝트에 도입하려면 최소한 어떤 준비가 필요한가요?
**A**: 세 가지가 필요합니다. 첫째, `netstandard2.0` 타겟의 소스 생성기 프로젝트를 구성합니다. 둘째, 대상 프로젝트에서 `IObservablePort` 인터페이스와 `[GenerateObservablePort]` 속성을 사용할 수 있도록 참조를 설정합니다. 셋째, Verify 스냅샷 테스트로 생성 결과를 검증하는 테스트 프로젝트를 준비합니다.

### Q2: 소스 생성기가 생성한 코드에 버그가 있으면 어떻게 수정하나요?
**A**: 생성된 `.g.cs` 파일은 직접 수정해도 다음 빌드에서 덮어쓰입니다. 소스 생성기의 코드 생성 로직(예: `GenerateMethod()`)을 수정한 뒤 테스트를 실행하고, Verify 스냅샷을 업데이트하여 수정 결과를 확인합니다. 스냅샷 diff를 통해 수정이 다른 시나리오에 영향을 미치는지도 즉시 파악할 수 있습니다.

### Q3: 이 튜토리얼에서 다룬 패턴을 소스 생성기가 아닌 다른 코드 생성 방식에도 적용할 수 있나요?
**A**: 템플릿 메서드 패턴, 결정적 출력 원칙, `StringBuilder` 기반 코드 조립, 스냅샷 테스트 등의 패턴은 T4 템플릿, Scriban 같은 텍스트 템플릿 엔진, 또는 CLI 기반 코드 생성 도구에도 동일하게 적용됩니다. 다만 컴파일 타임 실행과 증분 캐싱은 Roslyn 소스 생성기만의 고유한 장점입니다.

---

핵심 개념과 설계 패턴을 되짚었으니, 이제 이 지식을 어떤 방향으로 확장할 수 있는지 살펴볼 차례입니다.

→ [02. 다음 단계](02-next-steps.md)
