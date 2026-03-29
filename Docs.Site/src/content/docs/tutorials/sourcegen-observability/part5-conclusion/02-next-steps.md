---
title: "다음 단계"
---

## 개요

소스 생성기의 기초를 익히고 ObservablePortGenerator를 완성했다면, 이제 그 경험을 토대로 더 넓은 Roslyn 생태계를 탐험할 준비가 되었습니다. 이 섹션에서는 고급 Roslyn API, 실전에서 도전해볼 만한 프로젝트 아이디어, 그리고 지속적인 학습을 위한 커뮤니티 리소스를 소개합니다.

## 학습 목표

### 핵심 학습 목표
1. **추가 학습 주제 파악**
   - 소스 생성기 너머의 Roslyn API(Analyzer, Code Fix Provider)로 역량을 확장합니다
2. **실전 프로젝트 아이디어**
   - 난이도 순으로 정리된 프로젝트를 통해 소스 생성기 구현 감각을 키웁니다
3. **커뮤니티 리소스 활용**
   - 공식 문서, 오픈소스 프로젝트, 학습 자료를 활용하여 지속적으로 성장합니다

---

## 추가 학습 주제

### 1. 고급 Roslyn API

소스 생성기가 "새 코드를 추가"하는 도구라면, Roslyn은 그 외에도 기존 코드를 분석하고 변환하는 풍부한 API를 제공합니다.

#### Syntax Rewriter

기존 코드의 Syntax Tree를 탐색하면서 특정 노드를 변환하는 패턴입니다. 예를 들어 메서드 이름 규칙을 일괄 변경하거나, 특정 패턴의 코드를 자동으로 리팩터링할 수 있습니다.

```csharp
public class MyRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        // 메서드 선언 수정
        return base.VisitMethodDeclaration(node);
    }
}
```

#### Code Fix Provider

Analyzer가 문제를 감지했을 때, 개발자에게 자동 수정을 제안하는 도구입니다. IDE에서 "전구 아이콘"으로 나타나는 Quick Fix가 바로 이 메커니즘입니다.

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp)]
public class MyCodeFixProvider : CodeFixProvider
{
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // 코드 수정 제안 등록
    }
}
```

#### Analyzer

프로젝트 전체에 적용되는 커스텀 코드 분석 규칙을 정의합니다. ObservablePortGenerator에서 `[GenerateObservablePort]` 속성의 올바른 사용을 강제하는 Analyzer를 만들면, 소스 생성기와 시너지를 낼 수 있습니다.

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MyAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
    }
}
```

### 2. 다른 소스 생성기 패턴

.NET 생태계에는 이미 다양한 소스 생성기가 활용되고 있습니다. 이들의 설계를 분석하면 ObservablePortGenerator에서 배운 패턴이 어떻게 변형·확장되는지 확인할 수 있습니다.

#### JSON 직렬화 생성기

```csharp
// System.Text.Json.SourceGeneration
[JsonSerializable(typeof(User))]
public partial class MyJsonContext : JsonSerializerContext { }
```

#### Regex 소스 생성기

```csharp
// .NET 7+
[GeneratedRegex(@"\d+")]
private static partial Regex NumberRegex();
```

#### AutoMapper 소스 생성기

```csharp
[AutoMap(typeof(UserDto))]
public class User { }
```

### 3. 성능 최적화

대규모 프로젝트에서 소스 생성기가 수백 개의 클래스를 처리할 때, 증분 캐싱과 병렬 처리는 빌드 시간에 직접적인 영향을 미칩니다.

#### 증분 캐싱 심화

```csharp
// 값 비교 최적화
context.SyntaxProvider
    .CreateSyntaxProvider(...)
    .WithComparer(new MyEqualityComparer())
```

#### 병렬 처리

```csharp
// 대량 심볼 처리 시
Parallel.ForEach(symbols, symbol => {
    ProcessSymbol(symbol);
});
```

---

## Functorium의 다른 소스 생성기

ObservablePortGenerator에서 배운 `IncrementalGeneratorBase` 패턴은 이미 Functorium 프로젝트 내에서 다른 도메인에도 적용되고 있습니다. **UnionTypeGenerator는** 판별 합집합(Discriminated Union) 타입에 대해 `Match`/`Switch` 메서드를 자동 생성하는 소스 생성기로, `IncrementalGeneratorBase<UnionTypeInfo>`를 상속하여 동일한 2단계 파이프라인(소스 제공자 등록 → 코드 생성)을 따릅니다. 튜토리얼에서 학습한 템플릿 메서드 패턴이 관찰 가능성이 아닌 완전히 다른 도메인에서도 재사용되는 실제 사례이므로, 소스 코드(`Src/Functorium.SourceGenerators/Generators/UnionTypeGenerator/`)를 참고하면 패턴 확장에 대한 감각을 얻을 수 있습니다.

---

## 실전 프로젝트 아이디어

다음 프로젝트들은 난이도 순으로 정리되어 있습니다. DTO 매퍼와 Builder 패턴 생성기로 기본기를 다진 뒤, Enum 확장과 API 클라이언트 생성기로 복잡한 시나리오를 경험하고, 검증 규칙 생성기에서 Analyzer와의 통합까지 시도해보십시오.

### 1. DTO 매퍼 생성기

**목표**: 엔티티 → DTO 매핑 코드 자동 생성. 프로퍼티 이름과 타입을 비교하는 로직은 ObservablePortGenerator에서 심볼을 분석했던 경험을 직접 활용할 수 있어, 첫 번째 프로젝트로 적합합니다.

```csharp
// 입력
[GenerateMapper]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// 생성
public static class UserMapper
{
    public static UserDto ToDto(this User user) =>
        new UserDto { Id = user.Id, Name = user.Name };
}
```

### 2. Builder 패턴 생성기

**목표**: 불변 객체용 Builder 클래스 자동 생성. record의 생성자 파라미터를 분석하여 Fluent API를 생성하는 과정은 ConstructorParameterExtractor에서 다뤘던 패턴의 확장입니다.

```csharp
// 입력
[GenerateBuilder]
public record User(int Id, string Name, string Email);

// 생성
public class UserBuilder
{
    private int _id;
    private string _name;
    private string _email;

    public UserBuilder WithId(int id) { _id = id; return this; }
    public UserBuilder WithName(string name) { _name = name; return this; }
    public User Build() => new(_id, _name, _email);
}
```

### 3. Enum 확장 생성기

**목표**: Enum에 대한 유틸리티 메서드 생성. Enum 멤버를 순회하며 switch 표현식을 생성하는 과정에서, CollectionTypeHelper와 유사한 타입별 분기 로직을 설계하게 됩니다.

```csharp
// 입력
[GenerateEnumExtensions]
public enum OrderStatus { Pending, Processing, Completed }

// 생성
public static class OrderStatusExtensions
{
    public static string ToDisplayString(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "대기 중",
        OrderStatus.Processing => "처리 중",
        OrderStatus.Completed => "완료됨",
        _ => throw new ArgumentOutOfRangeException()
    };

    public static bool IsFinalState(this OrderStatus status) =>
        status == OrderStatus.Completed;
}
```

### 4. API 클라이언트 생성기

**목표**: 인터페이스에서 HTTP 클라이언트 구현 생성. 속성에서 URL 패턴을 추출하고, 메서드 시그니처에서 HTTP 메서드와 파라미터 바인딩을 결정하는 과정은 ObservablePortGenerator의 메서드 분석 로직을 한 단계 더 복잡하게 확장합니다.

```csharp
// 입력
[GenerateHttpClient("https://api.example.com")]
public interface IUserApi
{
    [Get("/users/{id}")]
    Task<User> GetUserAsync(int id);

    [Post("/users")]
    Task<User> CreateUserAsync(CreateUserRequest request);
}

// 생성
public class UserApiClient : IUserApi
{
    private readonly HttpClient _client;

    public async Task<User> GetUserAsync(int id)
    {
        var response = await _client.GetAsync($"/users/{id}");
        return await response.Content.ReadFromJsonAsync<User>();
    }
}
```

### 5. 검증 규칙 생성기

**목표**: 데이터 검증 코드 자동 생성. 가장 도전적인 프로젝트로, 속성 기반 규칙 해석과 함께 Analyzer를 결합하여 컴파일 타임에 잘못된 검증 규칙을 경고할 수도 있습니다.

```csharp
// 입력
[GenerateValidator]
public class CreateUserRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    [Range(0, 150)]
    public int Age { get; set; }
}

// 생성
public class CreateUserRequestValidator
{
    public ValidationResult Validate(CreateUserRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(request.Name))
            errors.Add("Name is required");

        if (request.Name?.Length > 100)
            errors.Add("Name must be at most 100 characters");

        // ...

        return new ValidationResult(errors);
    }
}
```

---

## 커뮤니티 리소스

소스 생성기를 깊이 있게 학습하려면 공식 문서와 실전 오픈소스 프로젝트를 병행하는 것이 효과적입니다.

### 공식 문서

| 리소스 | URL |
|--------|-----|
| Roslyn 공식 문서 | [docs.microsoft.com/dotnet/csharp/roslyn-sdk](https://docs.microsoft.com/dotnet/csharp/roslyn-sdk) |
| 소스 생성기 쿡북 | [github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md) |
| .NET 블로그 | [devblogs.microsoft.com/dotnet](https://devblogs.microsoft.com/dotnet) |

### GitHub 예제

| 프로젝트 | 설명 |
|----------|------|
| System.Text.Json | JSON 직렬화 소스 생성기 |
| Refit | REST API 클라이언트 생성기 |
| MediatR | CQRS 패턴 지원 생성기 |
| AutoMapper | 객체 매핑 소스 생성기 |

### 학습 자료

서적으로는 "Roslyn Cookbook"이 Roslyn API 전반을 체계적으로 다루고 있어 참고하기 좋습니다. 블로그로는 Andrew Lock의 .NET Blog가 소스 생성기를 포함한 .NET 심층 주제를 꾸준히 다루며, YouTube에서는 Nick Chapsas 채널이 소스 생성기 실전 활용 사례를 영상으로 제공합니다.

---

## 디버깅 팁 복습

소스 생성기 개발에서 가장 까다로운 부분 중 하나는 디버깅입니다. 생성기는 컴파일러 내부에서 실행되므로 일반적인 브레이크포인트 방식으로는 접근하기 어렵습니다. 다음 두 가지 기법을 기억해두십시오.

### Debugger.Launch()

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
#if DEBUG
    if (!System.Diagnostics.Debugger.IsAttached)
    {
        System.Diagnostics.Debugger.Launch();
    }
#endif
}
```

### 진단 메시지 출력

```csharp
context.ReportDiagnostic(Diagnostic.Create(
    new DiagnosticDescriptor(
        "SG001",
        "Debug Info",
        "Processing: {0}",
        "Debug",
        DiagnosticSeverity.Warning,
        true),
    Location.None,
    className));
```

---

## FAQ

### Q1: Analyzer와 소스 생성기를 함께 사용하면 어떤 시너지가 있나요?
**A**: 소스 생성기가 코드를 생성하는 "생산자"라면, Analyzer는 사용 규칙을 강제하는 "검증자"입니다. 예를 들어 `[GenerateObservablePort]` 속성이 `IObservablePort`를 구현하지 않는 클래스에 붙었을 때 컴파일 경고를 표시하는 Analyzer를 만들면, 잘못된 사용을 컴파일 타임에 미리 방지할 수 있습니다.

### Q2: 실전 프로젝트 아이디어 중 첫 번째로 어떤 것을 추천하나요?
**A**: DTO 매퍼 생성기를 추천합니다. 프로퍼티 이름과 타입을 비교하는 로직이 ObservablePortGenerator에서 심볼을 분석했던 경험과 직접 연결되고, 생성할 코드의 구조가 단순하여 빠르게 완성할 수 있습니다. 완성 후에는 Builder 패턴 생성기로 넘어가면 생성자 파라미터 분석 경험을 확장할 수 있습니다.

### Q3: 소스 생성기를 팀 프로젝트에 도입할 때 주의할 점은 무엇인가요?
**A**: 세 가지를 고려해야 합니다. 첫째, `.verified.txt` 파일의 리뷰 부담이 증가하므로 스냅샷 변경에 대한 팀 합의가 필요합니다. 둘째, 소스 생성기 프로젝트는 `netstandard2.0`이므로 최신 C# 기능 사용에 제약이 있습니다. 셋째, 생성기 버그가 컴파일 오류로 나타나면 원인 추적이 어려우므로, 충분한 테스트 커버리지와 디버깅 전략(`Debugger.Launch()`, 진단 메시지)을 사전에 갖춰야 합니다.

---

## 마무리

### 배운 내용

이 튜토리얼은 Roslyn의 기초 개념(Syntax Tree, Semantic Model, Symbol)에서 출발하여 `IIncrementalGenerator`로 증분 소스 생성 패턴을 구현하고, `ForAttributeWithMetadataName`으로 속성 기반 필터링을 적용하는 과정을 거쳤습니다. StringBuilder와 결정적 출력 원칙으로 코드 생성의 신뢰성을 확보하고, 생성자·제네릭·컬렉션 등 고급 시나리오를 처리하며, CSharpCompilation과 Verify 스냅샷으로 모든 결과를 검증했습니다.

### 핵심 교훈

> **컴파일 타임 코드 생성은** 런타임 오버헤드 없이
> 반복적인 보일러플레이트를 제거하는 강력한 도구입니다.

### 다음 목표

이 튜토리얼에서 익힌 패턴을 실전으로 확장할 차례입니다. DTO 매퍼 생성기 같은 작은 프로젝트로 시작하여 감각을 다지고, 팀 프로젝트에 도입을 제안하여 실무 적용 경험을 쌓아보십시오. 오픈소스 소스 생성기의 코드를 분석하면 실전 수준의 설계 판단을 배울 수 있고, Analyzer와 Code Fix Provider까지 영역을 넓히면 개발 도구 전반을 아우르는 Roslyn 전문가로 성장할 수 있습니다.

---

## 부록 참조

튜토리얼 본문에서 다루지 못한 세부 사항은 부록에서 확인할 수 있습니다.

- [A. 개발 환경](../Appendix/A-development-environment.md)
- [B. API 레퍼런스](../Appendix/B-api-reference.md)
- [C. 테스트 시나리오 카탈로그](../Appendix/C-test-scenario-catalog.md)
- [D. 트러블슈팅](../Appendix/D-troubleshooting.md)

---

소스 생성기를 이용한 관찰 가능성 코드 자동화 학습을 완료했습니다. 이제 직접 소스 생성기를 구현하여, 컴파일 타임의 힘을 프로젝트에 적용해보십시오.
