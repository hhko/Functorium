# StringBuilder 패턴

## 학습 목표

- StringBuilder를 활용한 효율적인 코드 생성
- 메서드 체이닝 패턴 적용
- 메모리 효율적인 문자열 조합

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
sb.AppendLine("public class UserPipeline")
  .AppendLine("{")
  .AppendLine("    private readonly ILogger _logger;")  // 4칸 들여쓰기
  .AppendLine()
  .AppendLine("    public UserPipeline(ILogger logger)")
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
// AdapterPipelineGenerator.cs의 실제 코드
private static string GeneratePipelineClassSource(
    PipelineClassInfo classInfo,
    StringBuilder sb)
{
    sb.Append(Header)
      .AppendLine()
      .AppendLine("using Functorium.Abstractions;")
      .AppendLine("using Functorium.Applications.Observabilities;")
      .AppendLine()
      .AppendLine("using LanguageExt;")
      .AppendLine("using Microsoft.Extensions.Logging;")
      .AppendLine("using System.Diagnostics;")
      .AppendLine()
      .AppendLine($"namespace {classInfo.Namespace};")
      .AppendLine()
      .AppendLine($"public class {classInfo.ClassName}Pipeline : {classInfo.ClassName}")
      .AppendLine("{");

    // 필드 생성
    GenerateFields(sb, classInfo);

    // 생성자 생성
    GenerateConstructor(sb, classInfo);

    // 메서드들 생성
    foreach (var method in classInfo.Methods)
    {
        GenerateMethod(sb, classInfo, method);
    }

    sb.AppendLine("}")
      .AppendLine();

    return sb.ToString();
}
```

### 필드 생성

```csharp
private static void GenerateFields(StringBuilder sb, PipelineClassInfo classInfo)
{
    sb.AppendLine("    private readonly ActivityContext _parentContext;")
      .AppendLine()
      .AppendLine($"    private readonly ILogger<{classInfo.ClassName}Pipeline> _logger;")
      .AppendLine("    private readonly IAdapterTrace _adapterTrace;")
      .AppendLine("    private readonly IAdapterMetric _adapterMetric;")
      .AppendLine()
      .AppendLine($"    private const string RequestHandler = nameof({classInfo.ClassName});")
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
// Raw String Literals로 가독성 향상
public const string Header = """
    // <auto-generated/>
    // This code was generated by AdapterPipelineGenerator.
    // Do not modify this file directly.

    #nullable enable

    """;

// 보간 Raw String Literals
private static string GenerateClass(string className, string @namespace)
{
    return $$"""
        namespace {{@namespace}};

        public class {{className}}Pipeline
        {
            // ...
        }
        """;
}
```

### 혼합 사용

```csharp
// StringBuilder + Raw String Literals 혼합
private static string GeneratePipelineClass(PipelineClassInfo classInfo)
{
    var sb = new StringBuilder();

    // 고정 부분: Raw String Literal
    sb.Append("""
        // <auto-generated/>
        #nullable enable

        using Functorium.Abstractions;
        using LanguageExt;

        """);

    // 동적 부분: StringBuilder
    sb.AppendLine($"namespace {classInfo.Namespace};");
    sb.AppendLine();
    sb.AppendLine($"public class {classInfo.ClassName}Pipeline");
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

## 요약

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

## 다음 단계

다음 섹션에서는 코드 템플릿 설계를 학습합니다.

➡️ [02. 템플릿 설계](02-template-design.md)
