---
title: "StringBuilder Pattern"
---

## 개요

앞 장까지 심볼 분석과 타입 추출로 코드 생성에 필요한 모든 데이터를 확보했습니다. 이제 그 데이터를 실제 C# 소스 코드 문자열로 조립할 차례입니다. ObservablePortGenerator는 각 클래스에 대해 필드, 생성자, 래퍼 메서드, 로깅 메서드를 모두 생성하므로 출력이 수천 줄에 달할 수 있습니다. 문자열 연결(`+` 연산자)은 매번 새 객체를 생성하여 `O(n^2)` 메모리를 소비하지만, `StringBuilder`는 내부 버퍼를 재사용하여 `O(n)`으로 처리합니다.

## 학습 목표

### 핵심 학습 목표
1. **StringBuilder의** 효율성을 이해하고 기본 API를 활용한다
   - Append, AppendLine, Clear, ToString의 역할
2. **메서드 체이닝 패턴으로** 가독성 높은 코드 생성 로직을 작성한다
3. **Raw String Literals와의** 혼합 사용 패턴을 학습한다
   - 고정 부분은 리터럴로, 동적 부분은 StringBuilder로

---

## 왜 StringBuilder인가?

### 문자열 연결의 비효율성

```csharp
// ❌ 문자열 연결 (비효율적)
string code = "";
code = code + "public class " + className + "\n";
code = code + "{\n";
code = code + "    // 멤버들\n";
code = code + "}\n";

// 문제: 매번 새로운 문자열 객체 생성
// 메모리: O(n²) - n은 연결 횟수
```

### StringBuilder의 효율성

```csharp
// ✅ StringBuilder (효율적)
var sb = new StringBuilder();
sb.Append("public class ").Append(className).AppendLine();
sb.AppendLine("{");
sb.AppendLine("    // 멤버들");
sb.AppendLine("}");
string code = sb.ToString();

// 장점: 내부 버퍼 재사용
// 메모리: O(n) - 선형 증가
```

---

## 기본 사용법

### Append vs AppendLine

```csharp
var sb = new StringBuilder();

// Append: 줄바꿈 없이 추가
sb.Append("public ");
sb.Append("class ");
sb.Append("User");
// → "public class User"

// AppendLine: 줄바꿈 포함 추가
sb.AppendLine("public class User");
// → "public class User\n"

// AppendLine(): 빈 줄 추가
sb.AppendLine();
// → "\n"
```

### 메서드 체이닝

```csharp
// 메서드 체이닝으로 가독성 향상
sb.Append("public class ")
  .Append(className)
  .AppendLine()
  .AppendLine("{")
  .AppendLine("}")
  .AppendLine();
```

---

## 코드 생성 패턴

### 들여쓰기 관리

```csharp
// 수동 들여쓰기 (Functorium 방식)
sb.AppendLine("public class UserObservable")
  .AppendLine("{")
  .AppendLine("    private readonly ILogger _logger;")  // 4칸 들여쓰기
  .AppendLine()
  .AppendLine("    public UserObservable(ILogger logger)")
  .AppendLine("    {")
  .AppendLine("        _logger = logger;")  // 8칸 들여쓰기
  .AppendLine("    }")
  .AppendLine("}");
```

### 들여쓰기 헬퍼 (선택적)

```csharp
// 들여쓰기 레벨 관리
public class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel = 0;
    private const string IndentString = "    ";  // 4칸

    public void Indent() => _indentLevel++;
    public void Unindent() => _indentLevel--;

    public void AppendLine(string line)
    {
        for (int i = 0; i < _indentLevel; i++)
            _sb.Append(IndentString);
        _sb.AppendLine(line);
    }
}

// 사용
var isb = new IndentedStringBuilder();
isb.AppendLine("public class User");
isb.AppendLine("{");
isb.Indent();
isb.AppendLine("private int _id;");
isb.Unindent();
isb.AppendLine("}");
```

---

## Functorium 코드 생성 예시

### 클래스 생성

```csharp
// ObservablePortGenerator.cs의 실제 코드
private static string GenerateObservableClassSource(
    ObservableClassInfo classInfo,
    StringBuilder sb)
{
    sb.Append(Header)
      .AppendLine()
      .AppendLine("using System.Diagnostics;")
      .AppendLine("using System.Diagnostics.Metrics;")
      .AppendLine("using Functorium.Adapters.Observabilities;")
      .AppendLine("using Functorium.Adapters.Observabilities.Naming;")
      .AppendLine("using Functorium.Abstractions.Observabilities;")
      .AppendLine()
      .AppendLine("using LanguageExt;")
      .AppendLine("using Microsoft.Extensions.Logging;")
      .AppendLine("using Microsoft.Extensions.Options;")
      .AppendLine()
      .AppendLine($"namespace {classInfo.Namespace};")
      .AppendLine()
      .AppendLine($"public class {classInfo.ClassName}Observable : {classInfo.ClassName}")
      .AppendLine("{");

    // 필드 생성
    GenerateFields(sb, classInfo);

    // 생성자 생성
    GenerateConstructor(sb, classInfo);

    // 헬퍼 메서드들 추가
    GenerateHelperMethods(sb, classInfo);

    // 메서드들 생성
    foreach (var method in classInfo.Methods)
    {
        GenerateMethod(sb, classInfo, method);
    }

    sb.AppendLine("}")
      .AppendLine()
      .AppendLine($"internal static class {classInfo.ClassName}ObservableLoggers")
      .AppendLine("{");

    // 로깅 확장 메서드들 생성
    foreach (var method in classInfo.Methods)
    {
        GenerateLoggingMethods(sb, classInfo, method);
    }

    sb.AppendLine("}")
      .AppendLine();

    return sb.ToString();
}
```

### 필드 생성

```csharp
private static void GenerateFields(StringBuilder sb, ObservableClassInfo classInfo)
{
    sb.AppendLine("    private readonly ActivitySource _activitySource;")
      .AppendLine($"    private readonly ILogger<{classInfo.ClassName}Observable> _logger;")
      .AppendLine()
      .AppendLine("    // Metrics")
      .AppendLine("    private readonly Counter<long> _requestCounter;")
      .AppendLine("    private readonly Counter<long> _responseCounter;")
      .AppendLine("    private readonly Histogram<double> _durationHistogram;")
      .AppendLine()
      .AppendLine($"    private const string RequestHandler = nameof({classInfo.ClassName});")
      .AppendLine()
      .AppendLine("    private readonly string _requestCategoryLowerCase;")
      .AppendLine()
      .AppendLine("    private readonly bool _isDebugEnabled;")
      .AppendLine("    private readonly bool _isInformationEnabled;")
      .AppendLine("    private readonly bool _isWarningEnabled;")
      .AppendLine("    private readonly bool _isErrorEnabled;")
      .AppendLine();
}
```

### 동적 파라미터 생성

```csharp
// 메서드 파라미터 목록 생성
private static string GenerateParameterList(List<ParameterInfo> parameters)
{
    var sb = new StringBuilder();

    for (int i = 0; i < parameters.Count; i++)
    {
        var param = parameters[i];

        if (i > 0) sb.Append(", ");

        // ref/out/in 키워드
        if (param.RefKind != RefKind.None)
        {
            sb.Append(param.RefKind.ToString().ToLower())
              .Append(' ');
        }

        sb.Append(param.Type)
          .Append(' ')
          .Append(param.Name);
    }

    return sb.ToString();
}

// 사용
// 입력: [("int", "id"), ("string", "name")]
// 출력: "int id, string name"
```

---

## Raw String Literals (C# 11+)

### 템플릿 기반 생성

```csharp
// Verbatim String Literal
public const string Header = @"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by source generator
//
//     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

#nullable enable
";

// 보간 Raw String Literals
private static string GenerateClass(string className, string @namespace)
{
    return $$"""
        namespace {{@namespace}};

        public class {{className}}Observable
        {
            // ...
        }
        """;
}
```

### 혼합 사용

```csharp
// StringBuilder + Raw String Literals 혼합
private static string GenerateObservableClass(ObservableClassInfo classInfo)
{
    var sb = new StringBuilder();

    // 고정 부분: Raw String Literal
    sb.Append("""
        // <auto-generated/>
        #nullable enable

        using System.Diagnostics;
        using Functorium.Adapters.Observabilities.Naming;
        using LanguageExt;

        """);

    // 동적 부분: StringBuilder
    sb.AppendLine($"namespace {classInfo.Namespace};");
    sb.AppendLine();
    sb.AppendLine($"public class {classInfo.ClassName}Observable");
    sb.AppendLine("{");

    // 메서드들 생성
    foreach (var method in classInfo.Methods)
    {
        GenerateMethod(sb, method);
    }

    sb.AppendLine("}");

    return sb.ToString();
}
```

---

## 성능 최적화

### 초기 용량 지정

```csharp
// 예상 크기를 알면 초기 용량 지정
var sb = new StringBuilder(capacity: 4096);

// 또는 대략적인 추정
int estimatedSize = classInfo.Methods.Count * 500;  // 메서드당 ~500자
var sb = new StringBuilder(estimatedSize);
```

### 재사용

```csharp
// ❌ 매번 새로 생성
foreach (var classInfo in classes)
{
    var sb = new StringBuilder();  // 매번 할당
    GenerateCode(sb, classInfo);
}

// ✅ 재사용
var sb = new StringBuilder();
foreach (var classInfo in classes)
{
    sb.Clear();  // 내용만 지우고 버퍼 재사용
    GenerateCode(sb, classInfo);
}
```

---

## 한눈에 보는 정리

`StringBuilder`는 소스 생성기에서 코드를 조립하는 핵심 도구입니다. 메서드 체이닝으로 가독성을 높이고, 여러 클래스를 생성할 때는 `Clear()`로 버퍼를 재사용하여 메모리 효율을 극대화합니다. Functorium 프로젝트에서는 고정 부분에 Raw String Literals를, 동적 부분에 StringBuilder를 혼합하여 사용합니다.

| 메서드 | 용도 | 줄바꿈 |
|--------|------|--------|
| `Append()` | 문자열 추가 | 없음 |
| `AppendLine()` | 문자열 + 줄바꿈 | 있음 |
| `AppendLine("")` | 빈 줄 | 있음 |
| `Clear()` | 내용 초기화 | - |
| `ToString()` | 결과 문자열 | - |

| 패턴 | 설명 |
|------|------|
| 메서드 체이닝 | `.Append().Append().AppendLine()` |
| 수동 들여쓰기 | `"    "` 접두사로 직접 관리 |
| Raw String Literals | 고정 템플릿에 사용 |
| 초기 용량 | 큰 출력 예상 시 지정 |

---

## FAQ

### Q1: `StringBuilder`를 `Clear()`로 재사용하는 것과 매번 새로 생성하는 것의 차이는 무엇인가요?
**A**: `Clear()`는 내용만 지우고 내부 버퍼를 유지합니다. 여러 클래스에 대해 순차적으로 코드를 생성할 때 버퍼 재할당이 발생하지 않아 GC 압력이 줄어듭니다. Functorium에서 `Collect()`로 모든 클래스를 모아 처리하는 이유 중 하나가 이 `StringBuilder` 재사용입니다.

### Q2: Raw String Literals와 `StringBuilder`를 혼합하는 기준은 무엇인가요?
**A**: `using` 문, 헤더, `#nullable enable` 같은 **고정된** 텍스트는 Raw String Literals로 작성하면 가독성이 높습니다. 클래스명, 메서드명, 타입명 등 **동적으로** 변하는 부분은 `StringBuilder`의 `Append`/`AppendLine`으로 조립합니다. Functorium의 `Header` 상수가 이 혼합 패턴의 대표적 예시입니다.

### Q3: `StringBuilder`에 초기 용량을 지정하면 얼마나 효과가 있나요?
**A**: 기본 용량은 16자로 시작하여 필요할 때마다 2배씩 확장됩니다. 생성 코드가 수천 줄이라면 확장이 여러 번 발생하여 불필요한 메모리 복사가 일어납니다. 메서드 수를 기반으로 대략적인 크기를 추정하여 초기 용량을 지정하면 이 오버헤드를 줄일 수 있습니다.

---

`StringBuilder`로 코드를 한 줄씩 조립하는 방법을 익혔습니다. 그런데 수백 줄의 생성 로직이 하나의 메서드에 뒤섞이면 유지보수가 어려워집니다. 다음 장에서는 헤더, 필드, 생성자, 메서드 등 고정 부분과 동적 부분을 계층적으로 분리하는 템플릿 설계를 살펴봅니다.

→ [10. 템플릿 설계](../10-Template-Design/)
