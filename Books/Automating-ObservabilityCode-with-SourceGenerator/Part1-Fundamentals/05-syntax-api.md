# Syntax API

## 학습 목표

- SyntaxNode, SyntaxToken, SyntaxTrivia의 차이 이해
- Syntax Tree 탐색 방법 습득
- 소스 생성기에서 Syntax API 활용법 학습

---

## Syntax Tree의 구성 요소

Syntax Tree는 세 가지 요소로 구성됩니다:

```
Syntax Tree 구성 요소
====================

SyntaxNode (노드)
├── 문법적 구조를 나타내는 단위
├── 예: 클래스 선언, 메서드 선언, if 문
└── 자식 노드나 토큰을 포함

SyntaxToken (토큰)
├── 가장 작은 문법 단위
├── 예: 키워드(class), 식별자(User), 연산자(+)
└── Leading/Trailing Trivia 포함

SyntaxTrivia (트리비아)
├── 의미 없는 텍스트
├── 예: 공백, 줄바꿈, 주석
└── 토큰에 부착됨
```

### 예시로 이해하기

```csharp
// 원본 코드
public class User { }
```

```
Syntax Tree 구조
================

ClassDeclarationSyntax (노드)
├── Modifiers: [public] (토큰)
│   └── LeadingTrivia: [공백]
├── Keyword: [class] (토큰)
│   └── LeadingTrivia: [공백]
├── Identifier: [User] (토큰)
│   └── LeadingTrivia: [공백]
├── OpenBraceToken: [{] (토큰)
│   └── LeadingTrivia: [공백]
└── CloseBraceToken: [}] (토큰)
    └── LeadingTrivia: [공백]
```

---

## SyntaxNode 주요 타입

C#의 문법 요소마다 대응하는 SyntaxNode가 있습니다:

```
선언 관련
=========
CompilationUnitSyntax       전체 파일
NamespaceDeclarationSyntax  네임스페이스
ClassDeclarationSyntax      클래스
InterfaceDeclarationSyntax  인터페이스
MethodDeclarationSyntax     메서드
PropertyDeclarationSyntax   프로퍼티
FieldDeclarationSyntax      필드
ParameterSyntax             파라미터

문장 관련
=========
BlockSyntax                 { } 블록
IfStatementSyntax           if 문
ForStatementSyntax          for 문
ReturnStatementSyntax       return 문
ExpressionStatementSyntax   표현식 문

표현식 관련
==========
InvocationExpressionSyntax  메서드 호출
MemberAccessExpressionSyntax 멤버 접근 (a.b)
LiteralExpressionSyntax     리터럴 (5, "hello")
IdentifierNameSyntax        식별자 (변수명)
```

---

## Syntax Tree 탐색

### DescendantNodes - 모든 자손 노드

```csharp
string code = """
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var root = tree.GetRoot();

// 모든 프로퍼티 선언 찾기
var properties = root
    .DescendantNodes()
    .OfType<PropertyDeclarationSyntax>();

foreach (var prop in properties)
{
    Console.WriteLine($"{prop.Type} {prop.Identifier}");
}
// 출력:
// int Id
// string Name
```

### ChildNodes - 직접 자식만

```csharp
var classDecl = root
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

// 클래스의 직접 자식만 (프로퍼티, 메서드 등)
var members = classDecl.ChildNodes();

foreach (var member in members)
{
    Console.WriteLine($"멤버 종류: {member.Kind()}");
}
// 출력:
// 멤버 종류: PropertyDeclaration
// 멤버 종류: PropertyDeclaration
```

### Ancestors - 부모 노드들

```csharp
var property = root
    .DescendantNodes()
    .OfType<PropertyDeclarationSyntax>()
    .First();

// 프로퍼티의 부모 노드들
var ancestors = property.Ancestors();

foreach (var ancestor in ancestors)
{
    Console.WriteLine($"부모: {ancestor.Kind()}");
}
// 출력:
// 부모: ClassDeclaration
// 부모: CompilationUnit
```

---

## 소스 생성기에서의 활용

### ForAttributeWithMetadataName의 predicate

```csharp
context.SyntaxProvider
    .ForAttributeWithMetadataName(
        "MyNamespace.GeneratePipelineAttribute",
        // predicate: Syntax API 사용
        predicate: (node, cancellationToken) =>
        {
            // node가 클래스인지 확인
            return node is ClassDeclarationSyntax classDecl
                // public 클래스인지 확인 (Syntax만으로 판단)
                && classDecl.Modifiers.Any(SyntaxKind.PublicKeyword);
        },
        transform: (ctx, ct) => /* ... */
    );
```

### 실제 코드: Selectors.cs

```csharp
// Functorium 프로젝트의 Selectors.cs
namespace Functorium.Adapters.SourceGenerator.Abstractions;

public static class Selectors
{
    /// <summary>
    /// 노드가 클래스 선언인지 확인합니다.
    /// </summary>
    public static bool IsClass(SyntaxNode node, CancellationToken cancellationToken)
        => node is ClassDeclarationSyntax;

    /// <summary>
    /// 노드가 인터페이스 선언인지 확인합니다.
    /// </summary>
    public static bool IsInterface(SyntaxNode node, CancellationToken cancellationToken)
        => node is InterfaceDeclarationSyntax;
}
```

---

## SyntaxToken 활용

### 토큰 정보 접근

```csharp
var classDecl = root
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

// 클래스 이름 토큰
SyntaxToken identifier = classDecl.Identifier;
Console.WriteLine($"이름: {identifier.Text}");        // User
Console.WriteLine($"위치: {identifier.SpanStart}");   // 문자 위치
Console.WriteLine($"종류: {identifier.Kind()}");      // IdentifierToken

// 수정자 토큰들
var modifiers = classDecl.Modifiers;
foreach (var modifier in modifiers)
{
    Console.WriteLine($"수정자: {modifier.Text}");  // public
}
```

### 특정 수정자 확인

```csharp
// public 여부 확인
bool isPublic = classDecl.Modifiers.Any(SyntaxKind.PublicKeyword);

// partial 여부 확인
bool isPartial = classDecl.Modifiers.Any(SyntaxKind.PartialKeyword);

// abstract 여부 확인
bool isAbstract = classDecl.Modifiers.Any(SyntaxKind.AbstractKeyword);
```

---

## SyntaxTrivia 활용

주석이나 공백 정보가 필요할 때 사용합니다:

```csharp
string code = """
    /// <summary>
    /// 사용자 정보
    /// </summary>
    public class User { }
    """;

var tree = CSharpSyntaxTree.ParseText(code);
var classDecl = tree.GetRoot()
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

// public 키워드 앞의 trivia (주석 포함)
var leadingTrivia = classDecl.GetLeadingTrivia();

foreach (var trivia in leadingTrivia)
{
    if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
    {
        Console.WriteLine($"문서 주석 발견: {trivia}");
    }
}
```

---

## 패턴 매칭과 Syntax API

C#의 패턴 매칭으로 Syntax 노드를 쉽게 분석할 수 있습니다:

```csharp
// 메서드 분석
void AnalyzeMethod(SyntaxNode node)
{
    if (node is MethodDeclarationSyntax method)
    {
        // 메서드 이름
        var name = method.Identifier.Text;

        // 반환 타입 (Syntax 수준)
        var returnType = method.ReturnType switch
        {
            PredefinedTypeSyntax predefined => predefined.Keyword.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            GenericNameSyntax generic => $"{generic.Identifier}<...>",
            _ => "unknown"
        };

        // 파라미터 목록
        var parameters = method.ParameterList.Parameters
            .Select(p => $"{p.Type} {p.Identifier}")
            .ToList();

        Console.WriteLine($"{returnType} {name}({string.Join(", ", parameters)})");
    }
}
```

---

## Syntax API의 한계

Syntax API만으로는 **타입 정보**를 알 수 없습니다:

```csharp
string code = """
    public class Example
    {
        public void Process(User user) { }
    }
    """;

var method = tree.GetRoot()
    .DescendantNodes()
    .OfType<MethodDeclarationSyntax>()
    .First();

var parameter = method.ParameterList.Parameters.First();

// Syntax만으로 알 수 있는 것
Console.WriteLine(parameter.Type!.ToString());  // "User" (문자열)
Console.WriteLine(parameter.Identifier.Text);   // "user"

// Syntax만으로 알 수 없는 것
// - User가 클래스인지 인터페이스인지?
// - User의 네임스페이스는?
// - User가 어느 어셈블리에 정의되어 있는지?
// → 이런 정보는 Semantic API가 필요
```

---

## 요약

| 구성 요소 | 역할 | 예시 |
|-----------|------|------|
| SyntaxNode | 문법 구조 | ClassDeclarationSyntax |
| SyntaxToken | 최소 문법 단위 | `public`, `User` |
| SyntaxTrivia | 공백, 주석 | 스페이스, `// 주석` |

| 탐색 메서드 | 설명 |
|-------------|------|
| `DescendantNodes()` | 모든 자손 노드 |
| `ChildNodes()` | 직접 자식만 |
| `Ancestors()` | 모든 부모 노드 |
| `GetLeadingTrivia()` | 앞쪽 trivia |
| `GetTrailingTrivia()` | 뒤쪽 trivia |

---

## 다음 단계

다음 섹션에서는 타입 정보를 제공하는 Semantic API를 학습합니다.

➡️ [03. Semantic API](03-semantic-api.md)
