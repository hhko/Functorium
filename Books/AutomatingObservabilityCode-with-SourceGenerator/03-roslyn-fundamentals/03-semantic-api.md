# Semantic API

## 학습 목표

- Semantic Model의 역할 이해
- 타입 정보 조회 방법 습득
- Syntax API와 Semantic API의 연계 학습

---

## Semantic API란?

**Semantic API**는 Syntax Tree에 **타입 정보와 의미론적 분석**을 추가한 것입니다.

```
Syntax API vs Semantic API
==========================

Syntax API (구문)
-----------------
코드: public void Process(User user) { }

알 수 있는 것:
- 메서드 이름이 "Process"
- 파라미터 이름이 "user"
- 파라미터 타입 텍스트가 "User"

알 수 없는 것:
- User가 클래스? 인터페이스? 구조체?
- User의 전체 네임스페이스?
- User에 어떤 멤버가 있는지?


Semantic API (의미)
------------------
알 수 있는 것:
- User는 MyApp.Models.User 클래스
- User는 IEntity 인터페이스 구현
- User에는 Id, Name 프로퍼티 존재
- Process 메서드의 반환 타입은 void
```

---

## SemanticModel 얻기

### 일반적인 방법

```csharp
// Compilation에서 SemanticModel 얻기
var compilation = CSharpCompilation.Create(
    "MyAssembly",
    [syntaxTree],
    references,
    options);

SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
```

### 소스 생성기에서

```csharp
context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "MyNamespace.GeneratePipelineAttribute",
        predicate: (node, _) => node is ClassDeclarationSyntax,
        transform: (ctx, _) =>
        {
            // GeneratorAttributeSyntaxContext에서 직접 접근
            SemanticModel semanticModel = ctx.SemanticModel;

            // 또는 타겟 심볼 직접 사용
            ISymbol symbol = ctx.TargetSymbol;

            return symbol;
        });
```

---

## 심볼 정보 조회

### GetSymbolInfo

Syntax 노드에서 심볼 정보를 얻습니다:

```csharp
string code = """
    public class User
    {
        public int Id { get; set; }
    }

    public class Example
    {
        public void Process(User user)
        {
            var id = user.Id;  // 이 부분 분석
        }
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("Test", [tree], references);
var semanticModel = compilation.GetSemanticModel(tree);

// user.Id 표현식 찾기
var memberAccess = tree.GetRoot()
    .DescendantNodes()
    .OfType<MemberAccessExpressionSyntax>()
    .First();

// 심볼 정보 조회
SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
ISymbol? symbol = symbolInfo.Symbol;

Console.WriteLine($"심볼: {symbol?.Name}");           // Id
Console.WriteLine($"종류: {symbol?.Kind}");          // Property
Console.WriteLine($"포함 타입: {symbol?.ContainingType}"); // User
```

### GetTypeInfo

표현식의 타입 정보를 얻습니다:

```csharp
// var id = user.Id; 에서 id의 타입
var variableDecl = tree.GetRoot()
    .DescendantNodes()
    .OfType<VariableDeclaratorSyntax>()
    .First(v => v.Identifier.Text == "id");

var initializer = variableDecl.Initializer!.Value;
TypeInfo typeInfo = semanticModel.GetTypeInfo(initializer);

Console.WriteLine($"타입: {typeInfo.Type}");          // int
Console.WriteLine($"변환 타입: {typeInfo.ConvertedType}"); // int
```

### GetDeclaredSymbol

선언에서 심볼을 얻습니다:

```csharp
var classDecl = tree.GetRoot()
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

// 클래스 선언에서 심볼 얻기
INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

Console.WriteLine($"클래스: {classSymbol?.Name}");
Console.WriteLine($"네임스페이스: {classSymbol?.ContainingNamespace}");
Console.WriteLine($"인터페이스: {string.Join(", ", classSymbol?.AllInterfaces ?? [])}");
```

---

## 소스 생성기에서의 활용

### GeneratorAttributeSyntaxContext 활용

```csharp
private static PipelineClassInfo MapToPipelineClassInfo(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken)
{
    // 1. 타겟 심볼 직접 접근 (Semantic API)
    if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
    {
        return PipelineClassInfo.None;
    }

    // 2. 클래스 정보 추출
    string className = classSymbol.Name;
    string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
        ? string.Empty
        : classSymbol.ContainingNamespace.ToString();

    // 3. 구현한 인터페이스 분석
    var interfaces = classSymbol.AllInterfaces;

    // 4. 인터페이스의 메서드 추출
    var methods = interfaces
        .Where(ImplementsIAdapter)
        .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
        .Where(m => m.MethodKind == MethodKind.Ordinary)
        .ToList();

    return new PipelineClassInfo(@namespace, className, methods);
}
```

---

## 타입 비교와 검사

### 타입 동일성 확인

```csharp
// 두 타입이 같은지 확인
bool areSameType = SymbolEqualityComparer.Default.Equals(type1, type2);

// SymbolEqualityComparer 옵션
// Default: 기본 비교
// IncludeNullability: nullable 어노테이션 포함 비교
```

### 특정 타입인지 확인

```csharp
// IAdapter 인터페이스를 구현하는지 확인
bool implementsIAdapter = classSymbol.AllInterfaces
    .Any(i => i.Name == "IAdapter");

// 특정 네임스페이스의 타입인지 확인
bool isInMyNamespace = classSymbol.ContainingNamespace
    .ToDisplayString() == "MyApp.Models";
```

### 타입 이름 얻기

```csharp
// 다양한 포맷으로 타입 이름 얻기
ITypeSymbol type = ...;

// 짧은 이름
string shortName = type.Name;  // User

// 네임스페이스 포함
string fullName = type.ToDisplayString();  // MyApp.Models.User

// global:: 접두사 포함 (결정적 코드 생성에 중요)
string globalName = type.ToDisplayString(
    SymbolDisplayFormat.FullyQualifiedFormat);  // global::MyApp.Models.User
```

---

## 메서드 심볼 분석

```csharp
IMethodSymbol method = ...;

// 기본 정보
Console.WriteLine($"이름: {method.Name}");
Console.WriteLine($"반환 타입: {method.ReturnType}");
Console.WriteLine($"정적 여부: {method.IsStatic}");
Console.WriteLine($"비동기 여부: {method.IsAsync}");

// 파라미터 분석
foreach (var param in method.Parameters)
{
    Console.WriteLine($"파라미터: {param.Type} {param.Name}");
    Console.WriteLine($"  - RefKind: {param.RefKind}");  // None, Ref, Out, In
    Console.WriteLine($"  - 기본값: {param.HasExplicitDefaultValue}");
}

// 제네릭 타입 파라미터
if (method.IsGenericMethod)
{
    foreach (var typeParam in method.TypeParameters)
    {
        Console.WriteLine($"타입 파라미터: {typeParam.Name}");
    }
}
```

---

## 실제 코드 예시: AdapterPipelineGenerator

```csharp
// AdapterPipelineGenerator.cs에서 메서드 정보 추출
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIAdapter)
    .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
    .Where(m => m.MethodKind == MethodKind.Ordinary)
    .Select(m => new MethodInfo(
        m.Name,
        m.Parameters.Select(p => new ParameterInfo(
            p.Name,
            // ★ Semantic API로 정확한 타입 문자열 얻기
            p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            p.RefKind)).ToList(),
        // ★ 반환 타입도 정확히 추출
        m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
    .ToList();
```

---

## Semantic API 성능 고려사항

```
성능 팁
=======

1. SemanticModel은 무거움
   - 가능하면 캐싱
   - 불필요하게 여러 번 생성하지 않기

2. GetSymbolInfo vs GetDeclaredSymbol
   - 선언에서 심볼 얻기: GetDeclaredSymbol (빠름)
   - 참조에서 심볼 얻기: GetSymbolInfo (조금 느림)

3. ForAttributeWithMetadataName 활용
   - 직접 Syntax Tree 순회보다 효율적
   - 증분 빌드에 최적화됨
```

---

## 요약

| 메서드 | 용도 | 입력 | 출력 |
|--------|------|------|------|
| `GetSymbolInfo` | 참조 해석 | 표현식 노드 | SymbolInfo |
| `GetTypeInfo` | 타입 정보 | 표현식 노드 | TypeInfo |
| `GetDeclaredSymbol` | 선언 심볼 | 선언 노드 | ISymbol |

| 비교 | Syntax API | Semantic API |
|------|------------|--------------|
| 정보 | 구조 | 구조 + 타입 |
| 속도 | 빠름 | 상대적으로 느림 |
| 용도 | 필터링 | 상세 분석 |

---

## 다음 단계

다음 섹션에서는 심볼의 상세 타입들을 학습합니다.

➡️ [04. 심볼 타입](04-symbol-types.md)
