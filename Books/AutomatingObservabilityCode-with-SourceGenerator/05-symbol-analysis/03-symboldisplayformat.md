# SymbolDisplayFormat

## 학습 목표

- SymbolDisplayFormat의 역할 이해
- 결정적(Deterministic) 타입 문자열 생성 방법 습득
- 커스텀 포맷 구성 학습

---

## 왜 SymbolDisplayFormat이 중요한가?

동일한 타입이 **다르게 표현**될 수 있습니다:

```csharp
// 모두 같은 타입이지만 다른 문자열
"User"
"MyApp.User"
"MyApp.Models.User"
"global::MyApp.Models.User"

// 문제: 캐싱 무효화
// 빌드 A에서: "User" → UserPipeline.g.cs 생성
// 빌드 B에서: "MyApp.User" → 다른 파일로 인식 → 캐시 미스
```

**SymbolDisplayFormat**을 사용하면 **항상 동일한 형식**의 문자열을 얻을 수 있습니다.

---

## 기본 사용법

### ToDisplayString()

```csharp
ITypeSymbol type = ...;

// 기본 포맷 (컨텍스트에 따라 다름)
string name1 = type.ToDisplayString();
// "User" 또는 "MyApp.User" (상황에 따라)

// 전체 한정 포맷 (권장)
string name2 = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
// "global::MyApp.Models.User"

// 최소 한정 포맷
string name3 = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
// "User"
```

---

## 기본 제공 포맷

### FullyQualifiedFormat

```csharp
SymbolDisplayFormat.FullyQualifiedFormat

// 특징:
// - global:: 접두사 포함
// - 전체 네임스페이스 포함
// - 제네릭 타입 파라미터 포함

// 예시:
// List<int> → "global::System.Collections.Generic.List<global::System.Int32>"
// User → "global::MyApp.Models.User"
```

### MinimallyQualifiedFormat

```csharp
SymbolDisplayFormat.MinimallyQualifiedFormat

// 특징:
// - 가장 짧은 형태
// - using 문에 따라 달라질 수 있음

// 예시:
// List<int> → "List<int>"
// User → "User"
```

### CSharpErrorMessageFormat

```csharp
SymbolDisplayFormat.CSharpErrorMessageFormat

// 특징:
// - 에러 메시지에 적합한 형태
// - 사람이 읽기 좋은 형태

// 예시:
// List<int> → "System.Collections.Generic.List<int>"
```

---

## 커스텀 포맷 구성

### SymbolDisplayFormat 생성자

```csharp
var customFormat = new SymbolDisplayFormat(
    globalNamespaceStyle: ...,       // global:: 접두사
    typeQualificationStyle: ...,     // 네임스페이스 표시 방식
    genericsOptions: ...,            // 제네릭 표시 방식
    memberOptions: ...,              // 멤버 표시 방식
    parameterOptions: ...,           // 파라미터 표시 방식
    miscellaneousOptions: ...        // 기타 옵션
);
```

### GlobalNamespaceStyle

```csharp
// global:: 접두사 제어
SymbolDisplayGlobalNamespaceStyle.Omitted      // 생략
SymbolDisplayGlobalNamespaceStyle.Included     // 포함 (권장)
SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining
```

### TypeQualificationStyle

```csharp
// 네임스페이스 표시 방식
SymbolDisplayTypeQualificationStyle.NameOnly
// "User"

SymbolDisplayTypeQualificationStyle.NameAndContainingTypes
// "Models.User" (중첩 클래스인 경우)

SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
// "MyApp.Models.User" (권장)
```

### GenericsOptions

```csharp
// 제네릭 표시 방식
SymbolDisplayGenericsOptions.None
// "List" (타입 파라미터 생략)

SymbolDisplayGenericsOptions.IncludeTypeParameters
// "List<T>" 또는 "List<int>"

SymbolDisplayGenericsOptions.IncludeTypeConstraints
// "List<T> where T : class"

SymbolDisplayGenericsOptions.IncludeVariance
// "IEnumerable<out T>"
```

### MiscellaneousOptions

```csharp
// 기타 옵션
SymbolDisplayMiscellaneousOptions.UseSpecialTypes
// "int" 대신 "System.Int32" (또는 반대)

SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers
// 키워드를 이스케이프 (@class, @event 등)

SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
// "string?" 표시
```

---

## Functorium의 GlobalQualifiedFormat

### SymbolDisplayFormats.cs

```csharp
namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 결정적 코드 생성을 위한 SymbolDisplayFormat 정의
/// </summary>
public static class SymbolDisplayFormats
{
    /// <summary>
    /// 전역 한정 포맷 - 결정적 코드 생성에 사용
    /// </summary>
    public static readonly SymbolDisplayFormat GlobalQualifiedFormat = new(
        // global:: 접두사 포함
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,

        // 전체 네임스페이스 포함
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,

        // 제네릭 타입 파라미터 포함
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,

        // 기타 옵션
        miscellaneousOptions:
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |      // int, string 등 사용
            SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |  // 키워드 이스케이프
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier  // ? 표시
    );
}
```

### 사용 예

```csharp
// 파라미터 타입
string paramType = param.Type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
// "global::System.Int32" 또는 "int" (UseSpecialTypes 때문에)

// 반환 타입
string returnType = method.ReturnType.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
// "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.Models.User>"
```

---

## 특수 타입 처리

### UseSpecialTypes 옵션

```csharp
// UseSpecialTypes 있음 (기본)
"int"
"string"
"bool"
"object"

// UseSpecialTypes 없음
"global::System.Int32"
"global::System.String"
"global::System.Boolean"
"global::System.Object"
```

### Nullable 타입

```csharp
// IncludeNullableReferenceTypeModifier 있음
"global::System.String?"
"global::MyApp.Models.User?"

// IncludeNullableReferenceTypeModifier 없음
"global::System.String"
"global::MyApp.Models.User"
```

---

## 결정적 출력 검증

### 동일 타입의 일관된 출력

```csharp
// 다양한 컨텍스트에서 같은 타입
var type1 = compilation1.GetTypeByMetadataName("MyApp.Models.User");
var type2 = compilation2.GetTypeByMetadataName("MyApp.Models.User");

string name1 = type1.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
string name2 = type2.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);

// 항상 동일해야 함
Debug.Assert(name1 == name2);
// → "global::MyApp.Models.User"
```

### 테스트로 검증

```csharp
[Fact]
public void TypeDisplayString_Should_Be_Deterministic()
{
    // Arrange
    string sourceCode = """
        namespace MyApp.Models;
        public class User { }
        """;

    // Act - 두 번 컴파일
    var type1 = CompileAndGetType(sourceCode, "MyApp.Models.User");
    var type2 = CompileAndGetType(sourceCode, "MyApp.Models.User");

    // Assert
    var format = SymbolDisplayFormats.GlobalQualifiedFormat;
    type1.ToDisplayString(format).ShouldBe(type2.ToDisplayString(format));
}
```

---

## 주의사항

### 1. 일관된 포맷 사용

```csharp
// ❌ 혼용 금지
var paramTypes = method.Parameters
    .Select(p => p.Type.ToDisplayString())  // 기본 포맷
    .ToList();

var returnType = method.ReturnType.ToDisplayString(
    SymbolDisplayFormat.FullyQualifiedFormat);  // 다른 포맷

// ✅ 항상 동일한 포맷
var format = SymbolDisplayFormats.GlobalQualifiedFormat;

var paramTypes = method.Parameters
    .Select(p => p.Type.ToDisplayString(format))
    .ToList();

var returnType = method.ReturnType.ToDisplayString(format);
```

### 2. 재사용 가능한 상수로 정의

```csharp
// ✅ 상수로 정의하여 재사용
public static class SymbolDisplayFormats
{
    public static readonly SymbolDisplayFormat GlobalQualifiedFormat = ...;
}

// 사용
type.ToDisplayString(SymbolDisplayFormats.GlobalQualifiedFormat);
```

---

## 요약

| 포맷 | 결과 예시 | 용도 |
|------|-----------|------|
| 기본 | "User" | 표시용 (비권장) |
| FullyQualifiedFormat | "global::MyApp.User" | 결정적 출력 |
| MinimallyQualifiedFormat | "User" | 간결한 표시 |
| 커스텀 GlobalQualifiedFormat | "global::MyApp.User" | **소스 생성기 권장** |

| 옵션 | 설명 |
|------|------|
| `GlobalNamespaceStyle.Included` | global:: 접두사 |
| `TypeQualificationStyle.NameAndContainingTypesAndNamespaces` | 전체 경로 |
| `GenericsOptions.IncludeTypeParameters` | 제네릭 파라미터 |
| `MiscellaneousOptions.UseSpecialTypes` | int, string 등 |

---

## 다음 단계

다음 섹션에서는 제네릭 타입 추출 기법을 학습합니다.

➡️ [04. 타입 추출](04-type-extraction.md)
