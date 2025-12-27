# Roslyn 아키텍처

## 학습 목표

- Roslyn 컴파일러 플랫폼의 전체 구조 이해
- 컴파일 파이프라인의 각 단계 파악
- 소스 생성기가 개입하는 시점 이해

---

## Roslyn이란?

**Roslyn**은 .NET 컴파일러 플랫폼(NET Compiler Platform)의 코드명으로, C# 및 Visual Basic 컴파일러를 오픈 소스로 재작성한 프로젝트입니다.

```
기존 컴파일러 (csc.exe)
======================
소스 코드 → [블랙박스] → IL 코드

Roslyn 컴파일러
===============
소스 코드 → [공개된 API] → IL 코드
              ↑
         개발자가 접근 가능한
         Syntax Tree, Semantic Model 등
```

### 핵심 특징

| 특징 | 설명 |
|------|------|
| API 공개 | 컴파일러 내부 데이터 구조에 접근 가능 |
| 확장성 | 분석기, 소스 생성기 등 확장 가능 |
| IDE 통합 | Visual Studio의 IntelliSense, 리팩터링 기반 |

---

## 컴파일 파이프라인

Roslyn 컴파일러는 소스 코드를 여러 단계를 거쳐 IL 코드로 변환합니다:

```
                    C# 소스 코드 (.cs)
                           │
                           ▼
┌──────────────────────────────────────────────────────┐
│  1단계: 어휘 분석 (Lexical Analysis)                 │
│  ─────────────────────────────────────               │
│  소스 텍스트 → 토큰(Token) 시퀀스                    │
│                                                      │
│  예: "int x = 5;" → [int] [x] [=] [5] [;]            │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────┐
│  2단계: 구문 분석 (Syntax Analysis)                  │
│  ─────────────────────────────────                   │
│  토큰 → Syntax Tree (구문 트리)                      │
│                                                      │
│  예: VariableDeclaration                             │
│       ├── Type: "int"                                │
│       └── Declarator: "x = 5"                        │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────┐
│  3단계: 의미 분석 (Semantic Analysis)                │
│  ─────────────────────────────────                   │
│  Syntax Tree + 타입 정보 → Semantic Model            │
│                                                      │
│  예: "x"는 int 타입, "5"는 int 리터럴                │
│      할당 호환성 검증 통과                           │
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────┐
│  4단계: 소스 생성기 실행 ★                           │
│  ───────────────────────                             │
│  Syntax Tree + Semantic Model을 분석하여             │
│  새로운 소스 코드 생성                               │
│                                                      │
│  예: [GeneratePipeline] → UserRepositoryPipeline.g.cs│
└────────────────────────┬─────────────────────────────┘
                         │
                         ▼
┌──────────────────────────────────────────────────────┐
│  5단계: IL 생성 (Emit)                               │
│  ─────────────────────                               │
│  Semantic Model → IL 바이트코드                      │
│                                                      │
│  결과: .dll 또는 .exe 파일                           │
└──────────────────────────────────────────────────────┘
```

---

## 핵심 개념

### Syntax Tree (구문 트리)

소스 코드의 **구조적 표현**입니다. 코드의 모든 문자를 포함하여 원본을 완벽히 복원할 수 있습니다.

```csharp
// 소스 코드
public class User
{
    public int Id { get; set; }
}
```

```
Syntax Tree
===========

CompilationUnit
└── ClassDeclaration "User"
    ├── Modifier: "public"
    └── Members
        └── PropertyDeclaration "Id"
            ├── Type: "int"
            ├── Modifier: "public"
            └── Accessors: { get; set; }
```

### Semantic Model (의미 모델)

Syntax Tree에 **타입 정보**를 추가한 것입니다. 변수의 타입, 메서드의 반환 타입 등을 조회할 수 있습니다.

```csharp
// 소스 코드
var user = new User();
user.Id = 5;

// Syntax만으로는 알 수 없는 정보
// - "user"의 타입이 무엇인가? → Semantic Model: User
// - "Id"가 어느 클래스에 정의되어 있나? → Semantic Model: User.Id
// - "5"를 "Id"에 할당 가능한가? → Semantic Model: int → int, 가능
```

### Symbol (심볼)

코드에서 **이름이 있는 엔티티**를 나타냅니다. 클래스, 메서드, 프로퍼티, 파라미터 등이 모두 심볼입니다.

```
심볼 계층 구조
=============

ISymbol (기본)
├── INamespaceSymbol      (네임스페이스)
├── INamedTypeSymbol      (클래스, 인터페이스, 구조체)
├── IMethodSymbol         (메서드, 생성자)
├── IPropertySymbol       (프로퍼티)
├── IFieldSymbol          (필드)
├── IParameterSymbol      (파라미터)
└── ILocalSymbol          (지역 변수)
```

---

## Roslyn API 구조

```
Microsoft.CodeAnalysis (기본)
├── SyntaxTree             구문 트리
├── SyntaxNode             구문 노드 (기본 클래스)
├── SyntaxToken            토큰 (키워드, 식별자 등)
├── SyntaxTrivia           공백, 주석 등
├── Compilation            컴파일 단위
├── SemanticModel          의미 모델
└── ISymbol                심볼 인터페이스

Microsoft.CodeAnalysis.CSharp (C# 전용)
├── CSharpSyntaxTree       C# 구문 트리
├── CSharpCompilation      C# 컴파일
└── CSharpSyntaxNode       C# 구문 노드 (기본 클래스)
    ├── ClassDeclarationSyntax
    ├── MethodDeclarationSyntax
    ├── PropertyDeclarationSyntax
    └── ... (수백 개의 구문 노드)
```

---

## 소스 생성기와 Roslyn

소스 생성기는 Roslyn API를 통해 **컴파일 중인 코드를 분석**하고 **새 코드를 추가**합니다.

### 접근 가능한 정보

```csharp
// IIncrementalGenerator.Initialize에서 접근 가능한 정보
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    // 1. Syntax Provider - 구문 트리 기반 필터링
    context.SyntaxProvider
        .ForAttributeWithMetadataName(
            "MyAttribute",                                    // 속성 이름
            predicate: (node, _) => node is ClassDeclarationSyntax,  // 구문 필터
            transform: (ctx, _) => {
                // 2. 여기서 Semantic Model에 접근 가능
                var symbol = ctx.TargetSymbol;                // ISymbol
                var semanticModel = ctx.SemanticModel;        // SemanticModel
                return symbol;
            });
}
```

### 접근 불가능한 정보

```
소스 생성기에서 접근 불가능한 것들
================================

✗ 파일 시스템 (File.ReadAllText 등)
✗ 네트워크 (HttpClient 등)
✗ 데이터베이스
✗ 환경 변수 (제한적)
✗ 다른 어셈블리의 소스 코드

이유: 결정적(Deterministic) 출력을 보장하기 위해
     동일한 소스 코드 → 항상 동일한 생성 결과
```

---

## Compilation 개념

`Compilation`은 **컴파일 단위 전체**를 나타냅니다.

```csharp
// Compilation 생성
var compilation = CSharpCompilation.Create(
    assemblyName: "MyAssembly",
    syntaxTrees: [syntaxTree1, syntaxTree2],        // 여러 파일
    references: [                                   // 참조 어셈블리
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
    ],
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
);

// Compilation에서 얻을 수 있는 정보
var globalNamespace = compilation.GlobalNamespace;  // 전역 네임스페이스
var allTypes = compilation.GetTypeByMetadataName("MyNamespace.MyClass");  // 특정 타입
```

---

## 실습: 간단한 Syntax Tree 분석

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// 1. 소스 코드 파싱
string code = """
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    """;

SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
SyntaxNode root = tree.GetRoot();

// 2. 클래스 선언 찾기
var classDeclaration = root
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .First();

Console.WriteLine($"클래스 이름: {classDeclaration.Identifier}");
// 출력: 클래스 이름: User

// 3. 프로퍼티 목록 출력
var properties = classDeclaration
    .DescendantNodes()
    .OfType<PropertyDeclarationSyntax>();

foreach (var prop in properties)
{
    Console.WriteLine($"프로퍼티: {prop.Type} {prop.Identifier}");
}
// 출력:
// 프로퍼티: int Id
// 프로퍼티: string Name
```

---

## 요약

| 개념 | 설명 | 접근 방법 |
|------|------|-----------|
| Syntax Tree | 코드의 구조적 표현 | `SyntaxTree.GetRoot()` |
| Semantic Model | 타입 정보가 추가된 모델 | `Compilation.GetSemanticModel()` |
| Symbol | 이름 있는 엔티티 | `SemanticModel.GetSymbolInfo()` |
| Compilation | 컴파일 단위 전체 | `CSharpCompilation.Create()` |

---

## 다음 단계

다음 섹션에서는 Syntax API를 더 자세히 살펴봅니다.

➡️ [02. Syntax API](02-syntax-api.md)
