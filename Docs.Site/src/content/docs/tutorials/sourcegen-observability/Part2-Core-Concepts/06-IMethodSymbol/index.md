---
title: "IMethodSymbol"
---

## 개요

앞 장에서 `INamedTypeSymbol`을 통해 인터페이스의 멤버 목록을 가져왔습니다. 그 멤버 중 `IMethodSymbol`로 캐스팅되는 것들이 바로 코드 생성의 직접적인 대상입니다. ObservablePortGenerator는 각 메서드의 이름으로 로깅 메서드명을 결정하고, 파라미터 목록으로 `LoggerMessage.Define`의 타입 인수를 구성하며, 반환 타입에서 `FinT<IO, T>`의 `T`를 추출하여 성공 로깅 시그니처를 생성합니다. 이번 장에서는 이 모든 과정의 토대가 되는 `IMethodSymbol` API를 살펴봅니다.

## 학습 목표

### 핵심 학습 목표
1. **IMethodSymbol의** 기본 속성으로 메서드 시그니처를 분석한다
   - Name, ReturnType, Parameters의 역할
2. **MethodKind를** 활용하여 일반 메서드만 필터링하는 이유를 이해한다
   - getter, setter, 생성자 등을 제외해야 하는 이유
3. **파라미터 정보를** 로깅 코드 생성에 활용하는 패턴을 학습한다
   - LoggerMessage.Define의 파라미터 슬롯 제한과 대응 전략

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

### MethodKind로 일반 메서드만 필터링하기

인터페이스의 `GetMembers()`는 프로퍼티의 getter/setter, 이벤트의 add/remove 접근자까지 포함한 모든 멤버를 반환합니다. 소스 생성기에서는 실제 비즈니스 로직을 담는 **일반 메서드(Ordinary)만** 필요하므로 `MethodKind`로 필터링합니다. 주요 값은 `Ordinary`(일반 메서드), `Constructor`(생성자), `PropertyGet`/`PropertySet`(프로퍼티 접근자), `EventAdd`/`EventRemove`(이벤트 접근자) 등이 있습니다.

```csharp
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

메서드의 파라미터 정보는 두 가지 용도로 사용됩니다. 첫째, 생성되는 래퍼 메서드의 시그니처를 구성합니다. 둘째, 로깅 메시지 템플릿에 파라미터 값을 포함할지 결정합니다. 특히 `LoggerMessage.Define`의 최대 6개 파라미터 제한 때문에, 메서드 파라미터 개수에 따라 고성능 로깅과 폴백 로깅 중 하나를 선택해야 합니다.

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

앞에서 살펴본 Name, Parameters, ReturnType이 하나로 조합되는 지점입니다. 아래 코드는 `IMethodSymbol`에서 `MethodInfo` 데이터 모델을 생성하는 우리 프로젝트의 실제 코드입니다.

```csharp
// ObservablePortGenerator.cs에서 메서드 정보 추출
var methods = classSymbol.AllInterfaces
    .Where(ImplementsIObservablePort)
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
// Generators/ObservablePortGenerator/MethodInfo.cs
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

// Generators/ObservablePortGenerator/ParameterInfo.cs
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

## 로깅 코드 생성 시 파라미터 활용

파라미터 분석이 실제로 어떤 영향을 미치는지 구체적으로 살펴봅니다. `LoggerMessage.Define`은 최대 6개의 타입 파라미터만 지원하는데, 관찰 가능성 로깅에서 기본적으로 4개 슬롯(핸들러명, 메서드명, 레이어, 상태 정보)을 사용합니다. 따라서 메서드 파라미터에 할당할 수 있는 슬롯은 2개뿐이며, 이 제한에 따라 코드 생성 전략이 달라집니다.

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

`IMethodSymbol`은 메서드 단위의 코드 생성에 필요한 모든 정보를 제공합니다. 우리 프로젝트에서는 `Name`으로 로깅 메서드명을, `Parameters`로 시그니처와 로깅 템플릿을, `ReturnType`에서 `FinT<IO, T>`의 `T`를 추출하여 성공 응답 타입을 결정합니다. `MethodKind == Ordinary` 필터링은 getter/setter 등의 접근자를 제외하기 위해 반드시 필요합니다.

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

`IMethodSymbol`에서 파라미터 타입과 반환 타입을 추출할 때 `ToDisplayString`을 사용했습니다. 그런데 같은 타입이라도 포맷에 따라 `"User"`, `"MyApp.User"`, `"global::MyApp.User"` 등 다르게 표현될 수 있습니다. 다음 장에서는 이 표현을 일관되게 유지하기 위한 `SymbolDisplayFormat`을 살펴봅니다.

→ [07. SymbolDisplayFormat](../07-SymbolDisplayFormat/)
