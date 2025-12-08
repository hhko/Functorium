# 단위 테스트 가이드

이 문서는 Functorium 프로젝트의 단위 테스트 작성 규칙과 패턴을 설명합니다.

## 목차
- [요약](#요약)
- [테스트 패키지](#테스트-패키지)
- [테스트 명명 규칙](#테스트-명명-규칙)
- [변수 명명 규칙](#변수-명명-규칙)
- [AAA 패턴](#aaa-패턴)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 요약

### 주요 명령

```bash
# 전체 테스트 실행
dotnet test

# 특정 프로젝트 테스트
dotnet test Tests/Functorium.Tests.Unit

# 코드 커버리지 포함 실행
dotnet test --collect:"XPlat Code Coverage"

# 특정 테스트만 실행
dotnet test --filter "FullyQualifiedName~Handle_ReturnsSuccess"
```

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
| Shouldly | Assertion 라이브러리 |
| NSubstitute | Mocking 라이브러리 |
| ArchUnitNET | 아키텍처 테스트 |

<br/>

## 테스트 패키지

| 패키지 | 버전 | 용도 |
|--------|------|------|
| xunit.v3 | 3.2.1 | 테스트 프레임워크 |
| Shouldly | 4.3.0 | Fluent Assertion |
| Verify.Xunit | 31.8.0 | 스냅샷 테스트 |
| NSubstitute | 5.3.0 | Mocking |
| TngTech.ArchUnitNET.xUnitV3 | 0.13.0 | 아키텍처 테스트 |

### 패키지 설치

```bash
# xUnit v3 (테스트 프레임워크)
dotnet add package xunit.v3

# Shouldly (Assertion)
dotnet add package Shouldly

# NSubstitute (Mocking)
dotnet add package NSubstitute
```

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

**A:** XPlat Code Coverage를 사용하세요:

```bash
# 커버리지 수집
dotnet test --collect:"XPlat Code Coverage"

# 리포트 생성 (ReportGenerator 필요)
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report"
```

또는 `Build-Local.ps1` 스크립트를 실행하면 자동으로 커버리지 리포트가 생성됩니다.

## 참고 문서

- [xUnit.net Documentation](https://xunit.net/docs/getting-started/v3/whats-new)
- [Shouldly Documentation](https://docs.shouldly.org/)
- [NSubstitute Documentation](https://nsubstitute.github.io/help/getting-started/)
