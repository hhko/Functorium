# Provider 패턴

## 학습 목표

- IncrementalValuesProvider와 IncrementalValueProvider 이해
- LINQ 스타일 파이프라인 구성 방법 습득
- Select, Where, Collect 등 연산자 활용

---

## Provider란?

**Provider**는 소스 생성기의 **데이터 파이프라인**을 구성하는 핵심 요소입니다. 소스 코드에서 필요한 정보를 추출하고 변환하는 과정을 선언적으로 표현합니다.

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

**복수(0개 이상)**의 값을 나타냅니다:

```csharp
// 여러 클래스가 [GeneratePipeline] 속성을 가질 수 있음
IncrementalValuesProvider<PipelineClassInfo> provider = context.SyntaxProvider
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
IncrementalValueProvider<ImmutableArray<PipelineClassInfo>> collected =
    provider.Collect();
```

---

## 주요 연산자

### Select - 데이터 변환

```csharp
// SyntaxNode → 클래스 이름
var classNames = context.SyntaxProvider
    .ForAttributeWithMetadataName(...)
    .Select((ctx, _) => ctx.TargetSymbol.Name);

// PipelineClassInfo → 생성할 코드
var codes = provider
    .Select((info, _) => GenerateCode(info));
```

### Where - 필터링

```csharp
// 유효한 항목만 선택
var validClasses = provider
    .Where(x => x != PipelineClassInfo.None);

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

## 실제 코드: AdapterPipelineGenerator

```csharp
private static IncrementalValuesProvider<PipelineClassInfo> RegisterSourceProvider(
    IncrementalGeneratorInitializationContext context)
{
    // 1단계: 고정 코드 생성 (Attribute 정의)
    context.RegisterPostInitializationOutput(ctx =>
        ctx.AddSource(
            hintName: GeneratePipelineAttributeFileName,
            sourceText: SourceText.From(GeneratePipelineAttribute, Encoding.UTF8)));

    // 2단계: 파이프라인 구성
    return context
        .SyntaxProvider
        // [GeneratePipeline] 속성이 붙은 클래스만 선택
        .ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: FullyQualifiedAttributeName,
            predicate: IsClass,                    // Syntax 수준 필터
            transform: MapToPipelineClassInfo)     // Semantic 정보 추출
        // 유효하지 않은 항목 제외
        .Where(x => x != PipelineClassInfo.None);
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

Provider 패턴의 핵심 장점은 **자동 캐싱**입니다:

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

캐싱이 올바르게 작동하려면 데이터 모델이 **값 의미론**을 가져야 합니다:

```csharp
// ✅ 레코드 사용 (자동으로 Equals/GetHashCode 구현)
public sealed record PipelineClassInfo(
    string Namespace,
    string ClassName,
    List<MethodInfo> Methods,
    List<ParameterInfo> BaseConstructorParameters)
{
    // None 패턴으로 null 대신 빈 객체 사용
    public static readonly PipelineClassInfo None = new(
        string.Empty, string.Empty, [], []);
}

public sealed record MethodInfo(
    string Name,
    List<ParameterInfo> Parameters,
    string ReturnType);

public sealed record ParameterInfo(
    string Name,
    string Type,
    RefKind RefKind);
```

---

## 요약

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

다음 섹션에서는 속성 기반 필터링의 핵심 API를 학습합니다.

➡️ [03. ForAttributeWithMetadataName](03-forattributewithmetadataname.md)
