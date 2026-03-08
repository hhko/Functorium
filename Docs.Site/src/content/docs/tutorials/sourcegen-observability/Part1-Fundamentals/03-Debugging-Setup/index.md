---
title: "Debugging 설정"
---

## 개요

소스 생성기 코드에 버그가 있을 때, 일반 애플리케이션처럼 F5를 눌러 디버깅할 수 없습니다. 소스 생성기는 런타임이 아닌 **컴파일 타임**에 실행되기 때문입니다. 컴파일러 프로세스에 디버거를 연결해야 하므로 별도의 설정이 필요하며, 이를 모르면 `Console.WriteLine`으로 생성된 코드를 확인하는 비효율적인 디버깅에 의존하게 됩니다.

이 장에서는 세 가지 디버깅 방법을 소개하고, 그중 테스트 프로젝트 기반 디버깅이 왜 가장 실용적인지 설명합니다.

## 학습 목표

### 핵심 학습 목표
1. **소스 생성기 디버깅의 특수성 이해**
   - 컴파일 타임 실행이라는 제약이 디버깅 방식에 미치는 영향
2. **`Debugger.Launch()` 사용법 습득**
   - 긴급 상황에서 JIT 디버거를 활용하는 방법
3. **테스트 프로젝트를 활용한 디버깅 기법 학습**
   - 재현 가능하고 격리된 환경에서의 반복 디버깅

---

## 소스 생성기 디버깅의 특수성

소스 생성기는 **컴파일 타임**에 실행되므로, 일반적인 애플리케이션 디버깅과는 다른 접근이 필요합니다.

```
일반 애플리케이션 디버깅
=======================
개발자 → F5 → 런타임 실행 → 브레이크포인트

소스 생성기 디버깅
=================
개발자 → 빌드 → 컴파일러 실행 → 소스 생성기 실행 → 브레이크포인트
                    ↑
              여기에 디버거를 연결해야 함
```

---

## 디버깅 방법 개요

| 방법 | 난이도 | 안정성 | 권장 상황 |
|------|--------|--------|-----------|
| Debugger.Launch() | 쉬움 | 높음 | 빠른 디버깅 |
| 테스트 프로젝트 | 쉬움 | **매우 높음** | **권장 (기본)** |
| Attach to Process | 어려움 | 낮음 | 특수 상황 |

---

## 방법 1: Debugger.Launch() 사용

가장 직관적인 방법은 코드에서 직접 디버거 연결을 요청하는 것입니다. 우리 프로젝트의 `IncrementalGeneratorBase`는 이를 `AttachDebugger` 파라미터로 추상화해 두었습니다.

### IncrementalGeneratorBase 활용

Functorium 프로젝트는 `AttachDebugger` 파라미터를 통해 디버깅을 지원합니다:

```csharp
// IncrementalGeneratorBase.cs
public abstract class IncrementalGeneratorBase<TValue>(
    Func<IncrementalGeneratorInitializationContext,
         IncrementalValuesProvider<TValue>> registerSourceProvider,
    Action<SourceProductionContext, ImmutableArray<TValue>> generate,
    bool AttachDebugger = false)  // ← 디버깅 플래그
    : IIncrementalGenerator
{
    private readonly bool _attachDebugger = AttachDebugger;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        // DEBUG 빌드에서만 디버거 연결 지원
        // 디버깅 필요 시 ObservablePortGenerator에서 AttachDebugger: true로 설정
        if (_attachDebugger && Debugger.IsAttached is false)
        {
            Debugger.Launch();  // ← JIT 디버거 대화상자 표시
        }
#endif

        IncrementalValuesProvider<TValue> provider = registerSourceProvider(context)
            .Where(static m => m is not null);

        context.RegisterSourceOutput(provider.Collect(), Execute);
    }

    private void Execute(SourceProductionContext context, ImmutableArray<TValue> values)
    {
        generate(context, values);
    }
}
```

### 디버깅 활성화

```csharp
// ObservablePortGenerator.cs
[Generator(LanguageNames.CSharp)]
public sealed class ObservablePortGenerator()
    : IncrementalGeneratorBase<ObservableClassInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: true)  // 🔧 true로 변경
```

### 디버깅 흐름

```
1. AttachDebugger: true 설정
              ↓
2. 솔루션 빌드 (Ctrl+Shift+B)
              ↓
3. "Just-In-Time Debugger" 대화상자 표시
              ↓
4. Visual Studio 인스턴스 선택
              ↓
5. 브레이크포인트에서 실행 중지
              ↓
6. 디버깅 완료 후 AttachDebugger: false 복원
```

### 주의사항

```
⚠️ 중요: 디버깅 완료 후 반드시 false로 복원

AttachDebugger: true 상태로 커밋하면:
- 모든 팀원의 빌드에서 디버거 대화상자 표시
- CI/CD 파이프라인 실패 (대화상자 대기로 인한 타임아웃)
```

---

## 방법 2: 테스트 프로젝트에서 디버깅 (권장)

`Debugger.Launch()`는 빠르지만 일회성입니다. 실제 개발에서는 동일한 입력으로 반복 디버깅할 수 있는 환경이 필요합니다. 테스트 프로젝트 기반 디버깅은 이 문제를 해결합니다.

### 장점

- 안정적: 컴파일러 프로세스 타이밍 문제 없음
- 반복 가능: 동일한 입력으로 여러 번 테스트
- 격리된 환경: 다른 프로젝트에 영향 없음
- 빠른 피드백: 전체 빌드 필요 없음

### SourceGeneratorTestRunner 활용

```csharp
// SourceGeneratorTestRunner.cs
public static class SourceGeneratorTestRunner
{
    public static string? Generate<TGenerator>(
        this TGenerator generator,
        string sourceCode)
        where TGenerator : IIncrementalGenerator, new()
    {
        // 1. 소스 코드 파싱
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

        // 2. 컴파일레이션 생성
        var compilation = CSharpCompilation.Create(
            "SourceGeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // 3. 소스 생성기 실행
        CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(
                compilation,
                out var outputCompilation,
                out var diagnostics);

        // 4. 생성된 코드 반환
        return outputCompilation.SyntaxTrees
            .Skip(1)  // 원본 제외
            .LastOrDefault()?
            .ToString();
    }
}
```

### 테스트 코드에서 디버깅

```csharp
[Fact]
public Task Should_Generate_Observable_For_Simple_Adapter()
{
    // Arrange
    string input = """
        using Functorium.Adapters.SourceGenerators;
        using LanguageExt;

        namespace MyApp.Adapters;

        public interface IUserRepository : IObservablePort
        {
            FinT<IO, User> GetUserAsync(int id);
        }

        [GenerateObservablePort]
        public class UserRepository : IUserRepository
        {
            public FinT<IO, User> GetUserAsync(int id) => throw new NotImplementedException();
        }
        """;

    // Act - 여기에 브레이크포인트 설정!
    string? actual = _sut.Generate(input);  // ← F11로 소스 생성기 내부 진입

    // Assert
    return Verify(actual);
}
```

### Visual Studio에서 테스트 디버깅

```
1. 테스트 메서드에 브레이크포인트 설정

2. 소스 생성기 코드에 브레이크포인트 설정
   - ObservablePortGenerator.cs: MapToObservableClassInfo()
   - ObservablePortGenerator.cs: Generate()

3. Test Explorer 열기 (Ctrl+E, T)

4. 테스트 우클릭 → "Debug" 선택

5. 브레이크포인트에서 중지

6. F11 (Step Into)로 소스 생성기 내부 진입
```

---

## 방법 3: Attach to Process

### 사용 시나리오

- 실제 프로젝트 빌드 시 문제 발생
- Debugger.Launch()가 작동하지 않는 환경

### 절차

```
1. 명령줄에서 빌드 시작 (--no-incremental 옵션)
   dotnet build MyProject.csproj --no-incremental

2. Visual Studio에서 Attach to Process (Ctrl+Alt+P)

3. 프로세스 검색: "csc" 또는 "VBCSCompiler"

4. 프로세스 선택 후 Attach

5. 브레이크포인트에서 중지
```

### 단점

```
⚠️ 권장하지 않는 이유:

- 컴파일러 프로세스가 빠르게 종료됨
- 타이밍을 맞추기 매우 어려움
- 반복 디버깅이 어려움
```

---

## 디버깅 시 유용한 팁

### 1. 생성된 코드 확인

Visual Studio에서 생성된 코드를 직접 볼 수 있습니다:

```
Solution Explorer
→ Dependencies
→ Analyzers
→ Functorium.SourceGenerators
→ Functorium.SourceGenerators.ObservablePortGenerator
   → GenerateObservablePortAttribute.g.cs
   → Repositories.UserRepositoryObservable.g.cs
   → ...
```

### 2. Watch 창 활용

디버깅 중 유용한 표현식:

```csharp
// 클래스 전체 이름
classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
// → "global::MyApp.Adapters.UserRepository"

// 모든 인터페이스
classSymbol.AllInterfaces.Select(i => i.Name).ToArray()
// → ["IUserRepository", "IObservablePort"]

// 메서드 시그니처
method.ToDisplayString()
// → "GetUserAsync(int)"

// 파라미터 타입
method.Parameters.Select(p => p.Type.ToDisplayString()).ToArray()
// → ["int"]

// 반환 타입
method.ReturnType.ToDisplayString()
// → "LanguageExt.FinT<LanguageExt.IO, User>"
```

### 3. 조건부 브레이크포인트

특정 조건에서만 중지:

```
브레이크포인트 우클릭 → Conditions

조건 예시:
- className == "UserRepository"
- method.Name == "GetUserAsync"
- method.Parameters.Length > 2
```

### 4. 빌드 로그 확인

```bash
# 상세 로그 생성
dotnet build MyProject.csproj -v:diag > build.log

# 소스 생성기 관련 로그 검색
grep -i "sourcegenerator" build.log
```

---

## 문제 해결

### 문제 1: 브레이크포인트가 작동하지 않음

**증상**: 브레이크포인트가 빈 원으로 표시됨

**해결**:

```bash
# 1. 빌드 캐시 삭제
rm -rf bin obj

# 2. 솔루션 정리 후 재빌드
dotnet clean
dotnet build
```

### 문제 2: 코드 변경이 반영되지 않음

**증상**: 소스 생성기 수정 후에도 이전 코드가 생성됨

**해결**:

```
1. Visual Studio 완전 종료 (중요!)

2. 모든 bin, obj 폴더 삭제:
   Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force

3. Visual Studio 재시작

4. Clean → Rebuild
```

### 문제 3: 테스트에서 소스 생성기 내부로 진입 불가

**해결**: 테스트 프로젝트에서 소스 생성기 참조 확인

```xml
<ProjectReference
    Include="..\MySourceGenerator\MySourceGenerator.csproj"
    ReferenceOutputAssembly="true" />  ← true 확인
```

---

## 권장 워크플로우

```
일반 개발
=========
1. 테스트 프로젝트에서 디버깅 (방법 2) ← 기본
2. 새 테스트 케이스 작성
3. 반복 디버깅으로 문제 해결

긴급 상황
=========
1. Debugger.Launch() 사용 (방법 1)
2. 문제 파악 후 즉시 false로 복원

확인 작업
=========
1. Solution Explorer → Analyzers에서 생성 코드 확인
2. 빌드 로그 분석
```

---

## 요약

세 가지 디버깅 방법 중 테스트 프로젝트 기반 디버깅이 안정성과 반복성 면에서 가장 실용적입니다. `Debugger.Launch()`는 긴급 상황에서만 사용하고, Watch 창의 `ToDisplayString()` 표현식은 심볼 상태를 파악하는 데 핵심적인 도구입니다.

| 항목 | 권장 방법 |
|------|-----------|
| 기본 디버깅 | 테스트 프로젝트 활용 |
| 빠른 확인 | Debugger.Launch() (임시) |
| 생성 코드 확인 | Solution Explorer → Analyzers |
| 디버깅 표현식 | classSymbol.ToDisplayString() 등 |

---

## FAQ

### Q1: `Debugger.Launch()`를 프로덕션 코드에 남겨두면 어떻게 되나요?
**A**: `#if DEBUG` 전처리기 지시문으로 감싸져 있으므로 Release 빌드에는 포함되지 않습니다. 하지만 Debug 빌드에서 의도치 않게 디버거 대화상자가 뜰 수 있으므로, 문제 해결 후 반드시 `false`로 되돌리거나 해당 코드를 비활성화해야 합니다.

### Q2: 테스트 프로젝트 기반 디버깅이 `Debugger.Launch()`보다 권장되는 이유는 무엇인가요?
**A**: 테스트 프로젝트에서는 `CSharpCompilation`으로 격리된 컴파일 환경을 만들어 생성기를 실행합니다. 일반적인 단위 테스트처럼 브레이크포인트를 설정하고 반복 실행할 수 있어 안정적이고, 실제 빌드 프로세스에 영향을 주지 않습니다.

### Q3: 소스 생성기 코드를 변경했는데 이전 결과가 계속 나오는 경우 어떻게 해결하나요?
**A**: Roslyn의 캐싱 메커니즘 때문에 발생합니다. `bin`/`obj` 폴더를 모두 삭제하고, Visual Studio를 완전히 종료한 뒤 다시 열어 Clean Build를 수행하면 해결됩니다.

---

## 다음 단계

디버깅 환경까지 갖추었으니, 이제 소스 생성기가 활용하는 Roslyn 컴파일러 플랫폼의 아키텍처를 이해할 차례입니다. Syntax Tree, Semantic Model, Symbol이 각각 무엇이고 어떻게 연결되는지 살펴봅니다.

→ [03장. Roslyn 기초](../03-roslyn-fundamentals/)
