# 컬렉션 타입 처리

## 학습 목표

- 컬렉션 타입 감지 방법
- Count/Length 필드 자동 생성
- 튜플 내 컬렉션 예외 처리

---

## 컬렉션 타입 처리의 필요성

관찰 가능성 코드에서 컬렉션의 **크기 정보**는 중요한 메트릭입니다.

```csharp
// 원본 메서드
public virtual FinT<IO, List<User>> GetUsersAsync() => ...;

// 생성된 Pipeline 코드
public override FinT<IO, List<User>> GetUsersAsync() =>
    FinT.lift<IO, List<User>>(
        // ...
        from __ in IO.lift(() =>
        {
            // ← 컬렉션 크기를 태그로 기록
            activityContext?.SetTag("Response_ResultCount", result?.Count ?? 0);
            activityContext?.Dispose();
            return Unit.Default;
        })
        select result
    );
```

---

## CollectionTypeHelper 구현

### 컬렉션 패턴 정의

```csharp
// Generators/AdapterPipelineGenerator/CollectionTypeHelper.cs
namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 컬렉션 타입 여부를 확인하는 헬퍼 클래스
/// </summary>
public static class CollectionTypeHelper
{
    private static readonly string[] CollectionTypePatterns = [
        // 일반 네임스페이스
        "System.Collections.Generic.List<",
        "System.Collections.Generic.IList<",
        "System.Collections.Generic.ICollection<",
        "System.Collections.Generic.IEnumerable<",
        "System.Collections.Generic.IReadOnlyList<",
        "System.Collections.Generic.IReadOnlyCollection<",
        "System.Collections.Generic.HashSet<",
        "System.Collections.Generic.Dictionary<",
        "System.Collections.Generic.IDictionary<",
        "System.Collections.Generic.IReadOnlyDictionary<",
        "System.Collections.Generic.Queue<",
        "System.Collections.Generic.Stack<",

        // global:: 접두사 버전
        "global::System.Collections.Generic.List<",
        "global::System.Collections.Generic.IList<",
        // ... (동일 패턴)
    ];
}
```

### 컬렉션 타입 확인

```csharp
/// <summary>
/// 타입이 Count 속성을 가진 컬렉션인지 확인합니다.
/// 튜플 타입은 내부에 컬렉션이 있더라도 컬렉션으로 취급하지 않습니다.
/// </summary>
public static bool IsCollectionType(string typeFullName)
{
    if (string.IsNullOrEmpty(typeFullName))
        return false;

    // 튜플 타입은 컬렉션으로 취급하지 않음
    if (IsTupleType(typeFullName))
        return false;

    // 배열 타입 확인 (예: int[], string[])
    if (typeFullName.Contains("[]"))
        return true;

    // 컬렉션 타입 패턴 확인
    return CollectionTypePatterns.Any(pattern => typeFullName.Contains(pattern));
}
```

---

## 튜플 예외 처리

### 왜 튜플을 제외하는가?

튜플 내부에 컬렉션이 있어도 **튜플 자체**의 Count를 기록하는 것은 의미가 없습니다.

```csharp
// 반환 타입: (int Id, List<string> Tags)

// ❌ 잘못된 처리 - 튜플을 컬렉션으로 인식
result?.Count  // 튜플에는 Count가 없음!

// ✅ 올바른 처리 - 튜플은 Count 생성 안 함
// Count 필드 미생성
```

### 튜플 타입 확인

```csharp
/// <summary>
/// 타입이 튜플인지 확인합니다.
/// </summary>
public static bool IsTupleType(string typeFullName)
{
    if (string.IsNullOrEmpty(typeFullName))
        return false;

    // C# 튜플 구문: (int Id, string Name)
    if (typeFullName.StartsWith("(") && typeFullName.EndsWith(")"))
        return true;

    // ValueTuple 타입
    if (typeFullName.Contains("System.ValueTuple") ||
        typeFullName.Contains("global::System.ValueTuple"))
        return true;

    return false;
}
```

---

## Count 표현식 생성

### Count vs Length

```csharp
/// <summary>
/// 컬렉션 타입에 대한 Count 접근 표현식을 생성합니다.
/// 배열은 Length, 나머지는 Count를 사용합니다.
/// </summary>
public static string? GetCountExpression(string variableName, string typeFullName)
{
    if (string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(typeFullName))
        return null;

    if (!IsCollectionType(typeFullName))
        return null;

    // 배열은 Length 사용
    if (typeFullName.Contains("[]"))
        return $"{variableName}?.Length ?? 0";

    // 나머지 컬렉션은 Count 사용
    return $"{variableName}?.Count ?? 0";
}
```

### 표현식 결과 예시

| 타입 | 표현식 |
|------|--------|
| `List<User>` | `result?.Count ?? 0` |
| `string[]` | `result?.Length ?? 0` |
| `Dictionary<K, V>` | `result?.Count ?? 0` |
| `IEnumerable<T>` | `result?.Count ?? 0` |

---

## 필드명 생성

### Request 파라미터 필드

```csharp
/// <summary>
/// Request 파라미터에 대한 필드 이름을 생성합니다.
/// 예: "ms" -> "Request_Ms", "name" -> "Request_Name"
/// </summary>
public static string GetRequestFieldName(string parameterName)
{
    if (string.IsNullOrEmpty(parameterName))
        return parameterName;

    // 첫 글자를 대문자로 변환
    string capitalizedName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);
    return $"Request_{capitalizedName}";
}

/// <summary>
/// Request 파라미터에 대한 Count 필드 이름을 생성합니다.
/// 예: "orders" -> "Request_OrdersCount"
/// </summary>
public static string? GetRequestCountFieldName(string parameterName)
{
    if (string.IsNullOrEmpty(parameterName))
        return null;

    string capitalizedName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);
    return $"Request_{capitalizedName}Count";
}
```

### Response 필드

```csharp
/// <summary>
/// Response 결과에 대한 필드 이름을 생성합니다.
/// </summary>
public static string GetResponseFieldName()
{
    return "Response_Result";
}

/// <summary>
/// Response 결과에 대한 Count 필드 이름을 생성합니다.
/// </summary>
public static string GetResponseCountFieldName()
{
    return "Response_ResultCount";
}
```

---

## 코드 생성에서 활용

### 반환 타입 처리

```csharp
private static void AppendResultTagging(
    StringBuilder sb,
    string innerType)
{
    if (CollectionTypeHelper.IsCollectionType(innerType))
    {
        string? countExpr = CollectionTypeHelper.GetCountExpression("result", innerType);
        string countField = CollectionTypeHelper.GetResponseCountFieldName();

        sb.AppendLine($"            activityContext?.SetTag(\"{countField}\", {countExpr});");
    }

    sb.AppendLine("            activityContext?.Dispose();");
}
```

### 파라미터 처리

```csharp
private static void AppendParameterTags(
    StringBuilder sb,
    IMethodSymbol method)
{
    foreach (var param in method.Parameters)
    {
        string paramType = param.Type.ToDisplayString(
            SymbolDisplayFormats.GlobalQualifiedFormat);

        string fieldName = CollectionTypeHelper.GetRequestFieldName(param.Name);
        sb.AppendLine($"            activityContext?.SetTag(\"{fieldName}\", {param.Name});");

        // 컬렉션 파라미터의 경우 Count 태그 추가
        if (CollectionTypeHelper.IsCollectionType(paramType))
        {
            string? countField = CollectionTypeHelper.GetRequestCountFieldName(param.Name);
            string? countExpr = CollectionTypeHelper.GetCountExpression(param.Name, paramType);

            if (countField is not null && countExpr is not null)
            {
                sb.AppendLine($"            activityContext?.SetTag(\"{countField}\", {countExpr});");
            }
        }
    }
}
```

---

## 생성 결과 예시

### 컬렉션 파라미터

```csharp
// 원본
public virtual FinT<IO, int> ProcessItems(List<string> items) => ...;

// 생성된 코드
public override FinT<IO, int> ProcessItems(List<string> items) =>
    FinT.lift<IO, int>(
        from activityContext in IO.lift(() => CreateActivity("ProcessItems"))
        from _ in IO.lift(() =>
        {
            activityContext?.SetTag("Request_Items", items);
            activityContext?.SetTag("Request_ItemsCount", items?.Count ?? 0);  // ← Count 태그
            StartActivity(activityContext);
            return Unit.Default;
        })
        from result in FinTToIO(base.ProcessItems(items))
        from __ in IO.lift(() =>
        {
            activityContext?.Dispose();
            return Unit.Default;
        })
        select result
    );
```

### 컬렉션 반환 타입

```csharp
// 원본
public virtual FinT<IO, List<User>> GetUsers() => ...;

// 생성된 코드
public override FinT<IO, List<User>> GetUsers() =>
    FinT.lift<IO, List<User>>(
        // ...
        from __ in IO.lift(() =>
        {
            activityContext?.SetTag("Response_ResultCount", result?.Count ?? 0);  // ← Count 태그
            activityContext?.Dispose();
            return Unit.Default;
        })
        select result
    );
```

### 배열 반환 타입

```csharp
// 원본
public virtual FinT<IO, string[]> GetNames() => ...;

// 생성된 코드
// ...
activityContext?.SetTag("Response_ResultCount", result?.Length ?? 0);  // ← Length 사용
```

---

## 테스트 시나리오

### 컬렉션 파라미터 테스트

```csharp
[Fact]
public Task Should_Generate_CollectionCountFields_WithCollectionParameters()
{
    string input = """
        [GeneratePipeline]
        public class DataRepository : IAdapter
        {
            public virtual FinT<IO, int> ProcessItems(List<string> items)
                => FinT<IO, int>.Succ(items?.Count ?? 0);
        }
        """;

    string? actual = _sut.Generate(input);

    // Request_ItemsCount 필드 확인
    actual.ShouldContain("Request_ItemsCount");
    actual.ShouldContain("items?.Count ?? 0");

    return Verify(actual);
}
```

### 튜플 반환 타입 테스트

```csharp
[Fact]
public Task Should_Not_Generate_Count_ForTupleContainingCollection()
{
    // 튜플 내부에 컬렉션이 있어도 Count 미생성
    string input = """
        [GeneratePipeline]
        public class UserRepository : IAdapter
        {
            public virtual FinT<IO, (int Id, List<string> Tags)> GetUserWithTags()
                => FinT<IO, (int Id, List<string> Tags)>.Succ((1, new List<string>()));
        }
        """;

    string? actual = _sut.Generate(input);

    // Response_ResultCount 미생성 확인
    actual.ShouldNotContain("Response_ResultCount");

    return Verify(actual);
}
```

### 배열 포함 튜플 테스트

```csharp
[Fact]
public Task Should_Not_Generate_Length_ForTupleContainingArray()
{
    string input = """
        [GeneratePipeline]
        public class StudentRepository : IAdapter
        {
            public virtual FinT<IO, (string Name, int[] Scores)> GetStudentScores()
                => FinT<IO, (string Name, int[] Scores)>.Succ(("Student", new[] { 90, 85 }));
        }
        """;

    string? actual = _sut.Generate(input);

    // Response_ResultCount (Length) 미생성 확인
    actual.ShouldNotContain("Response_ResultCount");

    return Verify(actual);
}
```

---

## 타입별 동작 요약

| 반환 타입 | Count/Length 생성 | 표현식 |
|----------|-------------------|--------|
| `List<T>` | O | `?.Count ?? 0` |
| `T[]` | O | `?.Length ?? 0` |
| `Dictionary<K, V>` | O | `?.Count ?? 0` |
| `(int, string)` | X | - |
| `(int, List<T>)` | X | - |
| `(T, T[])` | X | - |
| `int` | X | - |
| `string` | X | - |

---

## 요약

| 기능 | 구현 |
|------|------|
| 컬렉션 감지 | 패턴 매칭 |
| 튜플 제외 | `IsTupleType()` 체크 |
| Count 생성 | `GetCountExpression()` |
| Length 생성 | 배열 타입 전용 |
| 필드명 규칙 | `Request_*`, `Response_*` |

---

## 다음 단계

다음 섹션에서는 LoggerMessage.Define 파라미터 제한을 학습합니다.

➡️ [04. LoggerMessage.Define 제한](04-loggermessage-define-limits.md)
