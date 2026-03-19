---
title: "Constructor 처리"
---

## 개요

소스 생성기가 Observable 클래스를 만들 때, 원본 클래스를 상속하므로 부모의 생성자를 올바르게 호출해야 합니다. 그런데 C# 12의 Primary Constructor, 다중 생성자 중 최적 선택, 그리고 `logger`처럼 Observable 자체 파라미터와 이름이 충돌하는 경우까지 고려하면, 생성자 처리는 단순한 코드 복사 이상의 분석이 필요합니다. ObservablePortGenerator는 `ConstructorParameterExtractor`와 `ParameterNameResolver` 두 유틸리티를 통해 이 문제를 체계적으로 해결합니다.

## 학습 목표

### 핵심 학습 목표
1. **Primary Constructor 지원 (C# 12+)**
   - Roslyn에서 Primary Constructor를 식별하고 파라미터를 추출하는 방법
2. **부모 클래스 생성자 파라미터 추출**
   - 타겟 클래스와 부모 클래스의 생성자 탐색 우선순위
3. **파라미터 이름 충돌 해결**
   - Observable이 사용하는 예약 이름과 부모 파라미터가 겹칠 때의 자동 리네이밍

---

## 생성자 처리의 필요성

Observable 클래스는 원본 클래스를 상속합니다. 부모 클래스에 생성자 파라미터가 있으면 이를 전달해야 합니다.

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

## 한눈에 보는 정리

생성자 처리는 크게 두 단계로 이루어집니다. 먼저 `ConstructorParameterExtractor`가 타겟 클래스 또는 부모 클래스에서 최적의 생성자를 선택하고 파라미터를 추출합니다. 그 다음 `ParameterNameResolver`가 Observable의 예약 이름과 충돌하는 파라미터에 `base` 접두사를 붙여 이름을 해결합니다.

| 충돌 이름 | 해결 이름 |
|-----------|-----------|
| `activitySource` | `baseActivitySource` |
| `logger` | `baseLogger` |
| `meterFactory` | `baseMeterFactory` |
| `openTelemetryOptions` | `baseOpenTelemetryOptions` |
| `_logger` | `baseLogger` (언더스코어 제거 + 접두사) |

---

## FAQ

### Q1: Primary Constructor와 일반 생성자가 동시에 있으면 어느 것이 선택되나요?
**A**: `ConstructorParameterExtractor`는 Primary Constructor를 1순위로 선택합니다. Primary Constructor가 없는 경우에만 파라미터가 가장 많은 일반 생성자를 선택합니다. Roslyn에서 Primary Constructor는 `DeclaringSyntaxReferences`의 구문 노드가 `TypeDeclarationSyntax`이고 `ParameterList`가 `null`이 아닌 것으로 식별합니다.

### Q2: `ParameterNameResolver`가 `base` 접두사를 붙이는 예약 이름의 범위는 어디까지인가요?
**A**: Observable 클래스가 자체적으로 사용하는 파라미터 이름(`activitySource`, `logger`, `meterFactory`, `openTelemetryOptions`)이 예약 이름입니다. 부모 클래스의 생성자 파라미터가 이 이름과 동일하면 `baseLogger`, `baseMeterFactory` 등으로 자동 변환됩니다. 언더스코어로 시작하는 파라미터(`_logger`)도 언더스코어를 제거한 뒤 동일한 접두사 규칙을 적용합니다.

### Q3: 타겟 클래스와 부모 클래스 모두에 생성자가 없으면 어떻게 되나요?
**A**: `ConstructorParameterExtractor.ExtractParameters()`가 빈 리스트를 반환하고, 생성된 Observable 클래스의 생성자에는 Observable 자체 파라미터(`ActivitySource`, `ILogger`, `IMeterFactory`, `IOptions<OpenTelemetryOptions>`)만 포함됩니다. `: base(...)` 호출도 생략됩니다.

---

생성자 처리를 통해 Observable 클래스가 부모의 의존성을 올바르게 전달할 수 있게 되었습니다. 다음 섹션에서는 `FinT<IO, T>`에서 내부 타입 `T`를 추출하는 제네릭 타입 처리를 학습합니다.

→ [02. 제네릭 타입](../02-Generic-Types/)
