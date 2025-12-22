# 생성자 처리

## 학습 목표

- Primary Constructor 지원 (C# 12+)
- 부모 클래스 생성자 파라미터 추출
- 파라미터 이름 충돌 해결

---

## 생성자 처리의 필요성

Pipeline 클래스는 원본 클래스를 **상속**합니다. 부모 클래스에 생성자 파라미터가 있으면 이를 전달해야 합니다.

```csharp
// 원본 클래스 (Primary Constructor)
[GeneratePipeline]
public class UserRepository(ILogger<UserRepository> logger) : IAdapter
{
    public FinT<IO, User> GetUserAsync(int id) => ...;
}

// 생성되는 Pipeline 클래스
public class UserRepositoryPipeline : UserRepository
{
    // 부모의 logger 파라미터를 전달해야 함!
    public UserRepositoryPipeline(
        ActivityContext parentContext,
        ILogger<UserRepositoryPipeline> logger,
        IAdapterTrace adapterTrace,
        IAdapterMetric adapterMetric,
        ILogger<UserRepository> baseLogger)  // ← 부모용 logger
        : base(baseLogger)  // ← 부모 생성자 호출
    {
        // ...
    }
}
```

---

## ConstructorParameterExtractor

### 전체 구현

```csharp
// Generators/AdapterPipelineGenerator/ConstructorParameterExtractor.cs
namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 클래스의 생성자 파라미터를 추출합니다.
/// Primary Constructor와 일반 생성자를 모두 지원합니다.
/// </summary>
public static class ConstructorParameterExtractor
{
    /// <summary>
    /// 클래스의 생성자 파라미터를 추출합니다.
    /// 타겟 클래스에 생성자가 없으면 부모 클래스에서 찾습니다.
    /// </summary>
    public static List<ParameterInfo> ExtractParameters(INamedTypeSymbol classSymbol)
    {
        // 1. 타겟 클래스의 생성자에서 파라미터 찾기
        var constructor = classSymbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .Where(c => !c.IsStatic)  // 정적 생성자 제외
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

        // 2. 부모 클래스의 생성자에서 찾기 (재귀)
        if (classSymbol.BaseType is not null
            && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
        {
            return ExtractParameters(classSymbol.BaseType);
        }

        return [];
    }
}
```

### 동작 흐름

```
1. 타겟 클래스의 public 생성자 검색
   │
   ├─ 생성자 있음 → 파라미터 추출 → 반환
   │
   └─ 생성자 없음 (또는 파라미터 없음)
       │
       ▼
2. 부모 클래스에서 재귀적으로 검색
   │
   ├─ object까지 도달 → 빈 리스트 반환
   │
   └─ 생성자 발견 → 파라미터 추출 → 반환
```

---

## Primary Constructor 지원

### C# 12 Primary Constructor

```csharp
// Primary Constructor 형태
public class UserRepository(ILogger<UserRepository> logger) : IAdapter
{
    // logger는 클래스 전체에서 사용 가능
}

// 동일한 일반 생성자
public class UserRepository : IAdapter
{
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ILogger<UserRepository> logger)
    {
        _logger = logger;
    }
}
```

### Roslyn에서의 처리

Primary Constructor도 `Constructors`에 포함됩니다:

```csharp
var constructor = classSymbol.Constructors
    .FirstOrDefault();

// Primary Constructor인 경우
// - Parameters.Length > 0
// - MethodKind == MethodKind.Constructor (동일)
```

---

## 파라미터 이름 충돌 해결

### 문제 상황

```csharp
// 원본 클래스
public class UserRepository(ILogger<UserRepository> logger) : IAdapter { }

// 생성되는 Pipeline (충돌!)
public class UserRepositoryPipeline : UserRepository
{
    public UserRepositoryPipeline(
        ILogger<UserRepositoryPipeline> logger,  // Pipeline용 logger
        ILogger<UserRepository> logger)          // ❌ 같은 이름!
        : base(logger)
    {
    }
}
```

### ParameterNameResolver

```csharp
// Generators/AdapterPipelineGenerator/ParameterNameResolver.cs
namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 파라미터 이름 충돌을 해결합니다.
/// </summary>
public static class ParameterNameResolver
{
    // Pipeline 생성자에서 사용하는 예약된 파라미터 이름
    private static readonly HashSet<string> ReservedNames = new(StringComparer.Ordinal)
    {
        "parentContext",
        "logger",
        "adapterTrace",
        "adapterMetric"
    };

    /// <summary>
    /// 파라미터 이름 충돌을 해결하여 반환합니다.
    /// </summary>
    public static List<ResolvedParameter> ResolveNames(List<ParameterInfo> parameters)
    {
        var result = new List<ResolvedParameter>();

        foreach (var param in parameters)
        {
            string resolvedName = param.Name;

            // 예약된 이름과 충돌하면 "base" 접두사 추가
            if (ReservedNames.Contains(param.Name))
            {
                resolvedName = $"base{char.ToUpper(param.Name[0])}{param.Name.Substring(1)}";
                // "logger" → "baseLogger"
            }

            result.Add(new ResolvedParameter(param, resolvedName));
        }

        return result;
    }
}

public sealed record ResolvedParameter(
    ParameterInfo Original,
    string ResolvedName);
```

### 해결 결과

```csharp
// 원본: logger
// 해결: baseLogger

public class UserRepositoryPipeline : UserRepository
{
    public UserRepositoryPipeline(
        ActivityContext parentContext,
        ILogger<UserRepositoryPipeline> logger,      // Pipeline용
        IAdapterTrace adapterTrace,
        IAdapterMetric adapterMetric,
        ILogger<UserRepository> baseLogger)           // ← 이름 변경됨
        : base(baseLogger)                            // ← 부모에 전달
    {
        // ...
    }
}
```

---

## 생성자 코드 생성

### 파라미터 선언 생성

```csharp
private static string GenerateBaseConstructorParameters(
    List<ParameterInfo> baseConstructorParameters)
{
    if (baseConstructorParameters.Count == 0)
    {
        return string.Empty;
    }

    var resolvedParams = ParameterNameResolver.ResolveNames(baseConstructorParameters);

    var parameters = resolvedParams
        .Select(p => $",\n        {p.Original.Type} {p.ResolvedName}")
        .ToList();

    return string.Join("", parameters);
}

// 예시 출력:
// ",
//         global::Microsoft.Extensions.Logging.ILogger<global::MyApp.UserRepository> baseLogger"
```

### 부모 생성자 호출 생성

```csharp
private static string GenerateBaseConstructorCall(
    List<ParameterInfo> baseConstructorParameters)
{
    if (baseConstructorParameters.Count == 0)
    {
        return string.Empty;
    }

    var resolvedParams = ParameterNameResolver.ResolveNames(baseConstructorParameters);
    var parameterNames = resolvedParams.Select(p => p.ResolvedName);

    return $"        : base({string.Join(", ", parameterNames)})";
}

// 예시 출력:
// "        : base(baseLogger)"
```

---

## 테스트 시나리오

### Primary Constructor

```csharp
[Fact]
public Task Should_Handle_Primary_Constructor()
{
    string input = """
        [GeneratePipeline]
        public class UserRepository(ILogger<UserRepository> logger) : IAdapter
        {
            public FinT<IO, User> GetUserAsync(int id) => throw new();
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

### 다중 생성자

```csharp
[Fact]
public Task Should_Select_Constructor_With_Most_Parameters()
{
    string input = """
        [GeneratePipeline]
        public class UserRepository : IAdapter
        {
            public UserRepository() { }
            public UserRepository(ILogger<UserRepository> logger) { }
            public UserRepository(ILogger<UserRepository> logger, IDbContext db) { }  // 선택됨
        }
        """;

    string? actual = _sut.Generate(input);
    return Verify(actual);
}
```

### 파라미터 이름 충돌

```csharp
[Fact]
public Task Should_Resolve_Parameter_Name_Conflict()
{
    string input = """
        [GeneratePipeline]
        public class UserRepository(ILogger<UserRepository> logger) : IAdapter { }
        """;

    string? actual = _sut.Generate(input);

    // baseLogger로 이름 변경 확인
    actual.ShouldContain("baseLogger");
    actual.ShouldContain(": base(baseLogger)");

    return Verify(actual);
}
```

---

## 요약

| 상황 | 처리 방법 |
|------|-----------|
| Primary Constructor | Constructors에서 추출 |
| 다중 생성자 | 파라미터 많은 것 선택 |
| 부모 클래스 | 재귀적으로 탐색 |
| 이름 충돌 | `base` 접두사 추가 |

| 충돌 이름 | 해결 이름 |
|-----------|-----------|
| `logger` | `baseLogger` |
| `parentContext` | `baseParentContext` |
| `adapterTrace` | `baseAdapterTrace` |
| `adapterMetric` | `baseAdapterMetric` |

---

## 다음 단계

다음 섹션에서는 제네릭 타입 처리를 학습합니다.

➡️ [02. 제네릭 타입](02-generic-types.md)
