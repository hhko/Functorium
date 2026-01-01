# 타입 추출

## 학습 목표

- 제네릭 타입에서 타입 파라미터 추출
- TypeExtractor 유틸리티 이해
- FinT<IO, T>에서 T 추출하는 방법 학습

---

## 타입 추출이 필요한 이유

관찰 가능성 코드에서는 반환 타입의 **실제 값 타입**이 필요합니다:

```csharp
// 원본 메서드
public FinT<IO, User> GetUserAsync(int id);

// 생성되는 로깅 코드
void LogSuccess(User result, double elapsed)
//              ^^^^
// FinT<IO, User>가 아닌 User가 필요!
```

---

## TypeExtractor 클래스

### 전체 구현

```csharp
// Generators/AdapterPipelineGenerator/TypeExtractor.cs
namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 제네릭 타입에서 특정 타입 파라미터를 추출하는 유틸리티
/// </summary>
public static class TypeExtractor
{
    /// <summary>
    /// 두 번째 타입 파라미터를 추출합니다.
    /// 예: FinT&lt;IO, User&gt; → User
    /// </summary>
    public static string ExtractSecondTypeParameter(string genericTypeName)
    {
        // 입력: "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>"
        // 출력: "global::MyApp.User"

        int firstComma = genericTypeName.IndexOf(',');
        if (firstComma == -1)
        {
            return genericTypeName;  // 제네릭이 아닌 경우 원본 반환
        }

        int lastAngle = genericTypeName.LastIndexOf('>');
        if (lastAngle == -1)
        {
            return genericTypeName;  // 잘못된 형식인 경우 원본 반환
        }

        // 첫 번째 쉼표 이후, 마지막 > 이전
        return genericTypeName
            .Substring(firstComma + 1, lastAngle - firstComma - 1)
            .Trim();
    }
}
```

### 동작 예시

```
입력: "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>"
                                     ^                 ^
                               firstComma         lastAngle

추출: ", global::MyApp.User>"
      → "global::MyApp.User" (Trim 후)
```

---

## 다양한 타입 처리

### 단순 타입

```csharp
// 입력: "global::LanguageExt.FinT<global::LanguageExt.IO, int>"
// 출력: "int"

// 입력: "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.String>"
// 출력: "global::System.String"
```

### 컬렉션 타입

```csharp
// 입력: "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.List<global::MyApp.User>>"
// 출력: "global::System.Collections.Generic.List<global::MyApp.User>"
```

### 튜플 타입

```csharp
// 입력: "global::LanguageExt.FinT<global::LanguageExt.IO, (int, string)>"
// 출력: "(int, string)"
```

### 중첩 제네릭

```csharp
// 입력: "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int>>>"
// 출력: "global::System.Collections.Generic.Dictionary<string, global::System.Collections.Generic.List<int>>"
```

---

## 실제 활용

### 로깅 메서드 생성

```csharp
// AdapterPipelineGenerator.cs에서
private static void GenerateMethod(
    StringBuilder sb,
    PipelineClassInfo classInfo,
    MethodInfo method)
{
    // 반환 타입에서 실제 타입 추출
    string actualReturnType = ExtractActualReturnType(method.ReturnType);
    // "global::MyApp.User"

    // 로깅 콜백 시그니처 생성
    sb.AppendLine($"    private void Log{method.Name}Success(");
    sb.AppendLine($"        string requestHandler,");
    sb.AppendLine($"        string requestHandlerMethod,");
    sb.AppendLine($"        {actualReturnType} result,");  // ← 추출된 타입 사용
    sb.AppendLine($"        double elapsedMilliseconds) {{ ... }}");
}

private static string ExtractActualReturnType(string returnType)
{
    return TypeExtractor.ExtractSecondTypeParameter(returnType);
}
```

### 생성되는 코드

```csharp
// 원본 인터페이스
public interface IUserRepository : IAdapter
{
    FinT<IO, User> GetUserAsync(int id);
    FinT<IO, IEnumerable<User>> GetUsersAsync();
}

// 생성되는 코드
public class UserRepositoryPipeline : UserRepository
{
    // GetUserAsync의 성공 로깅 - User 타입 사용
    private void LogGetUserAsyncSuccess(
        string requestHandler,
        string requestHandlerMethod,
        global::MyApp.User result,        // ← 추출됨
        double elapsedMilliseconds) { }

    // GetUsersAsync의 성공 로깅 - IEnumerable<User> 타입 사용
    private void LogGetUsersAsyncSuccess(
        string requestHandler,
        string requestHandlerMethod,
        global::System.Collections.Generic.IEnumerable<global::MyApp.User> result,  // ← 추출됨
        double elapsedMilliseconds) { }
}
```

---

## 대안: ITypeSymbol 직접 사용

문자열 파싱 대신 심볼 API를 사용할 수도 있습니다:

```csharp
// ITypeSymbol을 직접 사용하는 방법
ITypeSymbol returnType = method.ReturnType;

if (returnType is INamedTypeSymbol namedType
    && namedType.IsGenericType
    && namedType.TypeArguments.Length >= 2)
{
    // 두 번째 타입 인수 직접 접근
    ITypeSymbol secondTypeArg = namedType.TypeArguments[1];
    string actualType = secondTypeArg.ToDisplayString(
        SymbolDisplayFormats.GlobalQualifiedFormat);
}
```

### 문자열 파싱 vs 심볼 API

| 방법 | 장점 | 단점 |
|------|------|------|
| 문자열 파싱 | 단순, 이미 변환된 문자열 사용 | 복잡한 타입에서 실패 가능 |
| 심볼 API | 정확, 타입 안전 | 추가 처리 필요 |

**Functorium**은 이미 문자열로 변환된 타입을 사용하므로 문자열 파싱을 선택했습니다.

---

## 엣지 케이스 처리

### 비제네릭 타입

```csharp
// 입력: "void"
// indexOf(',') == -1 → 원본 반환

public static string ExtractSecondTypeParameter(string genericTypeName)
{
    int firstComma = genericTypeName.IndexOf(',');
    if (firstComma == -1)
    {
        return genericTypeName;  // 제네릭이 아님
    }
    // ...
}
```

### 단일 타입 파라미터

```csharp
// 입력: "global::LanguageExt.Fin<global::MyApp.User>"
// indexOf(',') == -1 → 원본 반환
```

### Nullable 타입

```csharp
// 입력: "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User?>"
// 출력: "global::MyApp.User?"

// ? 마커가 유지됨
```

---

## 테스트 케이스

```csharp
public class TypeExtractorTests
{
    [Theory]
    [InlineData(
        "global::LanguageExt.FinT<global::LanguageExt.IO, int>",
        "int")]
    [InlineData(
        "global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>",
        "global::MyApp.User")]
    [InlineData(
        "global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.List<int>>",
        "global::System.Collections.Generic.List<int>")]
    [InlineData(
        "global::LanguageExt.FinT<global::LanguageExt.IO, (int, string)>",
        "(int, string)")]
    [InlineData(
        "void",
        "void")]  // 비제네릭은 원본 반환
    public void ExtractSecondTypeParameter_Should_Work(
        string input,
        string expected)
    {
        var actual = TypeExtractor.ExtractSecondTypeParameter(input);
        actual.ShouldBe(expected);
    }
}
```

---

## 요약

| 상황 | 입력 | 출력 |
|------|------|------|
| 단순 값 타입 | `FinT<IO, int>` | `int` |
| 참조 타입 | `FinT<IO, User>` | `User` |
| 컬렉션 | `FinT<IO, List<User>>` | `List<User>` |
| 튜플 | `FinT<IO, (int, string)>` | `(int, string)` |
| 비제네릭 | `void` | `void` |

---

## 다음 단계

다음 장에서는 코드 생성 기법을 학습합니다.

➡️ [06장. 코드 생성](../06-code-generation/)
