# 부록 D: 트러블슈팅

## 빌드 문제

### 문제: "Generator not found"

**증상:**
```
warning CS8785: Generator 'MyGenerator' failed to generate source.
```

**해결:**
1. `[Generator]` 속성 확인
2. `netstandard2.0` 타겟 프레임워크 확인
3. `IsRoslynComponent = true` 확인
4. 솔루션 재빌드

```xml
<PropertyGroup>
  <TargetFramework>netstandard2.0</TargetFramework>
  <IsRoslynComponent>true</IsRoslynComponent>
</PropertyGroup>
```

---

### 문제: "Assembly not found"

**증상:**
```
Could not load file or assembly 'Microsoft.CodeAnalysis, Version=...'
```

**해결:**
1. NuGet 패키지 버전 통일
2. `PrivateAssets="all"` 추가

```xml
<PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
```

---

### 문제: 생성된 파일이 표시 안 됨

**증상:**
- 빌드 성공하지만 생성된 파일이 안 보임
- IntelliSense가 생성된 타입을 인식 못함

**해결:**
1. 솔루션 탐색기 → "모든 파일 표시"
2. Dependencies → Analyzers → [Generator] 확인
3. `obj/Debug/*/generated` 폴더 확인
4. Visual Studio 재시작

---

## 런타임 문제

### 문제: ForAttributeWithMetadataName이 아무것도 반환 안 함

**증상:**
- Provider가 빈 결과 반환
- 생성 코드가 없음

**해결:**
1. 속성 전체 이름 확인 (네임스페이스 포함)
2. 속성이 실제로 적용되었는지 확인
3. predicate 조건 확인

```csharp
// ❌ 잘못된 예
ForAttributeWithMetadataName("GeneratePipeline", ...)

// ✅ 올바른 예
ForAttributeWithMetadataName("MyNamespace.GeneratePipelineAttribute", ...)
```

---

### 문제: INamedTypeSymbol이 null

**증상:**
```
NullReferenceException: INamedTypeSymbol was null
```

**해결:**
1. TargetSymbol 타입 캐스팅 확인
2. null 체크 추가

```csharp
if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
{
    return null;
}
```

---

### 문제: AllInterfaces가 비어 있음

**증상:**
- 인터페이스 구현을 찾지 못함

**해결:**
1. 직접 구현 vs 상속 구현 확인
2. `AllInterfaces` 사용 (직접 + 상속 모두 포함)

```csharp
// ❌ 직접 구현만 확인
classSymbol.Interfaces

// ✅ 상속 포함 모든 인터페이스
classSymbol.AllInterfaces
```

---

## 코드 생성 문제

### 문제: 네임스페이스 충돌

**증상:**
```
error CS0234: The type or namespace name 'X' does not exist in namespace 'Y'
```

**해결:**
`global::` 접두사 사용

```csharp
// ❌ 충돌 가능
sb.AppendLine("System.Int32 x = 0;");

// ✅ 안전
sb.AppendLine("global::System.Int32 x = 0;");
```

---

### 문제: 비결정적 출력

**증상:**
- 매 빌드마다 생성 파일 내용이 달라짐
- Git에 불필요한 변경 발생

**해결:**
1. `.OrderBy()` 사용하여 순서 정렬
2. 타임스탬프, GUID 제거
3. SymbolDisplayFormat 일관성 유지

```csharp
// ❌ 순서 불확정
var methods = classSymbol.GetMembers().OfType<IMethodSymbol>();

// ✅ 순서 정렬
var methods = classSymbol.GetMembers()
    .OfType<IMethodSymbol>()
    .OrderBy(m => m.Name)
    .ThenBy(m => m.Parameters.Length);
```

---

### 문제: 들여쓰기 불일치

**증상:**
- 생성된 코드 포맷이 일관되지 않음

**해결:**
스페이스 또는 탭 중 하나만 일관되게 사용

```csharp
// ✅ 일관된 4-스페이스 들여쓰기
sb.AppendLine("    private readonly int _id;");
sb.AppendLine("    private readonly string _name;");
```

---

## 테스트 문제

### 문제: Verify 테스트 실패

**증상:**
```
Verify mismatch: Received vs Verified
```

**해결:**
1. `.received.txt` 파일 내용 확인
2. 의도적 변경이면 승인

```bash
# received를 verified로 이름 변경
mv *.received.txt *.verified.txt
```

---

### 문제: CSharpCompilation 참조 오류

**증상:**
```
error CS0012: The type 'X' is defined in an assembly that is not referenced
```

**해결:**
필수 어셈블리 참조 추가

```csharp
private static readonly Type[] RequiredTypes =
[
    typeof(object),                                   // System.Runtime
    typeof(LanguageExt.IO),                           // LanguageExt.Core
    typeof(Microsoft.Extensions.Logging.ILogger),    // Logging
];

var references = RequiredTypes
    .Select(t => t.Assembly)
    .Distinct()
    .Select(a => MetadataReference.CreateFromFile(a.Location));
```

---

### 문제: 테스트에서 생성 결과가 null

**증상:**
```csharp
string? actual = _sut.Generate(input);
actual.ShouldNotBeNull();  // 실패
```

**해결:**
1. 입력 코드에 필요한 using 문 확인
2. `[GeneratePipeline]` 속성 적용 확인
3. IAdapter 인터페이스 구현 확인

```csharp
string input = """
    using Functorium.Adapters.SourceGenerator;  // 필수
    using Functorium.Applications.Observabilities;  // IAdapter
    using LanguageExt;  // FinT, IO

    namespace TestNamespace;

    public interface ITestAdapter : IAdapter  // IAdapter 상속
    {
        FinT<IO, int> GetValue();
    }

    [GeneratePipeline]  // 속성 적용
    public class TestAdapter : ITestAdapter
    {
        public virtual FinT<IO, int> GetValue() => ...;
    }
    """;
```

---

## 디버깅 팁

### Debugger.Launch() 사용

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
#if DEBUG
    if (!System.Diagnostics.Debugger.IsAttached)
    {
        System.Diagnostics.Debugger.Launch();
    }
#endif
    // ...
}
```

### 진단 메시지 출력

```csharp
context.ReportDiagnostic(Diagnostic.Create(
    new DiagnosticDescriptor(
        "SG001",
        "Debug",
        "Processing: {0}",
        "Debug",
        DiagnosticSeverity.Warning,
        true),
    Location.None,
    className));
```

### 로그 파일 작성 (개발 중에만)

```csharp
#if DEBUG
File.AppendAllText(@"C:\temp\generator.log", $"{DateTime.Now}: {message}\n");
#endif
```

---

## FAQ

### Q: netstandard2.0 대신 net6.0을 사용할 수 없나요?

**A:** 아니요. 소스 생성기는 컴파일러 확장이므로 `netstandard2.0`을 사용해야 합니다. 이는 Roslyn 컴파일러와의 호환성을 위한 것입니다.

---

### Q: Source Generator와 Analyzer의 차이점은?

**A:**
- **Analyzer**: 코드 분석, 경고/오류 보고
- **Source Generator**: 새로운 코드 생성

둘 다 Roslyn 기반이지만 목적이 다릅니다.

---

### Q: 증분 생성기를 꼭 사용해야 하나요?

**A:** .NET 6 이상에서는 `IIncrementalGenerator`를 권장합니다. 이전 `ISourceGenerator`보다 성능이 훨씬 좋고, 캐싱을 지원합니다.

---

### Q: 생성된 코드를 수정할 수 있나요?

**A:** 아니요. 생성된 코드는 빌드 시마다 덮어쓰여집니다. 대신 `partial class`를 사용하여 확장하세요.

---

➡️ [README로 돌아가기](../README.md)
