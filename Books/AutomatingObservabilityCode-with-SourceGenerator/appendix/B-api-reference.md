# 부록 B: API 레퍼런스

## IIncrementalGenerator

```csharp
namespace Microsoft.CodeAnalysis;

public interface IIncrementalGenerator
{
    void Initialize(IncrementalGeneratorInitializationContext context);
}
```

### 구현 예시

```csharp
[Generator]
public class MyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. 소스 제공자 등록
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(...)
            .Where(...)
            .Select(...);

        // 2. 출력 등록
        context.RegisterSourceOutput(provider, Generate);
    }

    private void Generate(SourceProductionContext context, MyInfo info)
    {
        context.AddSource($"{info.Name}.g.cs", code);
    }
}
```

---

## IncrementalGeneratorInitializationContext

### 주요 멤버

| 멤버 | 설명 |
|------|------|
| `SyntaxProvider` | 구문 기반 소스 제공자 |
| `CompilationProvider` | 컴파일 정보 제공자 |
| `RegisterSourceOutput` | 소스 출력 등록 |
| `RegisterPostInitializationOutput` | 초기화 후 출력 등록 |

### ForAttributeWithMetadataName

```csharp
IncrementalValuesProvider<T> ForAttributeWithMetadataName<T>(
    string fullyQualifiedMetadataName,
    Func<SyntaxNode, CancellationToken, bool> predicate,
    Func<GeneratorAttributeSyntaxContext, CancellationToken, T> transform)
```

**파라미터:**
- `fullyQualifiedMetadataName`: 속성의 전체 이름 (예: `"MyNamespace.MyAttribute"`)
- `predicate`: 노드 필터 조건
- `transform`: 변환 함수

---

## GeneratorAttributeSyntaxContext

### 주요 프로퍼티

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `TargetNode` | `SyntaxNode` | 대상 구문 노드 |
| `TargetSymbol` | `ISymbol` | 대상 심볼 |
| `SemanticModel` | `SemanticModel` | 의미 모델 |
| `Attributes` | `ImmutableArray<AttributeData>` | 속성 데이터 |

---

## ISymbol 계층구조

```
ISymbol
├── INamespaceSymbol
├── ITypeSymbol
│   ├── INamedTypeSymbol (클래스, 인터페이스, 구조체)
│   ├── IArrayTypeSymbol
│   └── ITypeParameterSymbol
├── IMethodSymbol
├── IPropertySymbol
├── IFieldSymbol
├── IParameterSymbol
└── ILocalSymbol
```

---

## INamedTypeSymbol

### 주요 프로퍼티

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Name` | `string` | 타입 이름 |
| `ContainingNamespace` | `INamespaceSymbol` | 포함 네임스페이스 |
| `BaseType` | `INamedTypeSymbol?` | 부모 타입 |
| `AllInterfaces` | `ImmutableArray<INamedTypeSymbol>` | 모든 인터페이스 |
| `Constructors` | `ImmutableArray<IMethodSymbol>` | 생성자들 |
| `TypeKind` | `TypeKind` | 클래스/인터페이스/구조체 |

### 주요 메서드

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `GetMembers()` | `ImmutableArray<ISymbol>` | 모든 멤버 |
| `GetMembers(string name)` | `ImmutableArray<ISymbol>` | 이름으로 멤버 검색 |
| `ToDisplayString(format)` | `string` | 포맷된 문자열 |

---

## IMethodSymbol

### 주요 프로퍼티

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Name` | `string` | 메서드 이름 |
| `ReturnType` | `ITypeSymbol` | 반환 타입 |
| `Parameters` | `ImmutableArray<IParameterSymbol>` | 파라미터들 |
| `IsVirtual` | `bool` | virtual 여부 |
| `IsAbstract` | `bool` | abstract 여부 |
| `IsOverride` | `bool` | override 여부 |
| `DeclaredAccessibility` | `Accessibility` | 접근 제한자 |

---

## IParameterSymbol

### 주요 프로퍼티

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Name` | `string` | 파라미터 이름 |
| `Type` | `ITypeSymbol` | 파라미터 타입 |
| `RefKind` | `RefKind` | ref/out/in 여부 |
| `IsOptional` | `bool` | 선택적 파라미터 여부 |
| `HasExplicitDefaultValue` | `bool` | 기본값 존재 여부 |
| `ExplicitDefaultValue` | `object?` | 기본값 |

---

## SymbolDisplayFormat

### 사전 정의 포맷

| 포맷 | 예시 출력 |
|------|----------|
| `MinimallyQualifiedFormat` | `List<User>` |
| `FullyQualifiedFormat` | `System.Collections.Generic.List<MyApp.User>` |

### 커스텀 포맷

```csharp
public static readonly SymbolDisplayFormat GlobalQualifiedFormat = new(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

// 결과: global::System.Collections.Generic.List<global::MyApp.User>
```

---

## SourceProductionContext

### 주요 멤버

| 멤버 | 설명 |
|------|------|
| `AddSource(hintName, source)` | 소스 파일 추가 |
| `ReportDiagnostic(diagnostic)` | 진단 보고 |
| `CancellationToken` | 취소 토큰 |

### AddSource 예시

```csharp
context.AddSource(
    hintName: $"{className}.g.cs",
    source: SourceText.From(code, Encoding.UTF8));
```

---

## Diagnostic

### 생성 예시

```csharp
var diagnostic = Diagnostic.Create(
    new DiagnosticDescriptor(
        id: "SG001",
        title: "Generation Error",
        messageFormat: "Cannot generate for {0}",
        category: "SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true),
    Location.None,
    className);

context.ReportDiagnostic(diagnostic);
```

### DiagnosticSeverity

| 레벨 | 설명 |
|------|------|
| `Hidden` | 숨김 |
| `Info` | 정보 |
| `Warning` | 경고 |
| `Error` | 오류 |

---

## CSharpCompilation (테스트용)

### 생성 예시

```csharp
var compilation = CSharpCompilation.Create(
    assemblyName: "TestAssembly",
    syntaxTrees: [syntaxTree],
    references: references,
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
```

---

## CSharpGeneratorDriver (테스트용)

### 사용 예시

```csharp
CSharpGeneratorDriver
    .Create(generator)
    .RunGeneratorsAndUpdateCompilation(
        compilation,
        out var outputCompilation,
        out var diagnostics);
```

---

➡️ [부록 C: 테스트 시나리오 카탈로그](C-test-scenario-catalog.md)
