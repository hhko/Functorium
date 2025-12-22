# Source Generator 디버깅 가이드

이 문서는 `Functorium.Adapters.SourceGenerator` 프로젝트를 Visual Studio에서 디버깅하는 방법을 설명합니다.

## 목차

- [개요](#개요)
- [방법 1: Debugger.Launch() 사용](#방법-1-debuggerlaunch-사용)
- [방법 2: 테스트 프로젝트에서 디버깅 (권장 ⭐)](#방법-2-테스트-프로젝트에서-디버깅-권장-)
- [방법 3: Attach to Process](#방법-3-attach-to-process)
- [디버깅 시 유용한 팁](#디버깅-시-유용한-팁)
- [문제 해결](#문제-해결)

---

## 개요

C# Source Generator는 컴파일 타임에 실행되므로, 일반적인 애플리케이션 디버깅과 다른 접근이 필요합니다. 이 프로젝트는 여러 디버깅 방법을 지원하도록 설계되었습니다.

**진입점**:
```
Functorium.Adapters.SourceGenerator\Generators\IncrementalGeneratorBase.cs
- public void Initialize(IncrementalGeneratorInitializationContext context)
```

---

## 방법 1: Debugger.Launch() 사용

컴파일 시작 시 자동으로 디버거 연결 대화상자를 표시하는 방법입니다.

### 1단계: AttachDebugger 파라미터 활성화

`Functorium.Adapters.SourceGenerator\AdapterPipelineGenerator.cs` 파일 수정:

```csharp
[Generator(LanguageNames.CSharp)]
public sealed class AdapterPipelineGenerator()
    : IncrementalGeneratorBase<PipelineClassInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: true)  // 🔧 디버깅 활성화
```

### 2단계: Visual Studio 재시작

소스 생성기 프로젝트를 수정한 후에는 **Visual Studio를 완전히 재시작**해야 합니다.

### 3단계: 브레이크포인트 설정

디버깅하려는 위치에 브레이크포인트를 설정합니다:

```
Functorium.Adapters.SourceGenerator\Generators\IncrementalGeneratorBase.cs
- Line 32: IncrementalValuesProvider<TValue> provider = ...

Functorium.Adapters.SourceGenerator\AdapterPipelineGenerator.cs
- Line 66: private static PipelineClassInfo MapToPipelineClassInfo(...)
- Line 130: private static void Generate(...)
```

### 4단계: 소스 생성기를 사용하는 프로젝트 빌드

```bash
# Visual Studio에서:
# - F5 (디버그 시작)
# - Ctrl+Shift+B (솔루션 빌드)

# 또는 명령줄에서:
dotnet build Observability.Adapters.Infrastructure
```

### 5단계: 디버거 선택

빌드가 시작되면 "Just-In-Time Debugger" 대화상자가 나타납니다:

1. **현재 실행 중인 Visual Studio 인스턴스 선택**
   - 예: `devenv.exe (PID: 12345) - C:\Program Files\Microsoft Visual Studio\...`

2. **"Set the currently selected debugger as the default" 체크**

3. **OK 클릭**

### 6단계: 디버깅 시작

디버거가 연결되면 설정한 브레이크포인트에서 실행이 멈춥니다. 이제 단계별 실행이 가능합니다:

- **F10**: Step Over (다음 줄로 이동)
- **F11**: Step Into (메서드 내부로 진입)
- **F5**: Continue (다음 브레이크포인트까지 실행)

### 7단계: 디버깅 종료 후 AttachDebugger 비활성화

디버깅이 끝나면 **반드시** `AttachDebugger: false`로 되돌립니다:

```csharp
[Generator(LanguageNames.CSharp)]
public sealed class AdapterPipelineGenerator()
    : IncrementalGeneratorBase<PipelineClassInfo>(
        RegisterSourceProvider,
        Generate,
        AttachDebugger: false)  // ⚠️ 디버깅 비활성화
```

⚠️ **중요**: `AttachDebugger: true` 상태로 커밋하지 마세요! 모든 빌드에서 디버거 대화상자가 나타납니다.

---

## 방법 2: 테스트 프로젝트에서 디버깅 (권장 ⭐)

이미 있는 단위 테스트 프로젝트를 활용하는 방법으로, **가장 안정적이고 반복 가능한 디버깅 방법**입니다.

### 1단계: 테스트 프로젝트 열기

```
Functorium.Tests.Unit\AdaptersTests\SourceGenerators\AdapterPipelineGeneratorTests.cs
```

### 2단계: 테스트 코드에 브레이크포인트 설정

```csharp
[Fact]
public Task AdapterPipelineGenerator_ShouldGeneratePipeline_WithTupleTypes()
{
    // Arrange
    string input = """
    using System;
    ...
    """;

    // Act
    string? actual = _sut.Generate(input);  // ⬅️ 브레이크포인트

    // Assert
    return Verify(actual);
}
```

### 3단계: 소스 생성기 코드에 브레이크포인트 설정

```csharp
// Functorium.Adapters.SourceGenerator\AdapterPipelineGenerator.cs
private static PipelineClassInfo MapToPipelineClassInfo(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken)
{
    // 클래스가 없을 때
    if (context.TargetSymbol is not INamedTypeSymbol classSymbol)  // ⬅️ 브레이크포인트
    {
        return PipelineClassInfo.None;
    }

    // 클래스 이름과 네임스페이스
    string className = classSymbol.Name;  // ⬅️ 브레이크포인트
    ...
}
```

### 4단계: 디버깅 시작

#### Visual Studio Test Explorer 사용:
1. **View** → **Test Explorer** (Ctrl+E, T)
2. 테스트 우클릭 → **Debug**

#### 코드에서 직접 실행:
1. 테스트 메서드 위에 마우스를 올리면 나타나는 아이콘 클릭
2. **Debug Test** 선택

### 5단계: 소스 생성기 내부로 Step Into

테스트 브레이크포인트에서 멈추면:
1. **F11** (Step Into)을 반복하여 `_sut.Generate(input)` 내부로 진입
2. 소스 생성기 코드의 브레이크포인트에 도달
3. 원하는 만큼 디버깅

### 장점

- ✅ 안정적: 컴파일러 프로세스 타이밍 문제 없음
- ✅ 반복 가능: 같은 입력으로 여러 번 테스트 가능
- ✅ 격리된 환경: 다른 프로젝트 영향 없음
- ✅ 빠른 피드백: 전체 빌드 필요 없음

---

## 방법 3: Attach to Process

수동으로 컴파일러 프로세스에 연결하는 방법입니다.

### 1단계: 빌드 시작

```bash
# 명령줄에서 빌드 시작 (종료하지 않음)
dotnet build Observability.Adapters.Infrastructure --no-incremental
```

### 2단계: Visual Studio에서 프로세스에 연결

1. **Debug** → **Attach to Process** (Ctrl+Alt+P)
2. **Search box**: `csc` 또는 `VBCSCompiler` 입력
3. 프로세스 선택:
   - `csc.exe` (C# 컴파일러)
   - `VBCSCompiler.exe` (빌드 서버)
4. **Attach to**: "Managed (CoreCLR)" 선택
5. **Attach** 클릭

### 단점

⚠️ **주의**: 컴파일러 프로세스는 매우 빠르게 종료되므로 타이밍을 맞추기 어렵습니다.

**권장하지 않습니다.** 방법 1 또는 방법 3을 사용하세요.

---

## 디버깅 시 유용한 팁

### 1. 생성된 코드 확인

Visual Studio에서 생성된 코드를 직접 볼 수 있습니다:

```
Solution Explorer
→ Dependencies
→ Analyzers
→ Functorium.Adapters.SourceGenerator
→ Functorium.Adapters.SourceGenerator.AdapterPipelineGenerator
   → GeneratePipelineAttribute.g.cs
   → Repositories.RepositoryIoPipeline.g.cs
   → ...
```

### 2. 소스 생성 로그 보기

상세한 빌드 로그에서 소스 생성 과정을 확인할 수 있습니다:

```bash
# 진단 수준 로그 생성
dotnet build Observability.Adapters.Infrastructure -v:diag > build.log

# build.log에서 "SourceGenerator" 검색
```

### 3. Roslyn 심볼 검색

생성기 내에서 심볼 정보를 확인하는 코드:

```csharp
private static PipelineClassInfo MapToPipelineClassInfo(
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken)
{
    if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
        return PipelineClassInfo.None;

    // 디버깅: 클래스 정보 확인
    string className = classSymbol.Name;  // ⬅️ 브레이크포인트: "RepositoryIo"
    string @namespace = classSymbol.ContainingNamespace.ToString();  // ⬅️ "Observability.Adapters.Infrastructure.Repositories"

    // 디버깅: 모든 인터페이스 확인
    var interfaces = classSymbol.AllInterfaces;  // ⬅️ Watch 창에서 확인

    // 디버깅: IAdapter를 구현하는 인터페이스의 메서드 확인
    var methods = classSymbol.AllInterfaces
        .Where(ImplementsIAdapter)
        .SelectMany(i => i.GetMembers().OfType<IMethodSymbol>())  // ⬅️ Watch 창에서 확인
        .ToList();

    ...
}
```

### 4. Watch 창 활용

디버깅 중 Watch 창에서 유용한 표현식:

```csharp
// 현재 심볼의 전체 이름
classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)

// 모든 인터페이스 목록
classSymbol.AllInterfaces.Select(i => i.Name).ToArray()

// 메서드 시그니처
method.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)

// 파라미터 타입
method.Parameters.Select(p => p.Type.ToDisplayString()).ToArray()
```

### 5. 조건부 브레이크포인트

특정 조건에서만 멈추도록 설정:

1. 브레이크포인트 우클릭 → **Conditions**
2. 조건 입력:
   ```csharp
   className == "RepositoryIo"
   method.Name == "Delay"
   ```

---

## 문제 해결

### 문제 1: 디버거가 연결되지 않음

**증상**: `Debugger.Launch()` 실행 후 대화상자가 나타나지 않음

**해결 방법**:
1. Visual Studio를 **관리자 권한**으로 실행
2. Windows 설정에서 Just-In-Time 디버깅이 활성화되었는지 확인:
   - **Control Panel** → **System** → **Advanced system settings**
   - **Environment Variables**에서 확인

### 문제 2: 브레이크포인트가 빨간 점이 아닌 속이 빈 원으로 표시됨

**증상**: "The breakpoint will not currently be hit. No symbols have been loaded for this document."

**해결 방법**:
1. Visual Studio 재시작
2. 빌드 캐시 삭제:
   ```bash
   # PowerShell
   Remove-Item -Recurse -Force .\bin, .\obj
   dotnet build
   ```
3. **Tools** → **Options** → **Debugging** → **Symbols**에서 Microsoft Symbol Servers 활성화

### 문제 3: 코드 변경이 반영되지 않음

**증상**: 소스 생성기 코드를 수정했지만 생성된 코드가 변경되지 않음

**해결 방법**:
1. **Visual Studio 완전히 종료** (중요!)
2. 모든 빌드 아티팩트 삭제:
   ```bash
   # PowerShell
   Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force
   ```
3. Visual Studio 재시작 후 Clean → Rebuild:
   ```bash
   dotnet clean
   dotnet build
   ```

### 문제 4: "The project is out of date" 경고가 계속 나타남

**증상**: 빌드할 때마다 프로젝트가 out-of-date로 표시됨

**해결 방법**:
1. `.csproj` 파일에서 다음 속성 추가:
   ```xml
   <PropertyGroup>
     <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
     <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
   </PropertyGroup>

   <ItemGroup>
     <Compile Remove="Generated/**/*.cs" />
   </ItemGroup>
   ```

2. 또는 생성된 파일을 `.gitignore`에 추가:
   ```
   **/Generated/**
   ```

### 문제 5: 테스트에서 디버깅이 작동하지 않음

**증상**: 테스트 실행 시 소스 생성기 코드 브레이크포인트에서 멈추지 않음

**해결 방법**:
1. 테스트 프로젝트 `.csproj`에 다음 참조 추가 확인:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\Functorium.Adapters.SourceGenerator\Functorium.Adapters.SourceGenerator.csproj"
                       OutputItemType="Analyzer"
                       ReferenceOutputAssembly="true" />
   </ItemGroup>
   ```

2. `ReferenceOutputAssembly="true"` 설정 확인 (디버깅에 필요)

---

## 권장 워크플로우

### 일반 개발 및 디버깅:
1. **테스트 프로젝트 사용** (방법 2) ⭐ 권장
2. 특정 입력에 대한 테스트 케이스 작성
3. 반복적으로 디버깅

### 긴급 디버깅:
1. **Debugger.Launch() 사용** (방법 1)
2. `AttachDebugger: true` 설정 후 빌드
3. 디버깅 완료 후 즉시 `AttachDebugger: false`로 되돌리기

### 빠른 확인:
1. **생성된 코드 직접 확인** (팁 1)
2. Solution Explorer → Analyzers에서 확인

---

## 참고 자료

- [Source Generators Cookbook (Microsoft)](https://github.com/dotnet/roslyn/blob/main/docs/features/source-generators.cookbook.md)
- [Source Generator Debugging (Microsoft Docs)](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview#debugging)
- [Incremental Generators (Roslyn Wiki)](https://github.com/dotnet/roslyn/blob/main/docs/features/incremental-generators.md)

---

## 버전 정보

- **.NET SDK**: 9.0
- **C# Language Version**: 12.0
- **Roslyn Version**: 4.x (with Incremental Generators API)
- **Target Functorium**: netstandard2.0 (Source Generator 프로젝트)

---

**Last Updated**: 2025-01-06
