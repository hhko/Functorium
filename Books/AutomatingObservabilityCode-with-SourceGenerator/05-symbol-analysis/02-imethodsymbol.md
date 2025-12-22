# IMethodSymbol

## 학습 목표

- IMethodSymbol로 메서드 시그니처 분석
- Parameters, ReturnType, MethodKind 활용
- 관찰 가능성 코드 생성에 필요한 정보 추출

---

## IMethodSymbol이란?

**IMethodSymbol**은 **메서드, 생성자, 소멸자, 연산자** 등을 나타내는 심볼입니다.

```csharp
// 인터페이스에서 메서드 심볼 얻기
var methods = interfaceSymbol.GetMembers()
    .OfType<IMethodSymbol>()
    .Where(m => m.MethodKind == MethodKind.Ordinary);
```

---

## 기본 정보 추출

### 이름과 종류

```csharp
IMethodSymbol method = ...;

// 메서드 이름
string name = method.Name;  // "GetUserAsync"

// 메서드 종류
MethodKind kind = method.MethodKind;
// Ordinary: 일반 메서드
// Constructor: 생성자
// PropertyGet: getter
// PropertySet: setter
// 등등
```

### MethodKind 주요 값

```csharp
MethodKind.Ordinary              // 일반 메서드
MethodKind.Constructor           // 생성자
MethodKind.StaticConstructor     // 정적 생성자
MethodKind.Destructor            // 소멸자 (Finalizer)
MethodKind.PropertyGet           // 프로퍼티 getter
MethodKind.PropertySet           // 프로퍼티 setter
MethodKind.EventAdd              // 이벤트 add
MethodKind.EventRemove           // 이벤트 remove
MethodKind.ExplicitInterfaceImplementation  // 명시적 인터페이스 구현

// 소스 생성기에서 일반 메서드만 필터링
.Where(m => m.MethodKind == MethodKind.Ordinary)
```

### 수정자

```csharp
// 접근성
Accessibility accessibility = method.DeclaredAccessibility;

// 정적 여부
bool isStatic = method.IsStatic;

// 가상/추상/오버라이드
bool isVirtual = method.IsVirtual;
bool isAbstract = method.IsAbstract;
bool isOverride = method.IsOverride;

// 비동기
bool isAsync = method.IsAsync;

// 확장 메서드
bool isExtension = method.IsExtensionMethod;
```

---

## 반환 타입 분석

### ReturnType

```csharp
IMethodSymbol method = ...;

// 반환 타입 심볼
ITypeSymbol returnType = method.ReturnType;

// void 여부
bool returnsVoid = method.ReturnsVoid;

// 타입 이름 (결정적 포맷)
string returnTypeName = method.ReturnType.ToDisplayString(
    SymbolDisplayFormats.GlobalQualifiedFormat);
// "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.Models.User>"
```

### 반환 타입에서 실제 타입 추출

관찰 가능성 코드에서는 `FinT<IO, T>`의 `T`가 필요합니다:

```csharp
// TypeExtractor.cs
public static class TypeExtractor
{
    /// <summary>
    /// FinT&lt;IO, User&gt;에서 User를 추출합니다.
    /// </summary>
    public static string ExtractSecondTypeParameter(string genericTypeName)
    {
        // "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>"
        // → "global::MyApp.User"

        int firstComma = genericTypeName.IndexOf(',');
        if (firstComma == -1) return genericTypeName;

        int lastAngle = genericTypeName.LastIndexOf('>');
        if (lastAngle == -1) return genericTypeName;

        // 첫 번째 쉼표 이후, 마지막 > 이전 문자열
        return genericTypeName
            .Substring(firstComma + 1, lastAngle - firstComma - 1)
            .Trim();
    }
}

// 사용 예
string returnType = "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>";
string actualType = TypeExtractor.ExtractSecondTypeParameter(returnType);
// → "global::MyApp.User"
```

---

## 파라미터 분석

### Parameters

```csharp
IMethodSymbol method = ...;

// 파라미터 목록
ImmutableArray<IParameterSymbol> parameters = method.Parameters;

foreach (var param in parameters)
{
    Console.WriteLine($"이름: {param.Name}");
    Console.WriteLine($"타입: {param.Type}");
    Console.WriteLine($"RefKind: {param.RefKind}");
    Console.WriteLine($"순서: {param.Ordinal}");
}
```

### IParameterSymbol 상세

```csharp
IParameterSymbol param = ...;

// 기본 정보
string name = param.Name;           // "userId"
ITypeSymbol type = param.Type;      // int
int ordinal = param.Ordinal;        // 0, 1, 2...

// RefKind
RefKind refKind = param.RefKind;
// None: 일반 파라미터
// Ref: ref 파라미터
// Out: out 파라미터
// In: in 파라미터

// 기본값
bool hasDefault = param.HasExplicitDefaultValue;
object? defaultValue = param.ExplicitDefaultValue;

// 특수
bool isParams = param.IsParams;      // params 배열
bool isOptional = param.IsOptional;  // 선택적 파라미터
bool isThis = param.IsThis;          // 확장 메서드의 this
```

---

## 실제 활용: MethodInfo 생성

```csharp
// AdapterPipelineGenerator.cs에서 메서드 정보 추출
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIAdapter)
    .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())
    .Where(m => m.MethodKind == MethodKind.Ordinary)
    .Select(m => new MethodInfo(
        // 1. 메서드 이름
        m.Name,

        // 2. 파라미터 목록
        m.Parameters.Select(p => new ParameterInfo(
            p.Name,
            p.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat),
            p.RefKind)).ToList(),

        // 3. 반환 타입
        m.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat)))
    .ToList();
```

### MethodInfo 레코드

```csharp
// Generators/AdapterPipelineGenerator/MethodInfo.cs
public sealed record MethodInfo(
    string Name,
    List<ParameterInfo> Parameters,
    string ReturnType);

// Generators/AdapterPipelineGenerator/ParameterInfo.cs
public sealed record ParameterInfo(
    string Name,
    string Type,
    RefKind RefKind);
```

---

## 로깅 코드 생성 시 파라미터 활용

### 파라미터 개수에 따른 처리

```csharp
// LoggerMessage.Define은 최대 6개 파라미터만 지원
const int MaxLoggerMessageParameters = 6;

// 기본 파라미터 4개:
// - requestHandler (클래스 이름)
// - requestHandlerMethod (메서드 이름)
// - layer (Adapter)
// - 응답 관련 (elapsed, status)

// 메서드 파라미터로 사용 가능한 슬롯: 6 - 4 = 2개

int methodParameterCount = method.Parameters.Length;
bool canUseLoggerMessageDefine = methodParameterCount <= 2;

if (canUseLoggerMessageDefine)
{
    // 고성능 로깅 코드 생성
    GenerateLoggerMessageDefine(method);
}
else
{
    // 폴백: 일반 로깅
    GenerateFallbackLogging(method);
}
```

### 파라미터 문자열 생성

```csharp
// 메서드 시그니처용 파라미터 목록
string parameterList = string.Join(", ",
    method.Parameters.Select(p =>
        $"{GetRefKindKeyword(p.RefKind)}{p.Type} {p.Name}".Trim()));

// 호출용 파라미터 목록
string argumentList = string.Join(", ",
    method.Parameters.Select(p =>
        $"{GetRefKindKeyword(p.RefKind)}{p.Name}".Trim()));

// ref, out, in 키워드 처리
static string GetRefKindKeyword(RefKind refKind) => refKind switch
{
    RefKind.Ref => "ref ",
    RefKind.Out => "out ",
    RefKind.In => "in ",
    _ => ""
};
```

---

## 제네릭 메서드

```csharp
IMethodSymbol method = ...;

// 제네릭 메서드 여부
bool isGeneric = method.IsGenericMethod;

// 타입 파라미터
var typeParams = method.TypeParameters;  // [T, TResult]

// 타입 인수 (바인딩된 경우)
var typeArgs = method.TypeArguments;  // [int, string]

// 원본 정의
var original = method.OriginalDefinition;
```

---

## 메서드 호출 코드 생성

```csharp
// 생성되는 파이프라인 메서드 예시
public new FinT<IO, User> GetUserAsync(int userId)
{
    long startTimestamp = Stopwatch.GetTimestamp();

    return ExecuteWithActivity(
        RequestHandler,           // "UserRepository"
        nameof(GetUserAsync),     // 메서드 이름
        FinTToIO(base.GetUserAsync(userId)),  // 실제 호출
        () => LogRequest(userId), // 요청 로깅
        LogResponseSuccess,       // 성공 로깅
        LogResponseFailure,       // 실패 로깅
        startTimestamp);
}
```

---

## 요약

| 속성/메서드 | 용도 | 반환 |
|-------------|------|------|
| `Name` | 메서드 이름 | string |
| `MethodKind` | 메서드 종류 | MethodKind |
| `ReturnType` | 반환 타입 | ITypeSymbol |
| `ReturnsVoid` | void 반환 여부 | bool |
| `Parameters` | 파라미터 목록 | ImmutableArray |
| `IsAsync` | async 여부 | bool |
| `IsStatic` | static 여부 | bool |

| 파라미터 속성 | 용도 |
|---------------|------|
| `Name` | 파라미터 이름 |
| `Type` | 파라미터 타입 |
| `RefKind` | ref/out/in 여부 |
| `Ordinal` | 순서 (0부터) |

---

## 다음 단계

다음 섹션에서는 결정적 코드 생성을 위한 SymbolDisplayFormat을 학습합니다.

➡️ [03. SymbolDisplayFormat](03-symboldisplayformat.md)
