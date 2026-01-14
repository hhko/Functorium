# Source Generator DiagnosticDescriptor 개선

## 개요

Source Generator에서 `#error` 지시문 대신 `DiagnosticDescriptor` 기반 IDE 친화적 진단으로 개선했습니다.

## 문제점

### 기존 방식: `#error` 지시문

```csharp
private static void GenerateErrorSource(StringBuilder sb, PipelineClassInfo classInfo, List<string> duplicateTypes)
{
    sb.Append(Header)
        .AppendLine()
        .AppendLine($"namespace {classInfo.Namespace};")
        .AppendLine()
        .AppendLine($"#error Pipeline constructor for '{classInfo.ClassName}' contains multiple parameters...")
        .AppendLine()
        .AppendLine($"public class {classInfo.ClassName}Pipeline")
        .AppendLine("{")
        .AppendLine("}");
}
```

### 문제점

| 항목 | 설명 |
|------|------|
| 위치 정보 | 생성된 `.g.cs` 파일을 가리킴 (원본 소스 아님) |
| 클릭 가능 | Error List에서 클릭해도 원본 파일로 이동 불가 |
| 에러 형식 | 컴파일러 에러로만 표시됨 |
| IDE 통합 | 제한적 |

## 해결 방안

### 1. `PipelineClassInfo`에 Location 추가

```csharp
public readonly record struct PipelineClassInfo
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly List<MethodInfo> Methods;
    public readonly List<ParameterInfo> BaseConstructorParameters;
    public readonly Location? Location;  // 추가

    public PipelineClassInfo(
        string @namespace,
        string className,
        List<MethodInfo> methods,
        List<ParameterInfo> baseConstructorParameters,
        Location? location)  // 파라미터 추가
    {
        // ...
        Location = location;
    }
}
```

### 2. Location 캡처

```csharp
private static PipelineClassInfo MapToPipelineClassInfo(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken)
{
    // ... 기존 코드 ...

    // 원본 소스 위치 (IDE 진단용)
    Location? location = context.TargetNode.GetLocation();

    return new PipelineClassInfo(@namespace, className, methods, baseConstructorParameters, location);
}
```

### 3. `ReportDiagnostic` 적용

```csharp
// DiagnosticDescriptor 정의 (기존)
private static readonly DiagnosticDescriptor DuplicateParameterTypeDiagnostic = new(
    id: "FUNCTORIUM001",
    title: "Duplicate parameter types in pipeline constructor",
    messageFormat: "Pipeline constructor for '{0}' contains multiple parameters of the same type '{1}'...",
    category: "Design",
    DiagnosticSeverity.Error,
    isEnabledByDefault: true);

// Generate 메서드에서 사용
if (duplicateTypes.Any())
{
    // IDE 친화적 진단 리포트 (원본 소스 파일 위치 표시)
    context.ReportDiagnostic(Diagnostic.Create(
        DuplicateParameterTypeDiagnostic,
        pipelineClass.Location,
        pipelineClass.ClassName,
        string.Join(", ", duplicateTypes)));

    continue;  // 에러 시 코드 생성 건너뛰기
}
```

## 비교

| 항목 | Before (`#error`) | After (`DiagnosticDescriptor`) |
|------|-------------------|-------------------------------|
| 에러 위치 | 생성된 .g.cs 파일 | 원본 소스 파일 (클래스 선언 위치) |
| 클릭 가능 | X | O (원본 파일로 이동) |
| Error List 표시 | 컴파일러 에러 | Analyzer 진단 (FUNCTORIUM001) |
| 에러 ID | 없음 | FUNCTORIUM001 |
| 에러 카테고리 | 없음 | Design |
| 코드 생성 | 에러 포함 빈 클래스 생성 | 코드 생성 건너뛰기 |

## IDE 경험 개선

### Before
```
Error CS1029: #error: 'Pipeline constructor for 'MyAdapter' contains multiple parameters...'
    MyAdapterPipeline.g.cs (line 5)  <- 생성된 파일 위치
```

### After
```
Error FUNCTORIUM001: Pipeline constructor for 'MyAdapter' contains multiple parameters of the same type 'ILogger'...
    MyAdapter.cs (line 10)  <- 원본 소스 파일 위치 (클릭 가능)
```

## 관련 파일

- `Src/Functorium.Adapters.SourceGenerator/AdapterPipelineGenerator.cs`
- `Src/Functorium.Adapters.SourceGenerator/Generators/AdapterPipelineGenerator/PipelineClassInfo.cs`

## 참고 자료

- [Roslyn Analyzers - Creating Diagnostics](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/tutorials/how-to-write-csharp-analyzer-code-fix)
- [DiagnosticDescriptor Class](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.diagnosticdescriptor)
