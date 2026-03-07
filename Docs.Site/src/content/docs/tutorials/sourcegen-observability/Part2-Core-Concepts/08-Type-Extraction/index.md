---
title: "Type 추출"
---

## 개요

앞 장에서 `SymbolDisplayFormat`으로 반환 타입을 `"global::LanguageExt.FinT<global::LanguageExt.IO, global::MyApp.User>"` 같은 일관된 문자열로 변환했습니다. 그런데 로깅 메서드의 성공 콜백에 필요한 것은 이 전체 문자열이 아니라 두 번째 타입 파라미터인 `User`뿐입니다. `FinT<IO, T>` 패턴은 Functorium의 모든 어댑터 메서드가 따르는 반환 타입 규약이므로, 이 타입 추출은 코드 생성의 필수 단계입니다.

## 학습 목표

### 핵심 학습 목표
1. **TypeExtractor의** 문자열 파싱 로직을 이해한다
   - 첫 번째 쉼표와 마지막 꺾쇠괄호를 기준으로 한 추출 방법
2. **다양한 타입 형태에** 대한 처리를 학습한다
   - 단순 타입, 컬렉션, 튜플, 중첩 제네릭, 비제네릭
3. **문자열 파싱 vs 심볼 API** 접근법의 트레이드오프를 이해한다

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
// Generators/ObservablePortGenerator/TypeExtractor.cs
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

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
// ObservablePortGenerator.cs에서
private static void GenerateMethod(
    StringBuilder sb,
    ObservableClassInfo classInfo,
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
public interface IUserRepository : IObservablePort
{
    FinT<IO, User> GetUserAsync(int id);
    FinT<IO, IEnumerable<User>> GetUsersAsync();
}

// 생성되는 코드
public class UserRepositoryObservable : UserRepository
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

`TypeExtractor.ExtractSecondTypeParameter`는 `FinT<IO, T>` 패턴에서 실제 값 타입 `T`를 추출하는 유틸리티입니다. 문자열의 첫 번째 쉼표와 마지막 꺾쇠괄호를 기준으로 파싱하는 단순한 접근이지만, 중첩 제네릭과 튜플까지 올바르게 처리합니다.

| 상황 | 입력 | 출력 |
|------|------|------|
| 단순 값 타입 | `FinT<IO, int>` | `int` |
| 참조 타입 | `FinT<IO, User>` | `User` |
| 컬렉션 | `FinT<IO, List<User>>` | `List<User>` |
| 튜플 | `FinT<IO, (int, string)>` | `(int, string)` |
| 비제네릭 | `void` | `void` |

---

## 다음 단계

심볼 분석과 타입 추출로 코드 생성에 필요한 모든 데이터를 확보했습니다. 다음 장에서는 이 데이터를 실제 C# 코드 문자열로 조립하는 도구인 `StringBuilder`의 활용 패턴을 살펴봅니다.

→ [09. StringBuilder Pattern](../09-StringBuilder-Pattern/)
