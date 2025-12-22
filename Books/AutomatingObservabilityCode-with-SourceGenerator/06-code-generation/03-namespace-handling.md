# 네임스페이스 처리

## 학습 목표

- ContainingNamespace에서 정보 추출
- 글로벌 네임스페이스 처리
- 파일명에 네임스페이스 접미사 활용

---

## 네임스페이스 추출

### 기본 추출

```csharp
INamedTypeSymbol classSymbol = ...;

// ContainingNamespace에서 네임스페이스 얻기
INamespaceSymbol namespaceSymbol = classSymbol.ContainingNamespace;

// 문자열로 변환
string @namespace = namespaceSymbol.ToString();
// "MyApp.Infrastructure.Repositories"
```

### 글로벌 네임스페이스 처리

```csharp
// 글로벌 네임스페이스인 경우 특별 처리 필요
string @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace
    ? string.Empty  // 빈 문자열 반환
    : classSymbol.ContainingNamespace.ToString();

// 글로벌 네임스페이스란?
// namespace 선언 없이 정의된 타입
// 예:
// public class GlobalClass { }  // 글로벌 네임스페이스

// namespace MyApp;
// public class NamespacedClass { }  // MyApp 네임스페이스
```

---

## 네임스페이스 선언 생성

### File-scoped 네임스페이스 (C# 10+)

```csharp
// 네임스페이스가 있는 경우
if (!string.IsNullOrEmpty(@namespace))
{
    sb.AppendLine($"namespace {@namespace};")
      .AppendLine();
}

// 생성 결과:
// namespace MyApp.Infrastructure.Repositories;
//
// public class UserRepositoryPipeline : UserRepository
// {
// }
```

### 글로벌 네임스페이스 처리

```csharp
// 글로벌 네임스페이스인 경우 namespace 선언 생략
if (string.IsNullOrEmpty(@namespace))
{
    // namespace 선언 없이 바로 클래스 정의
    sb.AppendLine($"public class {className}Pipeline : {className}")
      .AppendLine("{");
}
else
{
    sb.AppendLine($"namespace {@namespace};")
      .AppendLine()
      .AppendLine($"public class {className}Pipeline : {className}")
      .AppendLine("{");
}
```

---

## 파일명에 네임스페이스 활용

### 파일명 충돌 방지

```csharp
// 문제: 다른 네임스페이스에 같은 클래스 이름
// MyApp.Repositories.UserRepository
// MyApp.Services.UserRepository

// 같은 파일명으로 충돌 발생
// UserRepositoryPipeline.g.cs  (어느 것?)
```

### 네임스페이스 접미사 추출

```csharp
// AdapterPipelineGenerator.cs의 실제 코드
private static void Generate(
    SourceProductionContext context,
    ImmutableArray<PipelineClassInfo> pipelineClasses)
{
    foreach (var pipelineClass in pipelineClasses)
    {
        // 네임스페이스의 마지막 부분 추출
        string namespaceSuffix = string.Empty;
        if (!string.IsNullOrEmpty(pipelineClass.Namespace))
        {
            var lastDotIndex = pipelineClass.Namespace.LastIndexOf('.');
            if (lastDotIndex >= 0)
            {
                // "MyApp.Infrastructure.Repositories" → "Repositories"
                namespaceSuffix = pipelineClass.Namespace
                    .Substring(lastDotIndex + 1) + ".";
            }
        }

        // 파일명 생성
        string fileName = $"{namespaceSuffix}{pipelineClass.ClassName}Pipeline.g.cs";
        // "Repositories.UserRepositoryPipeline.g.cs"

        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }
}
```

### 예시

```
입력 클래스                              생성되는 파일명
==========                              ===============
MyApp.Repositories.UserRepository       Repositories.UserRepositoryPipeline.g.cs
MyApp.Services.UserRepository           Services.UserRepositoryPipeline.g.cs
MyApp.Data.UserRepository               Data.UserRepositoryPipeline.g.cs
GlobalClass (글로벌 네임스페이스)        GlobalClassPipeline.g.cs
```

---

## 중첩 네임스페이스 처리

### 깊은 네임스페이스

```csharp
// 입력: "A.B.C.D.E"
// lastDotIndex: 7 ('E' 직전의 '.')
// 접미사: "E."

string @namespace = "A.B.C.D.E";
var lastDotIndex = @namespace.LastIndexOf('.');
string suffix = @namespace.Substring(lastDotIndex + 1) + ".";
// suffix = "E."
```

### 단순 네임스페이스

```csharp
// 입력: "MyApp"
// lastDotIndex: -1 (점 없음)
// 접미사: 없음 (빈 문자열)

string @namespace = "MyApp";
var lastDotIndex = @namespace.LastIndexOf('.');
if (lastDotIndex >= 0)
{
    // 이 블록 실행 안 됨
}
// suffix = ""
```

---

## 타입 참조 시 네임스페이스

### global:: 접두사 사용

```csharp
// 생성되는 코드에서 타입 참조 시

// ❌ 위험: 사용자 코드와 충돌 가능
sb.AppendLine("using System;");
sb.AppendLine("ArgumentNullException.ThrowIfNull(value);");

// ✅ 안전: global:: 접두사로 명확히
sb.AppendLine("global::System.ArgumentNullException.ThrowIfNull(value);");
```

### 생성 코드 예시

```csharp
// 안전한 타입 참조
sb.AppendLine("    private readonly global::System.Diagnostics.ActivityContext _parentContext;")
  .AppendLine()
  .AppendLine("    public void Validate()")
  .AppendLine("    {")
  .AppendLine("        global::System.ArgumentNullException.ThrowIfNull(_logger);")
  .AppendLine("    }");
```

---

## using 별칭

### 긴 네임스페이스 축약

```csharp
// 생성되는 코드에 using 별칭 추가
sb.AppendLine("using ObservabilityFields = Functorium.Adapters.Observabilities.ObservabilityFields;");

// 사용
sb.AppendLine("    var eventId = ObservabilityFields.EventIds.Adapter.AdapterRequest;");

// global:: 대안
sb.AppendLine("    var eventId = global::Functorium.Adapters.Observabilities.ObservabilityFields.EventIds.Adapter.AdapterRequest;");
// → 너무 길어서 가독성 저하
```

---

## 테스트 시나리오

### 네임스페이스 관련 테스트

```csharp
public class NamespaceTests
{
    [Fact]
    public Task Should_Handle_Simple_Namespace()
    {
        string input = """
            namespace MyApp;

            [GeneratePipeline]
            public class UserRepository : IAdapter { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // 생성 파일: UserRepositoryPipeline.g.cs
        // 코드 내 namespace: MyApp;
    }

    [Fact]
    public Task Should_Handle_Deep_Namespace()
    {
        string input = """
            namespace A.B.C.D.E;

            [GeneratePipeline]
            public class UserRepository : IAdapter { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // 생성 파일: E.UserRepositoryPipeline.g.cs
        // 코드 내 namespace: A.B.C.D.E;
    }

    [Fact]
    public Task Should_Handle_Global_Namespace()
    {
        string input = """
            [GeneratePipeline]
            public class UserRepository : IAdapter { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // 생성 파일: UserRepositoryPipeline.g.cs
        // 코드 내 namespace: 없음 (글로벌)
    }
}
```

---

## 요약

| 상황 | 처리 방법 |
|------|-----------|
| 일반 네임스페이스 | `namespace X.Y.Z;` 선언 |
| 글로벌 네임스페이스 | namespace 선언 생략 |
| 파일명 충돌 | 접미사로 마지막 네임스페이스 사용 |
| 타입 참조 | `global::` 접두사 사용 |
| 긴 네임스페이스 | using 별칭 활용 |

| 메서드 | 용도 |
|--------|------|
| `IsGlobalNamespace` | 글로벌 여부 확인 |
| `ToString()` | 전체 네임스페이스 문자열 |
| `LastIndexOf('.')` | 마지막 세그먼트 추출 |

---

## 다음 단계

다음 섹션에서는 결정적 출력 보장 방법을 학습합니다.

➡️ [04. 결정적 출력](04-deterministic-output.md)
