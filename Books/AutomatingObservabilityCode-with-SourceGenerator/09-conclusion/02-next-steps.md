# 다음 단계

## 학습 목표

- 추가 학습 주제 파악
- 실전 프로젝트 아이디어
- 커뮤니티 리소스

---

## 추가 학습 주제

### 1. 고급 Roslyn API

#### Syntax Rewriter

기존 코드를 변환하는 패턴입니다.

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

컴파일러 경고/오류에 대한 자동 수정을 제공합니다.

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

코드 분석 규칙을 정의합니다.

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

## 실전 프로젝트 아이디어

### 1. DTO 매퍼 생성기

**목표**: 엔티티 → DTO 매핑 코드 자동 생성

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

**목표**: 불변 객체용 Builder 클래스 자동 생성

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

**목표**: Enum에 대한 유틸리티 메서드 생성

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

**목표**: 인터페이스에서 HTTP 클라이언트 구현 생성

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

**목표**: 데이터 검증 코드 자동 생성

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

### 공식 문서

| 리소스 | URL |
|--------|-----|
| Roslyn 공식 문서 | [docs.microsoft.com/dotnet/csharp/roslyn-sdk](https://docs.microsoft.com/dotnet/csharp/roslyn-sdk) |
| 소스 생성기 쿡북 | [github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md) |
| .NET 블로그 | [devblogs.microsoft.com/dotnet](https://devblogs.microsoft.com/dotnet) |

### GitHub 샘플

| 프로젝트 | 설명 |
|----------|------|
| System.Text.Json | JSON 직렬화 소스 생성기 |
| Refit | REST API 클라이언트 생성기 |
| MediatR | CQRS 패턴 지원 생성기 |
| AutoMapper | 객체 매핑 소스 생성기 |

### 학습 자료

| 유형 | 추천 |
|------|------|
| 책 | "Roslyn Cookbook" |
| 블로그 | Andrew Lock's .NET Blog |
| YouTube | Nick Chapsas 채널 |

---

## 디버깅 팁 복습

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

## 마무리

### 배운 내용

1. **Roslyn 기초**: Syntax Tree, Semantic Model, Symbol
2. **IIncrementalGenerator**: 증분 소스 생성 패턴
3. **ForAttributeWithMetadataName**: 속성 기반 필터링
4. **코드 생성**: StringBuilder, 결정적 출력
5. **고급 시나리오**: 생성자, 제네릭, 컬렉션
6. **테스트**: CSharpCompilation, Verify 스냅샷

### 핵심 교훈

> **컴파일 타임 코드 생성**은 런타임 오버헤드 없이
> 반복적인 보일러플레이트를 제거하는 강력한 도구입니다.

### 다음 목표

1. 직접 소스 생성기 프로젝트 시작하기
2. 팀 프로젝트에 소스 생성기 도입 제안
3. 오픈소스 소스 생성기 코드 분석
4. 고급 Roslyn API 학습

---

## 부록 참조

더 자세한 정보는 부록을 참조하세요.

- [A. 개발 환경](../appendix/A-development-environment.md)
- [B. API 레퍼런스](../appendix/B-api-reference.md)
- [C. 테스트 시나리오 카탈로그](../appendix/C-test-scenario-catalog.md)
- [D. 트러블슈팅](../appendix/D-troubleshooting.md)

---

**축하합니다!** 소스 생성기를 이용한 관찰 가능성 코드 자동화 학습을 완료했습니다.

이제 직접 소스 생성기를 구현해보세요!
