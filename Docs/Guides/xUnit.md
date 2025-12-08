# xUnit v3 가이드

이 문서는 xUnit.net v3 테스트 프레임워크의 주요 기능과 사용법을 설명합니다.

## 목차
- [요약](#요약)
- [시작하기](#시작하기)
- [테스트 유형](#테스트-유형)
- [Theory 데이터 소스](#theory-데이터-소스)
- [v3 새로운 기능](#v3-새로운-기능)
- [설정 파일](#설정-파일)
- [출력 및 로깅](#출력-및-로깅)
- [테스트 실행](#테스트-실행)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 요약

### 주요 명령

```bash
# 템플릿 설치
dotnet new install xunit.v3.templates

# 테스트 프로젝트 생성
dotnet new xunit3

# 테스트 실행
dotnet test

# 필터링 실행
dotnet test --filter "FullyQualifiedName~MyTest"

# 테스트 목록 조회 (실행 없이)
dotnet test -- -list
```

### 주요 절차

**1. 프로젝트 설정:**
```bash
# 1. 템플릿 설치
dotnet new install xunit.v3.templates

# 2. 프로젝트 생성
dotnet new xunit3 -n MyProject.Tests

# 3. 테스트 작성
# 4. 실행 및 검증
dotnet test
```

**2. 테스트 작성:**
```csharp
// Fact: 단일 테스트
[Fact]
public void Method_Returns_Expected() { }

// Theory: 파라미터화된 테스트
[Theory]
[InlineData(1, 2, 3)]
public void Add_ReturnsSum(int a, int b, int expected) { }
```

### 주요 개념

**1. 테스트 유형**

| Attribute | 용도 |
|-----------|------|
| `[Fact]` | 단일 테스트 케이스 |
| `[Theory]` | 파라미터화된 테스트 |

**2. Theory 데이터 소스**

| Attribute | 용도 |
|-----------|------|
| `[InlineData]` | 상수 값 전달 |
| `[MemberData]` | 메서드/속성 참조 |
| `[ClassData]` | 데이터 클래스 참조 |

**3. v3 주요 변경사항**

| 기능 | 설명 |
|------|------|
| 동적 스킵 | `Assert.Skip()` |
| 명시적 테스트 | `[Fact(Explicit = true)]` |
| TestContext | 테스트 상태 정보 |
| MatrixTheoryData | 조합 데이터 생성 |

<br/>

## 시작하기

### 요구사항

- .NET 8.0 이상
- .NET Framework 4.7.2 이상 (레거시)

### 프로젝트 템플릿

```bash
# 템플릿 설치
dotnet new install xunit.v3.templates

# 프로젝트 생성
dotnet new xunit3 -n MyProject.Tests

# 언어 지정 (기본: C#)
dotnet new xunit3 -n MyProject.Tests --language F#
```

### 패키지 설치 (기존 프로젝트)

```bash
# 필수 패키지
dotnet add package xunit.v3
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.runner.visualstudio
```

### 프로젝트 파일 (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

<br/>

## 테스트 유형

### Fact (단일 테스트)

조건이 항상 참이어야 하는 테스트입니다.

```csharp
[Fact]
public void Add_TwoNumbers_ReturnsSum()
{
    // Arrange
    var calculator = new Calculator();

    // Act
    var result = calculator.Add(2, 3);

    // Assert
    Assert.Equal(5, result);
}
```

### Fact 옵션

```csharp
// 표시명 지정
[Fact(DisplayName = "2 + 3 = 5")]
public void Add_TwoNumbers_ReturnsSum() { }

// 스킵
[Fact(Skip = "버그 수정 대기중")]
public void BrokenTest() { }

// 타임아웃 (밀리초)
[Fact(Timeout = 5000)]
public async Task LongRunningTest() { }

// 명시적 테스트 (v3 신규)
[Fact(Explicit = true)]
public void ManualTest() { }
```

### Theory (파라미터화된 테스트)

여러 입력 값으로 동일한 로직을 테스트합니다.

```csharp
[Theory]
[InlineData(1, 2, 3)]
[InlineData(0, 0, 0)]
[InlineData(-1, 1, 0)]
public void Add_VariousInputs_ReturnsExpected(int a, int b, int expected)
{
    var calculator = new Calculator();
    var result = calculator.Add(a, b);
    Assert.Equal(expected, result);
}
```

<br/>

## Theory 데이터 소스

### InlineData

상수 값을 직접 전달합니다. 기본 타입만 지원합니다.

```csharp
[Theory]
[InlineData(1, 2, 3)]
[InlineData(10, 20, 30)]
[InlineData(-5, 5, 0)]
public void Add_ReturnsSum(int a, int b, int expected)
{
    Assert.Equal(expected, new Calculator().Add(a, b));
}
```

**제한사항:**
- 상수 표현식만 가능 (int, string, bool 등)
- DateTime, 복합 객체 불가

### MemberData

정적 메서드나 속성에서 데이터를 가져옵니다.

```csharp
public class CalculatorTests
{
    // 정적 속성
    public static IEnumerable<object[]> AddTestData =>
    [
        [1, 2, 3],
        [10, 20, 30],
        [-5, 5, 0]
    ];

    [Theory]
    [MemberData(nameof(AddTestData))]
    public void Add_WithMemberData_ReturnsExpected(int a, int b, int expected)
    {
        Assert.Equal(expected, new Calculator().Add(a, b));
    }

    // 정적 메서드
    public static IEnumerable<object[]> GetTestData()
    {
        yield return [1, 2, 3];
        yield return [10, 20, 30];
    }

    [Theory]
    [MemberData(nameof(GetTestData))]
    public void Add_WithMethodData_ReturnsExpected(int a, int b, int expected)
    {
        Assert.Equal(expected, new Calculator().Add(a, b));
    }
}
```

### ClassData

별도 클래스에서 데이터를 제공합니다.

```csharp
// 데이터 클래스
public class AddTestData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return [1, 2, 3];
        yield return [10, 20, 30];
        yield return [-5, 5, 0];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// 테스트
[Theory]
[ClassData(typeof(AddTestData))]
public void Add_WithClassData_ReturnsExpected(int a, int b, int expected)
{
    Assert.Equal(expected, new Calculator().Add(a, b));
}
```

### TheoryData (강타입)

타입 안전성을 제공하는 강타입 데이터 소스입니다.

```csharp
// TheoryData 사용
public static TheoryData<int, int, int> AddTestData => new()
{
    { 1, 2, 3 },
    { 10, 20, 30 },
    { -5, 5, 0 }
};

[Theory]
[MemberData(nameof(AddTestData))]
public void Add_WithTheoryData_ReturnsExpected(int a, int b, int expected)
{
    Assert.Equal(expected, new Calculator().Add(a, b));
}
```

### TheoryDataRow (v3 메타데이터)

v3에서는 개별 행에 메타데이터를 지정할 수 있습니다.

```csharp
public static IEnumerable<ITheoryDataRow> GetTestData()
{
    yield return new TheoryDataRow<int, int, int>(1, 2, 3);
    yield return new TheoryDataRow<int, int, int>(10, 20, 30)
    {
        Skip = "아직 구현되지 않음"
    };
    yield return new TheoryDataRow<int, int, int>(-5, 5, 0)
    {
        Timeout = 1000,
        DisplayName = "음수 테스트"
    };
}

[Theory]
[MemberData(nameof(GetTestData))]
public void Add_WithMetadata_ReturnsExpected(int a, int b, int expected)
{
    Assert.Equal(expected, new Calculator().Add(a, b));
}
```

### MatrixTheoryData (v3 조합)

여러 데이터 세트의 조합을 자동 생성합니다.

```csharp
public static MatrixTheoryData<int, string> MatrixData => new(
    [1, 2, 3],           // 첫 번째 파라미터 값들
    ["A", "B", "C"]      // 두 번째 파라미터 값들
);
// 결과: (1,A), (1,B), (1,C), (2,A), (2,B), (2,C), (3,A), (3,B), (3,C)

[Theory]
[MemberData(nameof(MatrixData))]
public void Matrix_AllCombinations(int number, string letter)
{
    Assert.True(number > 0);
    Assert.NotNull(letter);
}
```

<br/>

## v3 새로운 기능

### 동적 테스트 스킵

실행 시간에 조건부로 테스트를 건너뜁니다.

```csharp
[Fact]
public void Test_SkipOnCondition()
{
    // 조건부 스킵
    Assert.SkipWhen(
        !OperatingSystem.IsWindows(),
        "Windows에서만 실행");

    // 또는
    Assert.SkipUnless(
        OperatingSystem.IsWindows(),
        "Windows에서만 실행");

    // 무조건 스킵
    Assert.Skip("아직 구현되지 않음");

    // 실제 테스트 코드
    Assert.True(true);
}
```

### Attribute 기반 동적 스킵

```csharp
public class TestConditions
{
    public static bool IsWindows => OperatingSystem.IsWindows();
    public static bool HasDatabase => CheckDatabaseConnection();
}

[Fact(SkipWhen = nameof(TestConditions.IsWindows))]
public void Test_SkipOnWindows() { }

[Fact(SkipUnless = nameof(TestConditions.HasDatabase))]
public void Test_RequiresDatabase() { }
```

### 명시적 테스트

사용자가 명시적으로 요청할 때만 실행됩니다.

```csharp
[Fact(Explicit = true)]
public void ManualIntegrationTest()
{
    // 명시적 요청 시에만 실행
}

[Theory(Explicit = true)]
[InlineData(1)]
[InlineData(2)]
public void ManualTheory(int value) { }
```

### TestContext

테스트 실행 중 컨텍스트 정보에 접근합니다.

```csharp
[Fact]
public async Task Test_WithContext()
{
    var context = TestContext.Current;

    // 취소 토큰
    var cancellationToken = context.CancellationToken;

    // 테스트 정보
    var testName = context.TestDisplayName;

    // 진단 메시지 출력
    context.SendDiagnosticMessage("테스트 시작");

    // 첨부 파일 추가
    context.AddAttachment("screenshot.png", "스크린샷");

    await Task.Delay(100, cancellationToken);
}
```

### ITestContextAccessor (의존성 주입)

```csharp
public class MyTests
{
    private readonly ITestContextAccessor _contextAccessor;

    public MyTests(ITestContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    [Fact]
    public void Test_WithInjectedContext()
    {
        var context = _contextAccessor.Current;
        context.SendDiagnosticMessage("의존성 주입된 컨텍스트");
    }
}
```

### 어셈블리 Fixture

어셈블리 수준에서 공유 리소스를 관리합니다.

```csharp
// Fixture 정의
public class DatabaseFixture : IAsyncLifetime
{
    public string ConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        ConnectionString = await SetupDatabase();
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupDatabase();
    }
}

// 어셈블리에 등록
[assembly: AssemblyFixture(typeof(DatabaseFixture))]

// 테스트에서 사용
public class DatabaseTests
{
    private readonly DatabaseFixture _fixture;

    public DatabaseTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Test_WithDatabaseFixture()
    {
        Assert.NotEmpty(_fixture.ConnectionString);
    }
}
```

<br/>

## 설정 파일

### xunit.runner.json

프로젝트 루트에 `xunit.runner.json` 파일을 생성합니다.

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4,
  "diagnosticMessages": true,
  "longRunningTestSeconds": 60,
  "methodDisplay": "classAndMethod",
  "methodDisplayOptions": "replaceUnderscoreWithSpace"
}
```

### 주요 설정 옵션

| 옵션 | 기본값 | 설명 |
|------|--------|------|
| `parallelizeAssembly` | false | 어셈블리 간 병렬 실행 |
| `parallelizeTestCollections` | true | 컬렉션 간 병렬 실행 |
| `maxParallelThreads` | CPU 수 | 최대 병렬 스레드 수 |
| `diagnosticMessages` | false | 진단 메시지 출력 |
| `longRunningTestSeconds` | 0 | 장시간 테스트 감지 (0=비활성) |
| `methodDisplay` | classAndMethod | 테스트 표시명 형식 |
| `methodDisplayOptions` | none | 표시명 변환 옵션 |
| `preEnumerateTheories` | true | Theory 데이터 사전 열거 |
| `failSkips` | false | 스킵된 테스트를 실패로 처리 |
| `failWarns` | false | 경고 테스트를 실패로 처리 |

### methodDisplayOptions 값

| 값 | 설명 |
|----|------|
| `none` | 변환 없음 |
| `replaceUnderscoreWithSpace` | `_`를 공백으로 변환 |
| `useOperatorMonikers` | 연산자명을 기호로 변환 |
| `all` | 모든 변환 적용 |

### 프로젝트에 설정 파일 포함

```xml
<ItemGroup>
  <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

<br/>

## 출력 및 로깅

### ITestOutputHelper

테스트 출력을 캡처합니다.

```csharp
public class OutputTests
{
    private readonly ITestOutputHelper _output;

    public OutputTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Test_WithOutput()
    {
        _output.WriteLine("테스트 시작");
        _output.WriteLine($"현재 시간: {DateTime.Now}");

        // v3에서 추가된 Write 메서드
        _output.Write("줄바꿈 없이 출력");
    }
}
```

### 콘솔/트레이스 출력 캡처 (v3)

어셈블리 레벨에서 Console, Debug, Trace 출력을 캡처합니다.

```csharp
// AssemblyInfo.cs 또는 테스트 파일에 추가
[assembly: CaptureConsole]
[assembly: CaptureTrace]

public class ConsoleTests
{
    [Fact]
    public void Test_CapturesConsole()
    {
        // Console 출력이 테스트 출력으로 리다이렉트됨
        Console.WriteLine("이 메시지가 캡처됩니다");
        System.Diagnostics.Trace.WriteLine("Trace도 캡처됩니다");
    }
}
```

<br/>

## 테스트 실행

### 기본 실행

```bash
# 전체 테스트 실행
dotnet test

# 특정 프로젝트
dotnet test Tests/MyProject.Tests

# 빌드 없이 실행
dotnet test --no-build

# Release 구성
dotnet test -c Release
```

### 필터링

```bash
# 정규화된 이름으로 필터
dotnet test --filter "FullyQualifiedName~Calculator"

# 테스트 이름으로 필터
dotnet test --filter "Name~Add"

# 클래스로 필터
dotnet test --filter "ClassName~CalculatorTests"

# 카테고리(Trait)로 필터
dotnet test --filter "Category=Unit"

# 조합 (AND)
dotnet test --filter "ClassName~Calculator&Name~Add"

# 조합 (OR)
dotnet test --filter "ClassName~Calculator|ClassName~Parser"
```

### v3 쿼리 필터

```bash
# 복잡한 필터링 (v3)
dotnet test -- -filter "assembly=MyTests AND class~Calculator"
```

### 테스트 목록 조회

```bash
# 실행 없이 테스트 목록만 조회 (v3)
dotnet test -- -list

# JSON 형식 출력
dotnet test -- -list json
```

### 보고서 생성

```bash
# TRX 보고서 (Visual Studio 형식)
dotnet test -- -trx

# CTRF 보고서 (Common Test Report Format)
dotnet test -- -ctrf
```

<br/>

## 트러블슈팅

### 테스트가 발견되지 않을 때

**원인**: 패키지 참조 누락 또는 버전 불일치

**해결:**
```bash
# 필수 패키지 확인
dotnet list package

# 패키지 재설치
dotnet add package xunit.v3
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.runner.visualstudio
```

### Theory 테스트가 단일 테스트로 표시될 때

**원인**: `preEnumerateTheories` 설정이 false

**해결:**
```json
// xunit.runner.json
{
  "preEnumerateTheories": true
}
```

### 병렬 실행으로 인한 테스트 실패

**원인**: 테스트 간 공유 리소스 충돌

**해결:**
```csharp
// 동일 컬렉션으로 순차 실행
[Collection("Database")]
public class DatabaseTests1 { }

[Collection("Database")]
public class DatabaseTests2 { }

// 또는 병렬화 비활성화
// xunit.runner.json
{
  "parallelizeTestCollections": false
}
```

### 비동기 테스트 타임아웃

**원인**: 테스트가 지정된 시간 내에 완료되지 않음

**해결:**
```csharp
// 타임아웃 증가
[Fact(Timeout = 30000)]  // 30초
public async Task LongRunningTest()
{
    // TestContext의 CancellationToken 사용 권장
    var token = TestContext.Current.CancellationToken;
    await Task.Delay(1000, token);
}
```

### 설정 파일이 적용되지 않을 때

**원인**: 파일이 출력 디렉토리에 복사되지 않음

**해결:**
```xml
<ItemGroup>
  <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

<br/>

## FAQ

### Q1. v2에서 v3로 마이그레이션하려면 어떻게 하나요?

**A:** 주요 변경사항:
1. 패키지 변경: `xunit` → `xunit.v3`
2. 네임스페이스 변경 없음 (대부분 호환)
3. 새 기능 활용: `Assert.Skip()`, `TestContext` 등

```xml
<!-- v2 -->
<PackageReference Include="xunit" Version="2.9.3" />

<!-- v3 -->
<PackageReference Include="xunit.v3" Version="3.2.1" />
```

### Q2. Fact와 Theory 중 어느 것을 사용해야 하나요?

**A:**
| 상황 | 선택 |
|------|------|
| 단일 입력/조건 | `[Fact]` |
| 여러 입력 테스트 | `[Theory]` |
| 경계값 테스트 | `[Theory]` |
| 동등 클래스 테스트 | `[Theory]` |

### Q3. InlineData로 복합 객체를 전달할 수 있나요?

**A:** 아니요, `InlineData`는 상수 값만 지원합니다. 복합 객체는 `MemberData` 또는 `ClassData`를 사용하세요.

```csharp
// 복합 객체 - MemberData 사용
public static IEnumerable<object[]> PersonData =>
[
    [new Person("Alice", 30)],
    [new Person("Bob", 25)]
];

[Theory]
[MemberData(nameof(PersonData))]
public void Test_WithPerson(Person person) { }
```

### Q4. 테스트 실행 순서를 제어할 수 있나요?

**A:** 기본적으로 테스트는 독립적이어야 합니다. 순서가 필요하면 `ITestCaseOrderer`를 구현하세요:

```csharp
[TestCaseOrderer("MyOrderer", "MyAssembly")]
public class OrderedTests
{
    [Fact]
    public void Test1() { }

    [Fact]
    public void Test2() { }
}
```

### Q5. 테스트 간 데이터를 공유하려면 어떻게 하나요?

**A:** Fixture를 사용하세요:

```csharp
// Collection Fixture (컬렉션 내 공유)
public class SharedFixture { public int Value = 42; }

[CollectionDefinition("Shared")]
public class SharedCollection : ICollectionFixture<SharedFixture> { }

[Collection("Shared")]
public class Tests1
{
    private readonly SharedFixture _fixture;
    public Tests1(SharedFixture fixture) => _fixture = fixture;
}
```

### Q6. 조건부로 테스트를 건너뛰려면 어떻게 하나요?

**A:** v3에서는 `Assert.Skip`을 사용하세요:

```csharp
[Fact]
public void ConditionalTest()
{
    Assert.SkipUnless(
        Environment.GetEnvironmentVariable("CI") == "true",
        "CI 환경에서만 실행");

    // 테스트 코드
}
```

### Q7. 테스트 출력이 보이지 않을 때는?

**A:** `ITestOutputHelper`를 사용하세요:

```csharp
public class MyTests(ITestOutputHelper output)
{
    [Fact]
    public void Test()
    {
        output.WriteLine("이 메시지가 테스트 결과에 표시됩니다");
    }
}
```

`Console.WriteLine`을 캡처하려면 `[assembly: CaptureConsole]`을 추가하세요.

<br/>

## 참고 문서

- [xUnit.net 공식 문서](https://xunit.net/docs/getting-started/v3/whats-new)
- [xUnit v3 시작하기](https://xunit.net/docs/getting-started/v3/getting-started)
- [xunit.runner.json 설정](https://xunit.net/docs/config-xunit-runner-json)
- [GitHub: xunit/xunit](https://github.com/xunit/xunit)
