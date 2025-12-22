# 제네릭 타입 처리

## 학습 목표

- FinT<IO, T>에서 T 추출하기
- 중첩된 제네릭 타입 파싱
- TypeExtractor 유틸리티 활용

---

## 제네릭 타입 처리의 필요성

Adapter 메서드는 `FinT<IO, T>` 형태의 반환 타입을 사용합니다. Pipeline 코드 생성 시 내부 타입 `T`를 추출해야 합니다.

```csharp
// 원본 메서드
public virtual FinT<IO, User> GetUserAsync(int id) => ...;

// 생성된 Pipeline 코드
public override FinT<IO, User> GetUserAsync(int id) =>
    FinT.lift<IO, User>(  // ← T = User 추출 필요
        from activityContext in IO.lift(() => CreateActivity(...))
        // ...
    );
```

---

## TypeExtractor 구현

### 전체 코드

```csharp
// Generators/AdapterPipelineGenerator/TypeExtractor.cs
namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 제네릭 타입에서 내부 타입을 추출하는 유틸리티 클래스
/// </summary>
internal static class TypeExtractor
{
    /// <summary>
    /// FinT<A, B> 형태에서 두 번째 타입 파라미터 B를 추출합니다.
    /// B는 제네릭 타입(예: List<T>)일 수 있으므로 중첩된 <> 처리를 지원합니다.
    /// </summary>
    public static string ExtractSecondTypeParameter(string returnType)
    {
        if (string.IsNullOrEmpty(returnType))
        {
            return returnType;
        }

        int finTStart = returnType.IndexOf("FinT<", StringComparison.Ordinal);
        if (finTStart == -1)
        {
            return returnType;
        }

        // FinT< 다음부터 시작
        int start = finTStart + 5;

        // 첫 번째 타입 파라미터(A)를 건너뛰기 위해 콤마 찾기
        int? commaIndex = FindFirstTypeParameterSeparator(returnType, start);

        if (!commaIndex.HasValue)
        {
            return returnType;
        }

        // 콤마 다음부터 시작 (공백 제거)
        start = SkipWhitespace(returnType, commaIndex.Value + 1);

        // 두 번째 타입 파라미터(B)의 끝 찾기
        int? end = FindTypeParameterEnd(returnType, start);

        if (!end.HasValue)
        {
            return returnType;
        }

        return returnType.Substring(start, end.Value - start).Trim();
    }

    // ... (헬퍼 메서드들)
}
```

---

## 파싱 알고리즘

### 브래킷 카운팅

중첩된 제네릭을 올바르게 파싱하려면 **브래킷 카운팅**이 필요합니다.

```
입력: FinT<IO, Dictionary<string, List<int>>>
      ^   ^  ^        ^      ^   ^  ^^^^^^^
      |   |  |        |      |   |     |
      |   |  |        |      |   +-----+--- 카운트: 3→2→1
      |   |  |        |      +------------- 카운트: 2
      |   |  |        +-------------------- 카운트: 1 (콤마 무시)
      |   |  +----------------------------- 카운트: 1 (여기서 분리!)
      |   +-------------------------------- 카운트: 1
      +------------------------------------ 카운트: 0→1

결과: Dictionary<string, List<int>>
```

### 헬퍼 메서드 - 콤마 찾기

```csharp
/// <summary>
/// 첫 번째 타입 파라미터와 두 번째 타입 파라미터를 구분하는 콤마의 위치를 찾습니다.
/// 중첩된 제네릭 타입 내부의 콤마는 무시합니다.
/// </summary>
private static int? FindFirstTypeParameterSeparator(string text, int startIndex)
{
    int bracketCount = 1; // FinT< 의 < 때문에 1로 시작

    for (int i = startIndex; i < text.Length; i++)
    {
        char c = text[i];

        if (c == '<')
        {
            bracketCount++;
        }
        else if (c == '>')
        {
            bracketCount--;

            if (bracketCount == 0)
            {
                // FinT의 끝에 도달했지만 콤마를 찾지 못함
                return null;
            }
        }
        else if (c == ',' && bracketCount == 1)
        {
            // 첫 번째 레벨의 콤마를 찾음
            return i;
        }
    }

    return null;
}
```

### 헬퍼 메서드 - 끝 위치 찾기

```csharp
/// <summary>
/// 타입 파라미터의 끝 위치를 찾습니다.
/// </summary>
private static int? FindTypeParameterEnd(string text, int startIndex)
{
    int bracketCount = 1; // 상위 FinT< 때문에 1로 시작

    for (int i = startIndex; i < text.Length; i++)
    {
        char c = text[i];

        if (c == '<')
        {
            bracketCount++;
        }
        else if (c == '>')
        {
            bracketCount--;

            if (bracketCount == 0)
            {
                return i;
            }
        }
    }

    return null;
}
```

---

## 지원하는 타입 패턴

### 1. 단순 타입

```csharp
// 입력
"FinT<IO, string>"
"FinT<IO, int>"
"FinT<IO, bool>"

// 출력
"string"
"int"
"bool"
```

### 2. 제네릭 컬렉션

```csharp
// 입력
"FinT<IO, List<int>>"
"FinT<IO, Dictionary<string, int>>"

// 출력
"List<int>"
"Dictionary<string, int>"
```

### 3. 중첩된 제네릭

```csharp
// 입력
"FinT<IO, Dictionary<string, List<int>>>"
"FinT<IO, Result<Data<User<string>>>>"

// 출력
"Dictionary<string, List<int>>"
"Result<Data<User<string>>>"
```

### 4. Fully Qualified Name

```csharp
// 입력 (실제 소스 생성기에서 사용)
"global::LanguageExt.FinT<global::LanguageExt.IO, global::System.Collections.Generic.List<DataResult>>"

// 출력
"global::System.Collections.Generic.List<DataResult>"
```

### 5. 배열 타입

```csharp
// 입력
"FinT<IO, string[]>"
"FinT<IO, int[]>"

// 출력
"string[]"
"int[]"
```

### 6. Nullable 타입

```csharp
// 입력
"FinT<IO, int?>"
"FinT<IO, string?>"

// 출력
"int?"
"string?"
```

### 7. 튜플 타입

```csharp
// 입력
"FinT<IO, (string Name, int Age)>"
"FinT<IO, ((int A, int B), string C)>"
"FinT<IO, (List<int> Numbers, string Name)>"

// 출력
"(string Name, int Age)"
"((int A, int B), string C)"
"(List<int> Numbers, string Name)"
```

---

## 코드 생성에서 활용

### 메서드 생성 시

```csharp
private static void AppendMethodOverride(
    StringBuilder sb,
    IMethodSymbol method,
    string className,
    int methodIndex)
{
    // 반환 타입에서 내부 타입 추출
    string returnType = method.ReturnType.ToDisplayString(
        SymbolDisplayFormats.GlobalQualifiedFormat);

    string innerType = TypeExtractor.ExtractSecondTypeParameter(returnType);

    // FinT.lift<IO, T>에서 T로 사용
    sb.Append($"        global::LanguageExt.FinT.lift<global::LanguageExt.IO, {innerType}>(");
    // ...
}
```

### 생성된 코드 예시

```csharp
// 원본: FinT<IO, List<User>> GetUsers()
// 추출된 타입: List<User>

public override FinT<IO, List<User>> GetUsers() =>
    FinT.lift<IO, List<User>>(  // ← 추출된 타입 사용
        from activityContext in IO.lift(() => CreateActivity("GetUsers"))
        from _ in IO.lift(() => StartActivity(activityContext))
        from result in FinTToIO(base.GetUsers())
        from __ in IO.lift(() =>
        {
            // 컬렉션인 경우 Count 필드 추가
            activityContext?.SetTag("result.Count", result?.Count ?? 0);
            activityContext?.Dispose();
            return Unit.Default;
        })
        select result
    );
```

---

## 엣지 케이스 처리

### FinT가 없는 경우

```csharp
// 입력
"string"

// TypeExtractor 동작
if (finTStart == -1)  // FinT< 없음
{
    return returnType;  // 원본 그대로 반환
}

// 출력
"string"
```

### 빈 문자열

```csharp
// 입력
""
null

// TypeExtractor 동작
if (string.IsNullOrEmpty(returnType))
{
    return returnType;
}

// 출력
""
null
```

---

## 테스트 시나리오

### 단순 타입 테스트

```csharp
[Fact]
public Task Should_Extract_SimpleType()
{
    string input = """
        [GeneratePipeline]
        public class DataRepository : IAdapter
        {
            public virtual FinT<IO, int> GetNumber() => FinT<IO, int>.Succ(42);
            public virtual FinT<IO, string> GetText() => FinT<IO, string>.Succ("hello");
            public virtual FinT<IO, bool> GetFlag() => FinT<IO, bool>.Succ(true);
        }
        """;

    string? actual = _sut.Generate(input);

    // FinT.lift<IO, int>, FinT.lift<IO, string>, FinT.lift<IO, bool> 확인
    return Verify(actual);
}
```

### 컬렉션 타입 테스트

```csharp
[Fact]
public Task Should_Extract_CollectionType()
{
    string input = """
        public class User { public int Id { get; set; } }

        [GeneratePipeline]
        public class UserRepository : IAdapter
        {
            public virtual FinT<IO, List<User>> GetUsers()
                => FinT<IO, List<User>>.Succ(new List<User>());
            public virtual FinT<IO, string[]> GetNames()
                => FinT<IO, string[]>.Succ(Array.Empty<string>());
        }
        """;

    string? actual = _sut.Generate(input);

    // List<User>, string[] 추출 확인
    return Verify(actual);
}
```

### 복잡한 제네릭 테스트

```csharp
[Fact]
public Task Should_Extract_ComplexGenericType()
{
    string input = """
        [GeneratePipeline]
        public class DataRepository : IAdapter
        {
            public virtual FinT<IO, Dictionary<string, List<int>>> GetComplexData()
                => FinT<IO, Dictionary<string, List<int>>>.Succ(
                    new Dictionary<string, List<int>>());
        }
        """;

    string? actual = _sut.Generate(input);

    // Dictionary<string, List<int>> 추출 확인
    return Verify(actual);
}
```

### 튜플 타입 테스트

```csharp
[Fact]
public Task Should_Extract_TupleType()
{
    string input = """
        [GeneratePipeline]
        public class UserRepository : IAdapter
        {
            public virtual FinT<IO, (int Id, string Name)> GetUserInfo()
                => FinT<IO, (int Id, string Name)>.Succ((1, "Test"));
        }
        """;

    string? actual = _sut.Generate(input);

    // (int Id, string Name) 추출 확인
    return Verify(actual);
}
```

---

## 요약

| 패턴 | 입력 예시 | 출력 |
|------|----------|------|
| 단순 타입 | `FinT<IO, int>` | `int` |
| 컬렉션 | `FinT<IO, List<T>>` | `List<T>` |
| 중첩 제네릭 | `FinT<IO, Dict<K, List<V>>>` | `Dict<K, List<V>>` |
| 배열 | `FinT<IO, T[]>` | `T[]` |
| 튜플 | `FinT<IO, (A, B)>` | `(A, B)` |
| Nullable | `FinT<IO, T?>` | `T?` |

| 핵심 알고리즘 | 설명 |
|---------------|------|
| 브래킷 카운팅 | `<` 만나면 +1, `>` 만나면 -1 |
| 콤마 분리 | 카운트 1일 때 콤마에서 분리 |
| 끝 위치 탐색 | 카운트 0이 되는 지점이 끝 |

---

## 다음 단계

다음 섹션에서는 컬렉션 타입 처리를 학습합니다.

➡️ [03. 컬렉션 타입](03-collection-types.md)
