---
title: "ForAttributeWithMetadataName"
---

## 개요

앞 장에서 Provider 파이프라인의 시작점으로 `SyntaxProvider`를 사용한다고 언급했습니다. 실제로 속성 기반 소스 생성기에서는 거의 항상 `ForAttributeWithMetadataName`이 그 시작점이 됩니다. 이 API는 컴파일러의 내부 속성 인덱스를 직접 활용하기 때문에, 모든 노드를 순회하며 속성을 확인하는 수동 구현보다 10~100배 빠릅니다. ObservablePortGenerator 역시 `[GenerateObservablePort]` 속성을 기반으로 동작하므로 이 API가 파이프라인의 핵심 진입점입니다.

## 학습 목표

### 핵심 학습 목표
1. **ForAttributeWithMetadataName API의** 역할과 성능 이점을 이해한다
   - 컴파일러 내부 인덱스를 활용한 최적화 원리
2. **predicate와 transform 콜백의** 활용법을 습득한다
   - Syntax 수준 필터와 Semantic 수준 변환의 분리
3. **GeneratorAttributeSyntaxContext의** 구조를 파악한다
   - TargetSymbol, SemanticModel, Attributes 접근 방법

---

## ForAttributeWithMetadataName이란?

**속성(Attribute) 기반 소스 생성**의 핵심 API입니다. 특정 속성이 붙은 선언만 효율적으로 필터링합니다.

```csharp
IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
    string fullyQualifiedMetadataName,  // 속성의 전체 이름
    Func<SyntaxNode, CancellationToken, bool> predicate,  // Syntax 수준 필터
    Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform  // 변환
);
```

---

## 왜 ForAttributeWithMetadataName인가?

### 직접 구현 vs ForAttributeWithMetadataName

```csharp
// ❌ 직접 구현 (비효율적)
var classes = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: (node, _) => node is ClassDeclarationSyntax,
        transform: (ctx, _) =>
        {
            var classDecl = (ClassDeclarationSyntax)ctx.Node;

            // 모든 클래스에 대해 Semantic Model 접근 (느림!)
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl);

            // 속성 확인
            return symbol?.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "GenerateObservablePortAttribute")
                    == true ? symbol : null;
        })
    .Where(x => x is not null);

// ✅ ForAttributeWithMetadataName (효율적)
var classes = context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "MyNamespace.GenerateObservablePortAttribute",  // 컴파일러가 최적화
        predicate: (node, _) => node is ClassDeclarationSyntax,
        transform: (ctx, _) => ctx.TargetSymbol);  // 이미 심볼이 준비됨
```

### 성능 차이

```
직접 구현
=========
1. 모든 클래스 순회
2. 각 클래스에 Semantic Model 접근
3. 속성 목록 조회
4. 속성 이름 비교

ForAttributeWithMetadataName
============================
1. 컴파일러가 속성 인덱스에서 직접 조회
2. 해당 속성이 있는 선언만 반환
3. Semantic Model이 미리 준비됨

→ 10-100배 이상 빠름
```

---

## 메서드 시그니처 분석

```csharp
.ForAttributeWithMetadataName(
    fullyQualifiedMetadataName: "Namespace.AttributeName",
    predicate: (SyntaxNode node, CancellationToken ct) => bool,
    transform: (GeneratorAttributeSyntaxContext ctx, CancellationToken ct) => T
)
```

### fullyQualifiedMetadataName

속성의 **전체 메타데이터 이름**입니다:

```csharp
// 속성 정의
namespace Functorium.Adapters.SourceGenerators;

public class GenerateObservablePortAttribute : System.Attribute { }

// 메타데이터 이름
"Functorium.Adapters.SourceGenerators.GenerateObservablePortAttribute"

// 제네릭 속성의 경우
"MyNamespace.MyAttribute`1"  // <T>를 가진 속성
```

### predicate

**Syntax 수준**에서 빠르게 필터링합니다:

```csharp
// 클래스만 선택
predicate: (node, _) => node is ClassDeclarationSyntax

// public 클래스만 선택
predicate: (node, _) =>
    node is ClassDeclarationSyntax classDecl &&
    classDecl.Modifiers.Any(SyntaxKind.PublicKeyword)

// 특정 이름 패턴만 선택
predicate: (node, _) =>
    node is ClassDeclarationSyntax classDecl &&
    classDecl.Identifier.Text.EndsWith("Repository")
```

### transform

**Semantic 정보**를 활용하여 필요한 데이터를 추출합니다:

```csharp
transform: (ctx, cancellationToken) =>
{
    // ctx.TargetNode: 속성이 붙은 Syntax 노드
    // ctx.TargetSymbol: 해당 심볼 (ISymbol)
    // ctx.SemanticModel: Semantic Model
    // ctx.Attributes: 매칭된 속성들

    return ExtractInfo(ctx.TargetSymbol);
}
```

---

## GeneratorAttributeSyntaxContext

transform 콜백에서 받는 컨텍스트입니다:

```csharp
public readonly struct GeneratorAttributeSyntaxContext
{
    // 속성이 붙은 Syntax 노드 (ClassDeclarationSyntax 등)
    public SyntaxNode TargetNode { get; }

    // 해당 심볼 (INamedTypeSymbol, IMethodSymbol 등)
    public ISymbol TargetSymbol { get; }

    // Semantic Model
    public SemanticModel SemanticModel { get; }

    // 매칭된 속성들 (같은 속성이 여러 개일 수 있음)
    public ImmutableArray<AttributeData> Attributes { get; }
}
```

---

## 실제 코드: ObservablePortGenerator

지금까지 API의 각 구성 요소를 살펴보았습니다. 이제 우리 프로젝트에서 이 요소들이 어떻게 조합되는지 전체 흐름을 확인합니다.

```csharp
[Generator(LanguageNames.CSharp)]
public sealed class ObservablePortGenerator()
    : IncrementalGeneratorBase<ObservableClassInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)
{
    private const string AttributeName = "GenerateObservablePort";
    private const string AttributeNamespace = "Functorium.Adapters.SourceGenerators";
    private const string FullyQualifiedAttributeName =
        $"{AttributeNamespace}.{AttributeName}Attribute";

    private static IncrementalValuesProvider<ObservableClassInfo> RegisterSourceProvider(
        IncrementalGeneratorInitializationContext context)
    {
        // 1. 속성 정의 생성
        context.RegisterPostInitializationOutput(ctx =>
            ctx.AddSource(
                hintName: "GenerateObservablePortAttribute.g.cs",
                sourceText: SourceText.From(GenerateObservablePortAttribute, Encoding.UTF8)));

        // 2. ForAttributeWithMetadataName으로 필터링
        return context
            .SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: FullyQualifiedAttributeName,
                predicate: IsClass,                    // 클래스인지 확인
                transform: MapToObservableClassInfo)     // 클래스 정보 추출
            .Where(x => x != ObservableClassInfo.None);  // 유효한 것만
    }

    // predicate 구현
    private static bool IsClass(SyntaxNode node, CancellationToken cancellationToken)
        => node is ClassDeclarationSyntax;

    // transform 구현
    private static ObservableClassInfo MapToObservableClassInfo(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken)
    {
        // 클래스 심볼 확인
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        {
            return ObservableClassInfo.None;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 클래스 정보 추출
        string className = classSymbol.Name;
        string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToString();

        // IObservablePort 인터페이스의 메서드 추출
        var methods = classSymbol.AllInterfaces
            .Where(ImplementsIObservablePort)
            .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
            .Where(m => m.MethodKind == MethodKind.Ordinary)
            .Select(m => new MethodInfo(
                m.Name,
                m.Parameters.Select(p => new ParameterInfo(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
                    p.RefKind)).ToList(),
                m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
            .ToList();

        // 메서드가 없으면 생성 불필요
        if (methods.Count == 0)
        {
            return ObservableClassInfo.None;
        }

        // 생성자 파라미터 추출
        var baseConstructorParameters =
            ConstructorParameterExtractor.ExtractParameters(classSymbol);

        return new ObservableClassInfo(
            @namespace, className, methods, baseConstructorParameters);
    }
}
```

---

## 속성(Attribute) 정의 생성

ForAttributeWithMetadataName을 사용하려면 **속성이 정의**되어 있어야 합니다:

```csharp
// RegisterPostInitializationOutput에서 속성 정의 생성
public const string GenerateObservablePortAttribute = """
    // <auto-generated/>

    namespace Functorium.Adapters.SourceGenerators;

    /// <summary>
    /// 어댑터 클래스에 파이프라인 래퍼 생성을 지시하는 속성
    /// </summary>
    [global::System.AttributeUsage(
        global::System.AttributeTargets.Class,
        AllowMultiple = false,
        Inherited = false)]
    [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(
        Justification = "Generated by source generator.")]
    public class GenerateObservablePortAttribute : global::System.Attribute;
    """;
```

### global:: 접두사를 사용하는 이유

```csharp
// ❌ 충돌 가능성
public class GenerateObservablePortAttribute : System.Attribute;
// 사용자 코드에 System 네임스페이스가 있으면 충돌

// ✅ 항상 안전
public class GenerateObservablePortAttribute : global::System.Attribute;
// global::은 항상 전역 네임스페이스에서 시작
```

---

## 취소 토큰 처리

장시간 실행되는 transform에서는 취소 토큰을 확인해야 합니다:

```csharp
transform: (ctx, cancellationToken) =>
{
    // 무거운 작업 전에 취소 확인
    cancellationToken.ThrowIfCancellationRequested();

    var methods = classSymbol.AllInterfaces
        .SelectMany(i =>
        {
            // 루프 내에서도 확인
            cancellationToken.ThrowIfCancellationRequested();
            return i.GetMembers().OfType<IMethodSymbol>();
        })
        .ToList();

    return new ClassInfo(...);
}
```

---

## 요약

`ForAttributeWithMetadataName`은 속성 기반 소스 생성기의 핵심 진입점입니다. `predicate`에서 Syntax 수준의 빠른 필터링을, `transform`에서 Semantic 수준의 데이터 추출을 담당하는 두 단계 분리가 성능의 핵심입니다. 속성 이름에는 반드시 네임스페이스와 `Attribute` 접미사를 포함해야 합니다.

| 구성 요소 | 역할 | 주의사항 |
|-----------|------|----------|
| `fullyQualifiedMetadataName` | 속성 전체 이름 | 네임스페이스 포함, `Attribute` 접미사 포함 |
| `predicate` | Syntax 수준 필터 | 빠름, Semantic 접근 불가 |
| `transform` | 데이터 추출 | Semantic 접근 가능, 무거움 |
| `GeneratorAttributeSyntaxContext` | transform 컨텍스트 | TargetSymbol이 핵심 |

---

## 다음 단계

`ForAttributeWithMetadataName`으로 데이터를 효율적으로 추출하는 방법을 이해했습니다. 하지만 이 효율성이 제대로 발휘되려면 파이프라인의 각 단계에서 캐싱이 올바르게 작동해야 합니다. 다음 장에서는 증분 캐싱의 원리와, 캐시를 무효화시키는 흔한 실수들을 살펴봅니다.

→ [04. 증분 캐싱](../04-Incremental-Caching/)
