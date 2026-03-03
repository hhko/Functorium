---
title: "생성자 처리"
---

## 학습 목표

- Primary Constructor 지원 (C# 12+)
- 부모 클래스 생성자 파라미터 추출
- 파라미터 이름 충돌 해결

---

## 생성자 처리의 필요성

Observable 클래스는 원본 클래스를 **상속**합니다. 부모 클래스에 생성자 파라미터가 있으면 이를 전달해야 합니다.

```csharp
// 원본 클래스 (Primary Constructor)
[GenerateObservablePort]
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
{
    public FinT<IO, User> GetUserAsync(int id) => ...;
}

// 생성되는 Observable 클래스
public class UserRepositoryObservable : UserRepository
{
    // 부모의 logger 파라미터를 전달해야 함!
    public UserRepositoryObservable(
        ActivitySource activitySource,
        ILogger<UserRepositoryObservable> logger,
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
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
// Generators/ObservablePortGenerator/ConstructorParameterExtractor.cs
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// 클래스의 생성자 파라미터를 추출하는 유틸리티 클래스
/// </summary>
internal static class ConstructorParameterExtractor
{
    /// <summary>
    /// 타겟 클래스 또는 부모 클래스에서 생성자 파라미터를 추출합니다.
    ///
    /// 우선순위:
    /// 1. 타겟 클래스 자체의 생성자 (파라미터가 있는 경우)
    /// 2. 부모 클래스의 생성자 (타겟 클래스에 파라미터 생성자가 없는 경우)
    /// </summary>
    public static List<ParameterInfo> ExtractParameters(INamedTypeSymbol classSymbol)
    {
        // 1. 타겟 클래스의 생성자 확인 (우선순위)
        var targetConstructorParams = TryExtractFromTargetClass(classSymbol);
        if (targetConstructorParams.Count > 0)
        {
            return targetConstructorParams;
        }

        // 2. 부모 클래스의 생성자 확인
        return TryExtractFromBaseClass(classSymbol);
    }

    private static List<ParameterInfo> TryExtractFromTargetClass(INamedTypeSymbol classSymbol)
    {
        var constructors = GetPublicConstructors(classSymbol);
        var selectedConstructor = SelectBestConstructor(constructors);

        if (selectedConstructor != null && selectedConstructor.Parameters.Length > 0)
        {
            return ConvertToParameterInfoList(selectedConstructor.Parameters);
        }

        return new List<ParameterInfo>();
    }

    private static List<ParameterInfo> TryExtractFromBaseClass(INamedTypeSymbol classSymbol)
    {
        if (classSymbol.BaseType == null || classSymbol.BaseType.SpecialType == SpecialType.System_Object)
        {
            return new List<ParameterInfo>();
        }

        var constructors = GetPublicConstructors(classSymbol.BaseType);
        var selectedConstructor = SelectBestConstructor(constructors);

        if (selectedConstructor != null && selectedConstructor.Parameters.Length > 0)
        {
            return ConvertToParameterInfoList(selectedConstructor.Parameters);
        }

        return new List<ParameterInfo>();
    }

    /// <summary>
    /// 가장 적절한 생성자를 선택합니다.
    /// 우선순위: 1. Primary constructor (C# 12+), 2. 파라미터가 가장 많은 생성자
    /// </summary>
    private static IMethodSymbol? SelectBestConstructor(List<IMethodSymbol> constructors)
    {
        // 1순위: Primary constructor
        var primaryConstructor = constructors.FirstOrDefault(IsPrimaryConstructor);
        if (primaryConstructor != null)
        {
            return primaryConstructor;
        }

        // 2순위: 파라미터가 가장 많은 생성자
        return constructors
            .OrderByDescending(c => c.Parameters.Length)
            .FirstOrDefault();
    }

    private static bool IsPrimaryConstructor(IMethodSymbol constructor)
    {
        var syntaxReferences = constructor.DeclaringSyntaxReferences;
        if (syntaxReferences.Length == 0) return false;

        var syntax = syntaxReferences[0].GetSyntax();
        return syntax is TypeDeclarationSyntax typeDecl && typeDecl.ParameterList != null;
    }
}
```

### 동작 흐름

```
1. TryExtractFromTargetClass: 타겟 클래스의 public 생성자 검색
   │
   ├─ 생성자 있음 → SelectBestConstructor → 파라미터 추출 → 반환
   │
   └─ 생성자 없음 (또는 파라미터 없음)
       │
       ▼
2. TryExtractFromBaseClass: 부모 클래스의 public 생성자 검색
   │
   ├─ object까지 도달 → 빈 리스트 반환
   │
   └─ 생성자 발견 → SelectBestConstructor → 파라미터 추출 → 반환
```

---

## Primary Constructor 지원

### C# 12 Primary Constructor

```csharp
// Primary Constructor 형태
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
{
    // logger는 클래스 전체에서 사용 가능
}

// 동일한 일반 생성자
public class UserRepository : IObservablePort
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
public class UserRepository(ILogger<UserRepository> logger) : IObservablePort { }

// 생성되는 Observable (충돌!)
public class UserRepositoryObservable : UserRepository
{
    public UserRepositoryObservable(
        ILogger<UserRepositoryObservable> logger,  // Observable용 logger
        ILogger<UserRepository> logger)          // ❌ 같은 이름!
        : base(logger)
    {
    }
}
```

### ParameterNameResolver

```csharp
// Generators/ObservablePortGenerator/ParameterNameResolver.cs
namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

/// <summary>
/// 파라미터 이름 충돌을 해결하는 유틸리티 클래스
/// </summary>
internal static class ParameterNameResolver
{
    /// <summary>
    /// 예약된 이름과 충돌하는 경우 새로운 이름을 반환합니다.
    /// </summary>
    public static string ResolveName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return parameterName;
        }

        // 언더스코어로 시작하는 경우: _logger → baseLogger
        if (parameterName.StartsWith("_"))
        {
            string nameWithoutUnderscore = parameterName.Substring(1);
            return $"{ObservableGeneratorConstants.NameConflictPrefix}{char.ToUpper(nameWithoutUnderscore[0])}{nameWithoutUnderscore.Substring(1)}";
        }

        // 예약된 이름과 충돌: logger → baseLogger
        if (ObservableGeneratorConstants.ReservedParameterNames.Contains(parameterName))
        {
            return $"{ObservableGeneratorConstants.NameConflictPrefix}{char.ToUpper(parameterName[0])}{parameterName.Substring(1)}";
        }

        return parameterName;
    }

    /// <summary>
    /// 파라미터 목록의 이름들을 충돌 없이 해결합니다.
    /// </summary>
    public static List<(ParameterInfo Original, string ResolvedName)> ResolveNames(List<ParameterInfo> parameters)
    {
        return parameters
            .Select(p => (Original: p, ResolvedName: ResolveName(p.Name)))
            .ToList();
    }
}
```

### 해결 결과

```csharp
// 원본: logger
// 해결: baseLogger

public class UserRepositoryObservable : UserRepository
{
    public UserRepositoryObservable(
        ActivitySource activitySource,
        ILogger<UserRepositoryObservable> logger,      // Observable용
        IMeterFactory meterFactory,
        IOptions<OpenTelemetryOptions> openTelemetryOptions,
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
        [GenerateObservablePort]
        public class UserRepository(ILogger<UserRepository> logger) : IObservablePort
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
        [GenerateObservablePort]
        public class UserRepository : IObservablePort
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
        [GenerateObservablePort]
        public class UserRepository(ILogger<UserRepository> logger) : IObservablePort { }
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
| `activitySource` | `baseActivitySource` |
| `logger` | `baseLogger` |
| `meterFactory` | `baseMeterFactory` |
| `openTelemetryOptions` | `baseOpenTelemetryOptions` |
| `_logger` | `baseLogger` (언더스코어 제거 + 접두사) |

---

## 다음 단계

다음 섹션에서는 제네릭 타입 처리를 학습합니다.

➡️ [02. 제네릭 타입](../02-Generic-Types/)
