# 단위 테스트 가이드

이 문서는 Functorium 프로젝트의 단위 테스트 작성 규칙과 패턴을 설명합니다.

## 목차
- [요약](#요약)
- [MTP 설정](#mtp-설정)
- [테스트 패키지](#테스트-패키지)
- [테스트 프로젝트 설정](#테스트-프로젝트-설정)
- [테스트 명명 규칙](#테스트-명명-규칙)
- [변수 명명 규칙](#변수-명명-규칙)
- [AAA 패턴](#aaa-패턴)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 요약

### 주요 명령 (MTP 모드)

```bash
# 전체 테스트 실행
dotnet test

# 특정 프로젝트 테스트
dotnet test --project Tests/Functorium.Tests.Unit

# 코드 커버리지 포함 실행 (MTP 방식)
dotnet test -- --coverage --coverage-output-format cobertura

# 특정 테스트만 실행 (MTP 필터)
dotnet test -- --filter-method "Handle_ReturnsSuccess"

# 클래스 필터링
dotnet test -- --filter-class "MyNamespace.MyTestClass"
```

> **참고**: MTP 모드에서는 테스트 옵션을 `--` 구분자 뒤에 전달합니다.

### 주요 절차

**1. 테스트 작성:**
```bash
# 1. 테스트 클래스 생성 (Tests/{프로젝트}.Tests.Unit/{기능}/)
# 2. 테스트 메서드 작성 (T1_T2_T3 명명 규칙)
# 3. AAA 패턴 적용 (Arrange-Act-Assert)
# 4. 테스트 실행 및 검증
```

**2. 테스트 실행:**
```bash
# 1. 빌드
dotnet build

# 2. 테스트 실행
dotnet test

# 3. 결과 확인
```

### 주요 개념

**1. 테스트 명명 규칙 (T1_T2_T3)**

| 구성요소 | 설명 | 예시 |
|---------|------|-----|
| **T1** | 테스트 대상 메서드명 | `Validate`, `Handle` |
| **T2** | 예상 결과 | `ReturnsSuccess`, `ReturnsFail` |
| **T3** | 테스트 시나리오 | `WhenTitleIsEmpty` |

**2. AAA 패턴**

| 단계 | 변수명 | 설명 |
|------|--------|------|
| Arrange | `sut`, `request` | 테스트 준비 |
| Act | `actual` | 실행 |
| Assert | - | 검증 |

**3. 테스트 패키지**

| 패키지 | 용도 |
|--------|------|
| xunit.v3 | 테스트 프레임워크 |
| Microsoft.Testing.Extensions.CodeCoverage | 코드 커버리지 |
| Microsoft.Testing.Extensions.TrxReport | TRX 리포트 |
| Shouldly | Assertion 라이브러리 |
| NSubstitute | Mocking 라이브러리 |
| TngTech.ArchUnitNET.xUnitV3 | 아키텍처 테스트 |

<br/>

## MTP 설정

### Microsoft Testing Platform이란?

MTP(Microsoft Testing Platform)는 VSTest를 대체하는 새로운 테스트 엔진입니다. xUnit v3는 MTP를 기본 지원합니다.

### MTP 모드 활성화

MTP를 사용하려면 **프로젝트 설정**과 **SDK 버전별 설정**이 모두 필요합니다.

#### 1. 프로젝트 필수 설정 (모든 .NET 버전 공통)

모든 테스트 프로젝트의 `.csproj` 파일에 다음 설정이 **필수**입니다:

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
</PropertyGroup>
```

| 속성 | 설명 |
|------|------|
| `OutputType` | MTP는 standalone executable 방식으로 동작하므로 `Exe` **필수** |
| `UseMicrosoftTestingPlatformRunner` | xUnit v3에서 MTP 러너 활성화 (xUnit 전용) |

> **참고**: `OutputType`을 `Exe`로 설정해야 하는 이유는 MSBuild/NuGet 복원 시 버그를 방지하기 위한 [Microsoft 공식 권장사항](https://devblogs.microsoft.com/dotnet/mtp-adoption-frameworks/)입니다.

> **팁**: `Directory.Build.props` 파일에 공통 설정을 추가하면 모든 테스트 프로젝트에 자동 적용됩니다:
> ```xml
> <Project>
>   <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
>     <OutputType>Exe</OutputType>
>     <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
>   </PropertyGroup>
> </Project>
> ```

#### 2. SDK 버전별 `dotnet test` 설정

**.NET 10 SDK 이상**: `global.json`에서 설정

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

> **위치**: `global.json`은 솔루션 루트에 위치합니다.
> ```
> 솔루션 루트/
> ├── global.json          ← MTP 설정 위치
> ├── Directory.Packages.props
> └── Functorium.slnx
> ```

> **참고**: .NET 10 SDK 이상에서는 `--` 구분자 없이 MTP 옵션을 직접 사용할 수 있습니다.

**.NET 8-9 SDK**: 프로젝트 파일에서 추가 설정 필요

```xml
<PropertyGroup>
  <TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>
</PropertyGroup>
```

> **주의**: .NET 8-9에서는 `dotnet test` 명령 시 `--` 구분자가 필요합니다.

### xUnit v3 MTP 패키지 선택

xUnit v3는 MTP 버전을 선택할 수 있습니다:

| 패키지 | 설명 |
|--------|------|
| `xunit.v3` | 기본값 (MTP v1 포함) |
| `xunit.v3.mtp-v1` | MTP v1 명시적 지정 |
| `xunit.v3.mtp-v2` | MTP v2 사용 |
| `xunit.v3.mtp-off` | MTP 비활성화 (VSTest만 사용) |

### MTP CLI 옵션 (xUnit v3)

| 기능 | xUnit 네이티브 | MTP 명령줄 |
|------|---------------|-----------|
| 클래스 필터링 | `-class "name"` | `--filter-class "name"` |
| 메서드 필터링 | `-method "name"` | `--filter-method "name"` |
| 네임스페이스 필터링 | `-namespace "name"` | `--filter-namespace "name"` |
| 트레이트 필터링 | `-trait "name=value"` | `--filter-trait "name=value"` |
| 병렬 처리 | `-parallel <option>` | `--parallel <option>` |
| HTML 리포트 | `-html <file>` | `--report-xunit-html --report-xunit-html-filename <file>` ¹ |
| JUnit 리포트 | `-junit <file>` | `--report-junit --report-junit-filename <file>` ¹ |
| 실시간 출력 | `-showLiveOutput` | `--show-live-output on` |

> ¹ HTML/JUnit 리포트는 별도 패키지 설치가 필요합니다: `xunit.v3.reports.html`, `xunit.v3.reports.junit`

### VSTest vs MTP 필터 비교

| 모드 | 필터 옵션 | 예시 |
|------|----------|------|
| VSTest | `--filter` | `dotnet test --filter "FullyQualifiedName~MyTest"` |
| MTP | `-- --filter-method` | `dotnet test -- --filter-method "MyTest"` |

> **참고**: VSTest 모드에서는 `--filter` 옵션을 `--` 구분자 없이 사용합니다.

### 코드 커버리지 옵션 (MTP)

`Microsoft.Testing.Extensions.CodeCoverage` 패키지 설치 후 사용 가능:

| 옵션 | 설명 |
|------|------|
| `--coverage` | 코드 커버리지 활성화 (필수) |
| `--coverage-output <file>` | 출력 파일명 지정 |
| `--coverage-output-format <format>` | 형식 (coverage, xml, cobertura) |
| `--coverage-settings <file>` | XML 설정 파일 경로 |

**사용 예:**

```bash
# dotnet test로 커버리지 수집
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.xml

# dotnet run으로 직접 실행
dotnet run --project Tests -- --coverage --coverage-output-format cobertura
```

### TRX 리포트 옵션 (MTP)

`Microsoft.Testing.Extensions.TrxReport` 패키지 설치 후 사용 가능:

| 옵션 | 설명 |
|------|------|
| `--report-trx` | TRX 리포트 생성 |
| `--report-trx-filename <file>` | 출력 파일명 지정 |

**사용 예:**

```bash
# TRX 리포트 생성
dotnet test -- --report-trx

# 파일명 지정
dotnet test -- --report-trx --report-trx-filename results.trx

# 커버리지와 TRX 리포트 함께 생성 (Build-Local.ps1 방식)
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.xml --report-trx
```

<br/>

## 테스트 패키지

| 패키지 | 용도 | 비고 |
|--------|------|------|
| xunit.v3 | 테스트 프레임워크 | xUnit v3 (MTP 기반) |
| xunit.runner.visualstudio | VS/IDE 테스트 탐색기 지원 | 필수 |
| Microsoft.NET.Test.Sdk | .NET 테스트 SDK | 필수 |
| Microsoft.Testing.Extensions.CodeCoverage | 코드 커버리지 수집 | MTP 확장 |
| Microsoft.Testing.Extensions.TrxReport | TRX 리포트 생성 | MTP 확장 |
| Shouldly | Fluent Assertion | 권장 |
| Verify.XunitV3 | 스냅샷 테스트 | xUnit v3용 |
| NSubstitute | Mocking | 권장 |
| TngTech.ArchUnitNET.xUnitV3 | 아키텍처 테스트 | xUnit v3용 |

### 패키지 설치

```bash
# xUnit v3 (테스트 프레임워크) - 필수 패키지
dotnet add package xunit.v3
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.NET.Test.Sdk

# MTP 확장 (코드 커버리지, TRX 리포트)
dotnet add package Microsoft.Testing.Extensions.CodeCoverage
dotnet add package Microsoft.Testing.Extensions.TrxReport

# Shouldly (Assertion)
dotnet add package Shouldly

# NSubstitute (Mocking)
dotnet add package NSubstitute
```

> **주의**: `Microsoft.Testing.Extensions.TrxReport` 패키지가 없으면 `Build-Local.ps1` 실행 시 `--report-trx` 옵션으로 인해 테스트가 실행되지 않습니다.

<br/>

## 테스트 프로젝트 설정

### 기본 csproj 구성

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
    <!-- MTP 필수 설정 -->
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>

  <ItemGroup>
    <!-- 필수 패키지 -->
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <PackageReference Include="xunit.runner.visualstudio" />

    <!-- MTP 확장 (커버리지, TRX 리포트) -->
    <PackageReference Include="Microsoft.Testing.Extensions.CodeCoverage" />
    <PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />

    <!-- Assertion 라이브러리 -->
    <PackageReference Include="Shouldly" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MyProject\MyProject.csproj" />
  </ItemGroup>

</Project>
```

> **중요**: `OutputType`과 `UseMicrosoftTestingPlatformRunner`는 MTP 동작을 위한 **필수 설정**입니다.

### xunit.runner.json 설정

테스트 프로젝트 루트에 `xunit.runner.json` 파일을 생성합니다:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": false,
  "parallelizeTestCollections": true
}
```

### xUnit v3 네임스페이스 변경사항

xUnit v2에서 v3로 마이그레이션 시 다음 네임스페이스 변경이 필요합니다:

| v2 | v3 |
|----|-----|
| `Xunit.Abstractions` | `Xunit` |
| `ITestOutputHelper` (Xunit.Abstractions) | `ITestOutputHelper` (Xunit) |

```csharp
// xUnit v2
using Xunit.Abstractions;

// xUnit v3
using Xunit;
```

### 비테스트 라이브러리에서 xUnit 타입 사용

테스트 유틸리티 라이브러리(예: `Functorium.Testing`)에서 `ITestOutputHelper` 등 xUnit 타입을 사용해야 하는 경우:

```xml
<!-- xunit.v3 대신 xunit.v3.extensibility.core 사용 -->
<PackageReference Include="xunit.v3.extensibility.core" />
```

> **주의**: `xunit.v3` 패키지는 테스트 프로젝트(`<IsTestProject>true</IsTestProject>`)에서만 사용해야 합니다. 비테스트 라이브러리에서 사용하면 "test projects must be executable" 오류가 발생합니다.

<br/>

## 테스트 명명 규칙

테스트 메서드 이름은 **T1_T2_T3** 형식으로 작성합니다.

### 형식

```
{T1}_{T2}_{T3}
```

| 구성요소 | 설명 | 예시 |
|---------|------|-----|
| **T1** | 테스트 대상 메서드명 | `Validate`, `Handle`, `Execute` |
| **T2** | 예상 결과 | `ReturnsSuccess`, `ReturnsFail`, `ThrowsException` |
| **T3** | 테스트 시나리오/조건 | `WhenTitleIsEmpty`, `WhenInputIsValid` |

### Validator 테스트 명명 예시

```csharp
// 유효성 검사 오류 반환
Validate_ReturnsValidationError_WhenTitleIsEmpty
Validate_ReturnsValidationError_WhenTitleExceedsMaxLength
Validate_ReturnsValidationError_WhenTemperatureCIsBelowMinimum
Validate_ReturnsValidationError_WhenTemperatureCIsAboveMaximum

// 유효성 검사 통과
Validate_ReturnsNoError_WhenRequestIsValid
Validate_ReturnsNoError_WhenTemperatureCIsWithinRange
Validate_ReturnsNoError_WhenTitleIsAtMaxLength
```

### Usecase 테스트 명명 예시

```csharp
// 성공 시나리오
Handle_ReturnsSuccess_WhenTemperatureCIsPositive
Handle_ReturnsSuccess_WhenTemperatureCIsZero
Handle_ReturnsSuccess_WhenRequestIsValid

// 실패 시나리오
Handle_ReturnsFail_WhenTemperatureCIsNegative
Handle_ReturnsFail_WhenEntityNotFound

// 반환값 검증
Handle_ReturnsTemperatureCBasedOnTitleLength_WhenSuccessful
Handle_ReturnsTemperatureCEqualToTitleLength_WhenSuccessful
```

### T2 (예상 결과) 표준 용어

| 용어 | 사용 시기 |
|------|----------|
| `ReturnsSuccess` | 성공 결과 반환 |
| `ReturnsFail` | 실패 결과 반환 |
| `ReturnsValidationError` | 유효성 검사 오류 |
| `ReturnsNoError` | 오류 없음 |
| `ThrowsException` | 예외 발생 |
| `Returns{값}` | 특정 값 반환 |

### T3 (시나리오) 표준 접두사

| 접두사 | 사용 시기 | 예시 |
|--------|----------|------|
| `When` | 조건/상황 | `WhenInputIsNull` |
| `Given` | 사전 조건 | `GivenUserIsAuthenticated` |
| `With` | 특정 값 | `WithValidInput` |

<br/>

## 변수 명명 규칙

### 표준 변수명

| 변수명 | 용도 | AAA 단계 |
|--------|------|----------|
| `sut` | System Under Test (테스트 대상) | Arrange |
| `request` | 요청 객체 | Arrange |
| `actual` | 실행 결과 | Act |
| `expected` | 예상 결과 (비교용) | Assert |

### 사용 예시

```csharp
[Fact]
public async Task Handle_ReturnsSuccess_WhenRequestIsValid()
{
    // Arrange
    var sut = new MyUsecase();
    var request = new MyRequest(Title: "Valid");

    // Act
    var actual = await sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

<br/>

## AAA 패턴

모든 테스트는 **Arrange-Act-Assert** 패턴을 따릅니다.

### 구조

```csharp
[Fact]
public async Task T1_T2_T3()
{
    // Arrange - 테스트 준비
    var sut = new TestTarget();
    var request = new Request(...);

    // Act - 실행
    var actual = await sut.Method(request);

    // Assert - 검증
    actual.ShouldBe(expected);
}
```

### 완전한 예시

```csharp
[Fact]
public async Task Handle_ReturnsSuccess_WhenTemperatureCIsPositive()
{
    // Arrange
    var sut = new UpdateWeatherForecastCommand.Usecase();
    var request = new UpdateWeatherForecastCommand.Request(
        Title: "Valid Title",
        Description: "Valid description",
        TemperatureC: 25);

    // Act
    var actual = await sut.Handle(request, CancellationToken.None);

    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

### Shouldly Assertion 예시

```csharp
// 값 비교
actual.ShouldBe(expected);
actual.ShouldNotBe(unexpected);

// Boolean
actual.IsSucc.ShouldBeTrue();
actual.IsFail.ShouldBeFalse();

// Null 체크
actual.ShouldBeNull();
actual.ShouldNotBeNull();

// 컬렉션
list.ShouldBeEmpty();
list.ShouldContain(item);
list.Count.ShouldBe(3);

// 예외
Should.Throw<ArgumentException>(() => sut.Method());
```

<br/>

## 트러블슈팅

### 테스트가 발견되지 않을 때

**원인**: xUnit 패키지 버전 불일치 또는 Test SDK 누락

**해결:**
```bash
# 패키지 확인
dotnet list package

# 필수 패키지 설치
dotnet add package xunit.v3
dotnet add package Microsoft.NET.Test.Sdk
dotnet add package xunit.runner.visualstudio
```

### Build-Local.ps1에서 일부 테스트가 실행되지 않을 때

**원인**: `Microsoft.Testing.Extensions.TrxReport` 패키지 누락

**증상:**
- `dotnet test`로 직접 실행하면 모든 테스트가 통과
- `Build-Local.ps1` 실행 시 "오류: N" 메시지와 함께 일부 테스트만 실행됨

**해결:**
```bash
# TrxReport 패키지 추가
dotnet add package Microsoft.Testing.Extensions.TrxReport
```

또는 csproj 파일에 직접 추가:
```xml
<PackageReference Include="Microsoft.Testing.Extensions.TrxReport" />
```

### "test projects must be executable" 오류

**원인**: 비테스트 라이브러리에서 `xunit.v3` 패키지 사용

**해결:** `xunit.v3` 대신 `xunit.v3.extensibility.core` 사용:
```xml
<!-- 잘못된 설정 (비테스트 라이브러리) -->
<PackageReference Include="xunit.v3" />

<!-- 올바른 설정 (비테스트 라이브러리) -->
<PackageReference Include="xunit.v3.extensibility.core" />
```

### ITestOutputHelper 네임스페이스 오류 (xUnit v3)

**원인**: xUnit v2에서 v3로 마이그레이션 시 네임스페이스 변경

**해결:**
```csharp
// 변경 전 (v2)
using Xunit.Abstractions;

// 변경 후 (v3)
using Xunit;
```

### 비동기 테스트가 실패할 때

**원인**: `async void` 사용 또는 `await` 누락

**해결:**
```csharp
// 잘못된 예시
[Fact]
public async void Handle_ReturnsSuccess_WhenValid()  // async void 사용
{
    var actual = sut.Handle(request);  // await 누락
}

// 올바른 예시
[Fact]
public async Task Handle_ReturnsSuccess_WhenValid()  // async Task 사용
{
    var actual = await sut.Handle(request);  // await 사용
}
```

### Shouldly Assertion 메시지가 불명확할 때

**원인**: 기본 Assert 사용

**해결:**
```csharp
// 불명확한 메시지
Assert.True(actual.IsSucc);  // "Expected: True, Actual: False"

// 명확한 메시지 (Shouldly)
actual.IsSucc.ShouldBeTrue();  // "actual.IsSucc should be True but was False"
```

### Mock 객체가 예상대로 동작하지 않을 때

**원인**: NSubstitute 설정 누락

**해결:**
```csharp
// Mock 설정
var repository = Substitute.For<IRepository>();
repository.GetById(Arg.Any<int>()).Returns(expectedEntity);

// 호출 검증
repository.Received(1).GetById(42);
```

<br/>

## FAQ

### Q1. 테스트 메서드 이름이 너무 길어지면 어떻게 하나요?

**A:** T1_T2_T3 형식을 유지하되, 각 부분을 간결하게 작성하세요:

```csharp
// 너무 긴 이름
Handle_ReturnsValidationErrorWithDetailedMessage_WhenUserInputTemperatureCelsiusValueIsNegativeNumber

// 적절한 이름
Handle_ReturnsValidationError_WhenTemperatureCIsNegative
```

### Q2. 여러 조건을 테스트해야 할 때는 어떻게 하나요?

**A:** `[Theory]`와 `[InlineData]`를 사용하세요:

```csharp
[Theory]
[InlineData(-10)]
[InlineData(-1)]
[InlineData(int.MinValue)]
public void Validate_ReturnsFail_WhenTemperatureCIsNegative(int temperature)
{
    var request = new Request(TemperatureC: temperature);
    var actual = sut.Validate(request);
    actual.IsFail.ShouldBeTrue();
}
```

### Q3. 테스트 클래스는 어떻게 구성하나요?

**A:** 테스트 대상 클래스당 하나의 테스트 클래스를 만드세요:

```
Tests/Functorium.Tests.Unit/
├── Features/
│   └── WeatherForecast/
│       ├── UpdateWeatherForecastCommandTests.cs
│       └── GetWeatherForecastQueryTests.cs
└── Common/
    └── ValidationTests.cs
```

### Q4. private 메서드는 어떻게 테스트하나요?

**A:** private 메서드는 직접 테스트하지 않습니다. public 메서드를 통해 간접적으로 테스트하세요. 만약 private 메서드를 직접 테스트해야 한다면, 설계를 재검토하세요.

### Q5. 외부 의존성(DB, API)이 있는 코드는 어떻게 테스트하나요?

**A:** NSubstitute로 Mock 객체를 만들어 사용하세요:

```csharp
[Fact]
public async Task Handle_ReturnsSuccess_WhenEntityExists()
{
    // Arrange
    var repository = Substitute.For<IRepository>();
    repository.GetByIdAsync(42).Returns(new Entity { Id = 42 });

    var sut = new MyUsecase(repository);

    // Act
    var actual = await sut.Handle(new Request(Id: 42));

    // Assert
    actual.IsSucc.ShouldBeTrue();
}
```

### Q6. 테스트 실행 순서에 의존하는 테스트는 어떻게 처리하나요?

**A:** 테스트는 독립적이어야 합니다. 실행 순서에 의존하지 않도록 각 테스트에서 필요한 상태를 직접 설정하세요:

```csharp
// 나쁜 예시 - 다른 테스트에 의존
[Fact]
public void Test2_DependsOnTest1()
{
    // Test1에서 설정한 상태에 의존
}

// 좋은 예시 - 독립적
[Fact]
public void Test2_IsIndependent()
{
    // Arrange - 필요한 모든 상태를 직접 설정
    var sut = CreateSut();
    SetupRequiredState();

    // Act & Assert
}
```

### Q7. 코드 커버리지는 어떻게 확인하나요?

**A:** MTP 코드 커버리지 확장을 사용하세요:

```bash
# MTP 방식 커버리지 수집 (권장)
dotnet test -- --coverage --coverage-output-format cobertura --coverage-output coverage.xml

# 리포트 생성 (ReportGenerator 필요)
reportgenerator -reports:"**/coverage.xml" -targetdir:"coverage-report"
```

또는 `Build-Local.ps1` 스크립트를 실행하면 자동으로 커버리지 리포트가 생성됩니다.

> **참고**: VSTest 방식(`--collect:"XPlat Code Coverage"`)도 여전히 작동하지만, MTP 방식이 권장됩니다.

## 참고 문서

### xUnit v3
- [xUnit.net v3 What's New](https://xunit.net/docs/getting-started/v3/whats-new)
- [xUnit.net v3 Migration Guide](https://xunit.net/docs/getting-started/v3/migration)
- [xUnit.net v3 Microsoft Testing Platform](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
- [xUnit.net v3 Code Coverage with MTP](https://xunit.net/docs/getting-started/v3/code-coverage-with-mtp)

### Microsoft Testing Platform
- [Microsoft Testing Platform Overview](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro)
- [Testing with dotnet test](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test)
- [dotnet test Command Reference](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-test)

### 기타 라이브러리
- [Shouldly Documentation](https://docs.shouldly.org/)
- [NSubstitute Documentation](https://nsubstitute.github.io/help/getting-started/)
- [ArchUnitNET Documentation](https://archunitnet.readthedocs.io/)
