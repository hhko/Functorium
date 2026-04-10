---
title: "Namespace 처리"
---

## 개요

생성된 코드가 원본 클래스와 같은 네임스페이스에 위치해야 partial class나 상속이 올바르게 작동합니다. 네임스페이스를 빠뜨리면 컴파일 오류가 발생하고, 서로 다른 네임스페이스에 같은 이름의 클래스가 있으면 생성 파일명이 충돌합니다. 이 장에서는 `ContainingNamespace`에서 정보를 추출하고, 글로벌 네임스페이스를 안전하게 처리하며, 파일명 충돌을 방지하는 실전 기법을 다룹니다.

## 학습 목표

### 핵심 학습 목표
1. **ContainingNamespace에서 정보 추출**
   - `IsGlobalNamespace` 확인과 문자열 변환
2. **글로벌 네임스페이스 처리**
   - namespace 선언 없이 정의된 타입에 대한 안전한 코드 생성
3. **파일명에 네임스페이스 접미사 활용**
   - `LastIndexOf('.')`를 이용한 마지막 세그먼트 추출로 충돌 방지

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
// public class UserRepositoryObservable : UserRepository
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
// UserRepositoryObservable.g.cs  (어느 것?)
```

### 네임스페이스 접미사 추출

```csharp
// ObservablePortGenerator.cs의 실제 코드
private static void Generate(
    SourceProductionContext context,
    ImmutableArray<ObservableClassInfo> pipelineClasses)
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
        string fileName = $"{namespaceSuffix}{pipelineClass.ClassName}Observable.g.cs";
        // "Repositories.UserRepositoryObservable.g.cs"

        context.AddSource(fileName, SourceText.From(source, Encoding.UTF8));
    }
}
```

### 예시

```
입력 클래스                              생성되는 파일명
==========                              ===============
MyApp.Repositories.UserRepository       Repositories.UserRepositoryObservable.g.cs
MyApp.Services.UserRepository           Services.UserRepositoryObservable.g.cs
MyApp.Data.UserRepository               Data.UserRepositoryObservable.g.cs
GlobalClass (글로벌 네임스페이스)        GlobalClassObservable.g.cs
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

            [GenerateObservablePort]
            public class UserRepository : IObservablePort { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // 생성 파일: UserRepositoryObservable.g.cs
        // 코드 내 namespace: MyApp;
    }

    [Fact]
    public Task Should_Handle_Deep_Namespace()
    {
        string input = """
            namespace A.B.C.D.E;

            [GenerateObservablePort]
            public class UserRepository : IObservablePort { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // 생성 파일: E.UserRepositoryObservable.g.cs
        // 코드 내 namespace: A.B.C.D.E;
    }

    [Fact]
    public Task Should_Handle_Global_Namespace()
    {
        string input = """
            [GenerateObservablePort]
            public class UserRepository : IObservablePort { }
            """;

        string? actual = _sut.Generate(input);
        return Verify(actual);
        // 생성 파일: UserRepositoryObservable.g.cs
        // 코드 내 namespace: 없음 (글로벌)
    }
}
```

---

## 한눈에 보는 정리

네임스페이스 처리의 핵심 전략을 정리합니다.

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

## FAQ

### Q1: 글로벌 네임스페이스에 정의된 클래스는 어떻게 처리하나요?
**A**: `IsGlobalNamespace` 속성으로 확인한 뒤, `true`이면 `namespace` 선언을 생략하고 바로 클래스를 정의합니다. 파일명에도 네임스페이스 접미사가 없으므로 `{ClassName}Observable.g.cs` 형태가 됩니다.

### Q2: `LastIndexOf('.')`로 마지막 세그먼트만 추출하면 충돌이 완전히 해결되나요?
**A**: 마지막 세그먼트까지 동일한 경우(예: `A.Repositories.UserRepository`와 `B.Repositories.UserRepository`)는 여전히 충돌할 수 있습니다. 실무에서는 이런 경우가 드물지만, 더 안전하게 하려면 전체 네임스페이스를 파일명에 포함시키는 방법을 고려할 수 있습니다. ObservablePortGenerator는 마지막 세그먼트 방식이 가독성과 안전성의 적절한 균형점이라 판단하여 이 전략을 채택했습니다.

### Q3: 생성된 코드에서 `global::` 접두사를 사용하는 이유는 무엇인가요?
**A**: 사용자 코드에 `System`이라는 이름의 클래스가 존재할 경우, `System.ArgumentNullException`이 사용자의 `System` 클래스를 참조하여 컴파일 오류가 발생합니다. `global::System.ArgumentNullException`으로 작성하면 항상 전역 `System` 네임스페이스를 정확히 참조하므로 이러한 충돌을 원천적으로 방지합니다.

---

네임스페이스 처리까지 마쳤으니 이제 코드 생성의 마지막 원칙을 다룰 차례입니다. 동일한 입력에 대해 항상 동일한 출력을 보장하는 결정적 출력은 증분 빌드, 소스 제어, CI/CD 모두에 영향을 미치는 핵심 요소입니다.

→ [12. 결정적 출력](../12-Deterministic-Output/)
