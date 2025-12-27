# 심볼 타입

## 학습 목표

- ISymbol 계층 구조 이해
- INamedTypeSymbol, IMethodSymbol 상세 학습
- 소스 생성기에서 활용하는 심볼 API 습득

---

## ISymbol 계층 구조

모든 심볼은 `ISymbol` 인터페이스를 기반으로 합니다:

```
ISymbol (기본 인터페이스)
│
├── INamespaceSymbol          네임스페이스
│
├── ITypeSymbol (추상)        타입
│   ├── INamedTypeSymbol      클래스, 인터페이스, 구조체, 열거형
│   ├── IArrayTypeSymbol      배열 타입
│   ├── IPointerTypeSymbol    포인터 타입
│   └── ITypeParameterSymbol  제네릭 타입 파라미터
│
├── IMethodSymbol             메서드, 생성자
├── IPropertySymbol           프로퍼티
├── IFieldSymbol              필드
├── IEventSymbol              이벤트
├── IParameterSymbol          파라미터
├── ILocalSymbol              지역 변수
└── IAliasSymbol              using 별칭
```

---

## ISymbol 공통 속성

```csharp
ISymbol symbol = ...;

// 기본 정보
symbol.Name                    // 이름
symbol.Kind                    // 심볼 종류 (SymbolKind 열거형)
symbol.ContainingNamespace     // 포함 네임스페이스
symbol.ContainingType          // 포함 타입 (멤버인 경우)
symbol.ContainingSymbol        // 포함 심볼 (부모)

// 접근성
symbol.DeclaredAccessibility   // Public, Private, Internal 등

// 메타데이터
symbol.IsStatic               // 정적 여부
symbol.IsAbstract             // 추상 여부
symbol.IsVirtual              // 가상 여부
symbol.IsOverride             // 오버라이드 여부
symbol.IsSealed               // sealed 여부

// 위치
symbol.Locations              // 소스 코드 위치들
symbol.DeclaringSyntaxReferences // 선언 Syntax 참조
```

---

## INamedTypeSymbol

**클래스, 인터페이스, 구조체, 열거형**을 나타냅니다.

### 기본 속성

```csharp
INamedTypeSymbol typeSymbol = ...;

// 타입 종류
typeSymbol.TypeKind          // Class, Interface, Struct, Enum, Delegate

// 이름 관련
typeSymbol.Name              // 짧은 이름
typeSymbol.MetadataName      // 메타데이터 이름 (제네릭 포함)
typeSymbol.ToDisplayString() // 전체 이름

// 네임스페이스
typeSymbol.ContainingNamespace
typeSymbol.ContainingNamespace.IsGlobalNamespace // 글로벌 여부

// 기본 타입
typeSymbol.BaseType          // 부모 클래스
typeSymbol.AllInterfaces     // 모든 인터페이스 (직접 + 상속)
typeSymbol.Interfaces        // 직접 구현한 인터페이스만
```

### 멤버 조회

```csharp
// 모든 멤버
var allMembers = typeSymbol.GetMembers();

// 특정 이름의 멤버
var namedMembers = typeSymbol.GetMembers("GetUser");

// 타입별 필터링
var methods = typeSymbol.GetMembers()
    .OfType<IMethodSymbol>();

var properties = typeSymbol.GetMembers()
    .OfType<IPropertySymbol>();

var constructors = typeSymbol.Constructors;  // 생성자들
```

### 제네릭 타입

```csharp
// 제네릭 여부
typeSymbol.IsGenericType     // List<T>의 경우 true
typeSymbol.TypeArguments     // 타입 인수 [int] for List<int>
typeSymbol.TypeParameters    // 타입 파라미터 [T] for List<T>

// 원본 정의
typeSymbol.OriginalDefinition // List<> (unbounded)

// 예시: Dictionary<string, int>
// TypeArguments: [string, int]
// TypeParameters: [TKey, TValue] (OriginalDefinition에서)
```

---

## IMethodSymbol

**메서드, 생성자, 소멸자, 연산자**를 나타냅니다.

### 기본 속성

```csharp
IMethodSymbol method = ...;

// 이름
method.Name                  // 메서드 이름

// 메서드 종류
method.MethodKind            // Ordinary, Constructor, PropertyGet, etc.

// 반환 타입
method.ReturnType            // ITypeSymbol
method.ReturnsVoid           // void 반환 여부

// 수정자
method.IsStatic              // 정적 여부
method.IsAsync               // async 여부
method.IsAbstract            // 추상 여부
method.IsVirtual             // 가상 여부
method.IsExtensionMethod     // 확장 메서드 여부
```

### MethodKind 열거형

```csharp
public enum MethodKind
{
    Ordinary,              // 일반 메서드
    Constructor,           // 생성자
    StaticConstructor,     // 정적 생성자
    Destructor,            // 소멸자
    PropertyGet,           // 프로퍼티 getter
    PropertySet,           // 프로퍼티 setter
    EventAdd,              // 이벤트 add
    EventRemove,           // 이벤트 remove
    ExplicitInterfaceImplementation,  // 명시적 인터페이스 구현
    Conversion,            // 변환 연산자
    UserDefinedOperator,   // 사용자 정의 연산자
    // ...
}
```

### 파라미터 분석

```csharp
// 파라미터 목록
foreach (var param in method.Parameters)
{
    Console.WriteLine($"이름: {param.Name}");
    Console.WriteLine($"타입: {param.Type}");
    Console.WriteLine($"RefKind: {param.RefKind}");    // None, Ref, Out, In
    Console.WriteLine($"기본값 있음: {param.HasExplicitDefaultValue}");

    if (param.HasExplicitDefaultValue)
    {
        Console.WriteLine($"기본값: {param.ExplicitDefaultValue}");
    }
}
```

### 제네릭 메서드

```csharp
// 제네릭 여부
method.IsGenericMethod
method.TypeArguments        // 타입 인수
method.TypeParameters       // 타입 파라미터
```

---

## 실제 활용: AdapterPipelineGenerator

### 메서드 정보 추출

```csharp
// AdapterPipelineGenerator.cs
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIAdapter)
    .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
    .Where(m => m.MethodKind == MethodKind.Ordinary)  // ★ 일반 메서드만
    .Select(m => new MethodInfo(
        m.Name,
        m.Parameters.Select(p => new ParameterInfo(
            p.Name,
            p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            p.RefKind)).ToList(),
        m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
    .ToList();
```

### 생성자 파라미터 추출

```csharp
// ConstructorParameterExtractor.cs
public static List<ParameterInfo> ExtractParameters(INamedTypeSymbol classSymbol)
{
    // 1. 클래스 자체의 생성자에서 파라미터 찾기
    var constructor = classSymbol.Constructors
        .Where(c => c.DeclaredAccessibility == Accessibility.Public)
        .OrderByDescending(c => c.Parameters.Length)  // 파라미터 많은 것 우선
        .FirstOrDefault();

    if (constructor is not null && constructor.Parameters.Length > 0)
    {
        return constructor.Parameters
            .Select(p => new ParameterInfo(
                p.Name,
                p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
                p.RefKind))
            .ToList();
    }

    // 2. 부모 클래스의 생성자에서 찾기
    if (classSymbol.BaseType is not null)
    {
        return ExtractParameters(classSymbol.BaseType);
    }

    return [];
}
```

### IAdapter 구현 확인

```csharp
private static bool ImplementsIAdapter(INamedTypeSymbol interfaceSymbol)
{
    // IAdapter 자체인지 확인
    if (interfaceSymbol.Name == "IAdapter")
    {
        return true;
    }

    // IAdapter를 상속받은 인터페이스인지 확인
    return interfaceSymbol.AllInterfaces.Any(i => i.Name == "IAdapter");
}
```

---

## IPropertySymbol

```csharp
IPropertySymbol property = ...;

// 기본 정보
property.Name
property.Type               // 프로퍼티 타입
property.IsIndexer          // 인덱서 여부

// Getter/Setter
property.GetMethod          // getter (IMethodSymbol?)
property.SetMethod          // setter (IMethodSymbol?)
property.IsReadOnly         // 읽기 전용 (setter 없음)
property.IsWriteOnly        // 쓰기 전용 (getter 없음)
```

---

## IParameterSymbol

```csharp
IParameterSymbol param = ...;

// 기본 정보
param.Name
param.Type
param.Ordinal               // 파라미터 순서 (0부터)

// RefKind
param.RefKind               // None, Ref, Out, In, RefReadOnlyParameter

// 기본값
param.HasExplicitDefaultValue
param.ExplicitDefaultValue

// 특수 파라미터
param.IsParams              // params 배열 여부
param.IsOptional            // 선택적 파라미터 여부
param.IsThis                // 확장 메서드의 this 파라미터
```

### RefKind 열거형

```csharp
public enum RefKind
{
    None,      // 일반 파라미터
    Ref,       // ref 파라미터
    Out,       // out 파라미터
    In,        // in 파라미터 (읽기 전용 ref)
    RefReadOnlyParameter  // ref readonly 파라미터
}
```

---

## SymbolDisplayFormat 활용

심볼을 문자열로 변환할 때 포맷을 지정할 수 있습니다:

```csharp
ITypeSymbol type = ...; // MyApp.Models.User

// 기본 포맷
type.ToDisplayString()
// → "User"

// 전체 이름 (네임스페이스 포함)
type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
// → "global::MyApp.Models.User"

// 커스텀 포맷 (소스 생성기에서 권장)
var format = new SymbolDisplayFormat(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    miscellaneousOptions:
        SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

type.ToDisplayString(format)
// → "global::MyApp.Models.User"
```

---

## 요약

| 심볼 타입 | 대표 멤버 | 용도 |
|-----------|-----------|------|
| `INamedTypeSymbol` | Name, AllInterfaces, GetMembers() | 클래스 분석 |
| `IMethodSymbol` | Name, ReturnType, Parameters | 메서드 분석 |
| `IPropertySymbol` | Type, GetMethod, SetMethod | 프로퍼티 분석 |
| `IParameterSymbol` | Name, Type, RefKind | 파라미터 분석 |

| 주요 조회 패턴 | 코드 |
|----------------|------|
| 모든 인터페이스 | `typeSymbol.AllInterfaces` |
| 모든 메서드 | `typeSymbol.GetMembers().OfType<IMethodSymbol>()` |
| 생성자 | `typeSymbol.Constructors` |
| 일반 메서드만 | `.Where(m => m.MethodKind == MethodKind.Ordinary)` |

---

## 다음 단계

다음 장에서는 IIncrementalGenerator 패턴을 학습합니다.

➡️ [04장. IIncrementalGenerator 패턴](../04-incremental-generator-pattern/)
