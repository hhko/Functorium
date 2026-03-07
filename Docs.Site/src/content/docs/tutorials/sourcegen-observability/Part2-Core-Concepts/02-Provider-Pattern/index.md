---
title: "Provider Pattern"
---

## 개요

앞 장에서 `Initialize` 메서드 안에서 파이프라인을 구성한다는 것을 확인했습니다. 그렇다면 이 파이프라인은 구체적으로 어떻게 만들어질까요? LINQ를 사용해 본 적이 있다면 `Select`, `Where`, `Collect` 같은 연산자가 익숙할 것입니다. **Provider 패턴은** 바로 이 LINQ 스타일의 선언적 연산자를 사용하여 소스 코드에서 필요한 정보를 추출하고 변환하는 데이터 파이프라인을 구성합니다. 우리 프로젝트의 ObservablePortGenerator도 이 패턴으로 `[GenerateObservablePort]` 속성이 붙은 클래스를 찾아 `ObservableClassInfo`로 변환합니다.

## 학습 목표

### 핵심 학습 목표
1. **IncrementalValuesProvider와 IncrementalValueProvider의** 차이를 이해한다
   - 복수 값(0..N)과 단일 값(정확히 1개)의 구분
2. **LINQ 스타일 연산자를** 활용한 파이프라인 구성 방법을 습득한다
   - Select, Where, Collect, Combine의 역할과 사용 시점
3. **ObservablePortGenerator의** 실제 파이프라인 구조를 분석한다
   - ForAttributeWithMetadataName → Where → Collect 흐름

---

## Provider란?

**Provider는** 소스 생성기의 **데이터 파이프라인을** 구성하는 핵심 요소입니다. LINQ에서 `IEnumerable<T>`에 `Select`와 `Where`를 체이닝하듯, Provider에도 동일한 이름의 연산자를 체이닝하여 소스 코드에서 필요한 정보를 추출하고 변환하는 과정을 선언적으로 표현합니다.

```
Provider 파이프라인 흐름
=======================

소스 코드
    │
    ▼
┌─────────────────────────┐
│ SyntaxProvider          │  소스에서 노드 추출
│ (ForAttributeWithMeta...)│
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ Select                  │  데이터 변환
│ (Syntax → 필요한 정보)   │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ Where                   │  필터링
│ (유효한 것만 선택)       │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│ Collect                 │  배열로 수집
│ (개별 항목 → 배열)       │
└───────────┬─────────────┘
            │
            ▼
RegisterSourceOutput
(코드 생성)
```

---

## 두 가지 Provider 타입

### IncrementalValuesProvider<T>

**복수(0개 이상)의** 값을 나타냅니다:

```csharp
// 여러 클래스가 [GenerateObservablePort] 속성을 가질 수 있음
IncrementalValuesProvider<ObservableClassInfo> provider = context.SyntaxProvider
    .ForAttributeWithMetadataName(...);

// 0개: 속성이 붙은 클래스 없음
// 1개: 하나의 클래스
// N개: 여러 클래스
```

### IncrementalValueProvider<T>

**정확히 1개**의 값을 나타냅니다:

```csharp
// 컴파일 옵션은 항상 1개
IncrementalValueProvider<CompilationOptions> options =
    context.CompilationOptionsProvider;

// Collect로 변환하면 단일 값이 됨
IncrementalValueProvider<ImmutableArray<ObservableClassInfo>> collected =
    provider.Collect();
```

---

## 주요 연산자

각 연산자는 LINQ의 대응 연산자와 동일한 의미를 가집니다. 차이점은 이 연산자들이 컴파일러의 증분 캐싱 시스템과 통합되어, 입력이 변경되지 않으면 이전 결과를 재사용한다는 것입니다.

### Select - 데이터 변환

```csharp
// SyntaxNode → 클래스 이름
var classNames = context.SyntaxProvider
    .ForAttributeWithMetadataName(...)
    .Select((ctx, _) => ctx.TargetSymbol.Name);

// ObservableClassInfo → 생성할 코드
var codes = provider
    .Select((info, _) => GenerateCode(info));
```

### Where - 필터링

```csharp
// 유효한 항목만 선택
var validClasses = provider
    .Where(x => x != ObservableClassInfo.None);

// public 클래스만 선택
var publicClasses = provider
    .Where(x => x.IsPublic);
```

### Collect - 배열로 수집

```csharp
// IncrementalValuesProvider<T> → IncrementalValueProvider<ImmutableArray<T>>
var collected = provider.Collect();

// 여러 항목을 한 번에 처리할 때 유용
context.RegisterSourceOutput(collected, (ctx, items) =>
{
    foreach (var item in items)
    {
        ctx.AddSource(...);
    }
});
```

### Combine - 두 Provider 결합

```csharp
// 클래스 정보 + 컴파일 옵션 결합
var combined = provider.Combine(context.CompilationOptionsProvider);

context.RegisterSourceOutput(combined, (ctx, pair) =>
{
    var classInfo = pair.Left;
    var options = pair.Right;
    // ...
});
```

---

## 실제 코드: ObservablePortGenerator

지금까지 개별 연산자를 살펴보았으니, 우리 프로젝트에서 이 연산자들이 어떻게 조합되는지 확인해 봅니다.

```csharp
private static IncrementalValuesProvider<ObservableClassInfo> RegisterSourceProvider(
    IncrementalGeneratorInitializationContext context)
{
    // 1단계: 고정 코드 생성 (Attribute 정의)
    context.RegisterPostInitializationOutput(ctx =>
        ctx.AddSource(
            hintName: GenerateObservablePortAttributeFileName,
            sourceText: SourceText.From(GenerateObservablePortAttribute, Encoding.UTF8)));

    // 2단계: 파이프라인 구성
    return context
        .SyntaxProvider
        // [GenerateObservablePort] 속성이 붙은 클래스만 선택
        .ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: FullyQualifiedAttributeName,
            predicate: IsClass,                    // Syntax 수준 필터
            transform: MapToObservableClassInfo)     // Semantic 정보 추출
        // 유효하지 않은 항목 제외
        .Where(x => x != ObservableClassInfo.None);
}
```

---

## 파이프라인 구성 패턴

### 패턴 1: 단순 변환

```csharp
// 클래스 이름만 추출
var classNames = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .Select((ctx, _) => ctx.TargetSymbol.Name);

context.RegisterSourceOutput(classNames, (ctx, name) =>
{
    ctx.AddSource($"{name}.g.cs", $"// Generated for {name}");
});
```

### 패턴 2: 복잡한 데이터 구조

```csharp
// 상세 정보를 레코드로 변환
var classInfos = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .Select((ctx, _) => new ClassInfo(
        Name: ctx.TargetSymbol.Name,
        Namespace: ctx.TargetSymbol.ContainingNamespace.ToString(),
        Methods: GetMethods(ctx.TargetSymbol)));

context.RegisterSourceOutput(classInfos, (ctx, info) =>
{
    var code = GenerateCode(info);
    ctx.AddSource($"{info.Name}.g.cs", code);
});
```

### 패턴 3: 일괄 처리

```csharp
// 모든 클래스를 한 번에 처리
var allClasses = context.SyntaxProvider
    .ForAttributeWithMetadataName("MyAttribute", ...)
    .Collect();  // ImmutableArray로 수집

context.RegisterSourceOutput(allClasses, (ctx, classes) =>
{
    // 요약 파일 생성
    var summary = string.Join("\n", classes.Select(c => c.Name));
    ctx.AddSource("Summary.g.cs", $"// Generated {classes.Length} classes\n{summary}");

    // 각 클래스별 파일 생성
    foreach (var cls in classes)
    {
        ctx.AddSource($"{cls.Name}.g.cs", GenerateCode(cls));
    }
});
```

### 패턴 4: 조건부 결합

```csharp
// 컴파일 옵션에 따라 다른 코드 생성
var withOptions = provider
    .Combine(context.CompilationOptionsProvider);

context.RegisterSourceOutput(withOptions, (ctx, pair) =>
{
    var (classInfo, options) = pair;

    string code = options.OptimizationLevel == OptimizationLevel.Debug
        ? GenerateDebugCode(classInfo)
        : GenerateReleaseCode(classInfo);

    ctx.AddSource($"{classInfo.Name}.g.cs", code);
});
```

---

## 캐싱과 성능

Provider 패턴을 사용해야 하는 가장 중요한 이유는 **자동 캐싱**입니다. 파이프라인의 각 단계에서 입력이 이전과 동일하면 컴파일러가 해당 단계의 결과를 캐시에서 가져와 처리를 건너뜁니다.

```
증분 빌드 시 동작
================

1. 파일 A 수정됨
   │
   ▼
2. 파이프라인 재실행
   - 파일 A: 새로 처리
   - 파일 B: 캐시에서 가져옴 (처리 생략)
   - 파일 C: 캐시에서 가져옴 (처리 생략)
   │
   ▼
3. 변경된 파일 A에 대해서만 코드 재생성
```

### 캐싱을 위한 주의사항

```csharp
// ❌ 나쁜 예: 비결정적 데이터
.Select((ctx, _) => new ClassInfo(
    Name: ctx.TargetSymbol.Name,
    Timestamp: DateTime.Now  // 매번 다른 값!
))

// ✅ 좋은 예: 결정적 데이터
.Select((ctx, _) => new ClassInfo(
    Name: ctx.TargetSymbol.Name,
    Namespace: ctx.TargetSymbol.ContainingNamespace.ToString()
))
```

---

## 데이터 모델 설계

캐싱이 올바르게 작동하려면 데이터 모델이 **값 의미론을** 가져야 합니다. 내용이 같은 두 객체가 `Equals`로 동일하게 판정되어야 컴파일러가 "변경 없음"을 인식하고 캐시를 활용할 수 있기 때문입니다. 우리 프로젝트의 `ObservableClassInfo`가 `readonly record struct`로 정의된 것도 이 이유입니다.

```csharp
// ✅ readonly record struct 사용 (값 의미론 + 자동 Equals/GetHashCode)
public readonly record struct ObservableClassInfo
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly List<MethodInfo> Methods;
    public readonly List<ParameterInfo> BaseConstructorParameters;
    public readonly Location? Location;

    // None 패턴으로 null 대신 빈 객체 사용
    public static readonly ObservableClassInfo None = new(
        string.Empty, string.Empty, new List<MethodInfo>(),
        new List<ParameterInfo>(), null);

    public ObservableClassInfo(
        string @namespace, string className,
        List<MethodInfo> methods,
        List<ParameterInfo> baseConstructorParameters,
        Location? location)
    {
        Namespace = @namespace;
        ClassName = className;
        Methods = methods;
        BaseConstructorParameters = baseConstructorParameters;
        Location = location;
    }
}

// 생성자 기반 class
public class MethodInfo
{
    public string Name { get; }
    public List<ParameterInfo> Parameters { get; }
    public string ReturnType { get; }

    public MethodInfo(string name, List<ParameterInfo> parameters,
        string returnType)
    {
        Name = name;
        Parameters = parameters;
        ReturnType = returnType;
    }
}

public class ParameterInfo
{
    public string Name { get; }
    public string Type { get; }
    public RefKind RefKind { get; }
    public bool IsCollection { get; }

    public ParameterInfo(string name, string type, RefKind refKind)
    {
        Name = name;
        Type = type;
        RefKind = refKind;
        IsCollection = CollectionTypeHelper.IsCollectionType(type);
    }
}
```

---

## 요약

Provider 패턴은 LINQ와 동일한 선언적 스타일로 소스 생성 파이프라인을 구성하되, 각 단계에 자동 캐싱을 제공하여 증분 빌드 성능을 보장합니다. 데이터 모델에 값 의미론을 적용하는 것이 캐싱의 핵심 전제 조건입니다.

| Provider 타입 | 값 개수 | 용도 |
|---------------|---------|------|
| `IncrementalValuesProvider<T>` | 0..N개 | 여러 항목 처리 |
| `IncrementalValueProvider<T>` | 정확히 1개 | 단일 값, Collect 결과 |

| 연산자 | 기능 | 반환 |
|--------|------|------|
| `Select` | 변환 | 같은 Provider 타입 |
| `Where` | 필터링 | ValuesProvider |
| `Collect` | 배열로 수집 | ValueProvider |
| `Combine` | 결합 | ValueProvider (튜플) |

---

## 다음 단계

Provider 파이프라인의 전체 흐름을 이해했으니, 다음으로는 파이프라인의 시작점에서 가장 자주 사용되는 API인 `ForAttributeWithMetadataName`을 살펴봅니다. 이 API가 속성 기반 필터링을 어떻게 최적화하는지, 그리고 직접 구현과 비교했을 때 왜 10~100배 빠른지 확인합니다.

→ [03. ForAttributeWithMetadataName](../03-ForAttribute/)
