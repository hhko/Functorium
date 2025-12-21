# Phase 4: 릴리스 노트 작성

## 목표

분석 결과를 바탕으로 전문적인 릴리스 노트를 작성합니다.

## 파일

| 파일 | 설명 |
|------|------|
| **템플릿** | `.release-notes/TEMPLATE.md` (복사용) |
| **출력** | `.release-notes/RELEASE-$VERSION.md` |

## 작성 절차

1. **템플릿 복사**
   ```bash
   cp .release-notes/TEMPLATE.md .release-notes/RELEASE-$VERSION.md
   ```

2. **placeholder 교체**
   - `{VERSION}` → 실제 버전 (예: v1.0.0)
   - `{DATE}` → 오늘 날짜 (예: 2025-12-19)

3. **섹션 채우기**
   - Phase 3 분석 결과 (`phase3-commit-analysis.md`, `phase3-feature-groups.md`) 참조
   - 각 기능에 대해 코드 샘플과 "Why this matters (왜 중요한가):" 섹션 작성

4. **API 검증**
   - 모든 코드 샘플을 Uber 파일에서 검증

5. **주석 정리**
   - 템플릿 가이드 주석 (`<!-- -->`) 삭제

## 템플릿 구조

템플릿 파일(`.release-notes/TEMPLATE.md`)에는 다음 구조가 포함되어 있습니다:

| 섹션 | 필수 | 설명 |
|------|:----:|------|
| 프론트매터 | ✓ | YAML 헤더 (title, description, date) |
| 개요 | ✓ | 버전 소개, 주요 변경 요약 |
| Breaking Changes | ✓ | API 호환성 깨는 변경 (없으면 명시) |
| 새로운 기능 | ✓ | feat 커밋 기반 |
| 버그 수정 | | fix 커밋 기반 (없으면 생략) |
| API 변경사항 | ✓ | 새로 추가된 주요 API 요약 |
| 설치 | ✓ | 설치 방법 |

## 핵심 문서화 규칙

### 1. 포괄적인 분석

- `.analysis-output/`의 모든 컴포넌트 분석 파일 검토
- 각 파일의 모든 커밋 검토하여 변경 범위 이해
- `api-changes-summary.md`를 사용하여 새 API 추가 및 변경 식별
- 컴포넌트 간 주요 기능 및 테마 요약

### 2. 샘플이 포함된 API 변경

- 모든 API 변경에 코드 샘플 포함
- 코드 샘플 작성 전 Uber 파일 검색
- 표시된 대로 매개변수 이름과 타입 정확히 일치
- diff에서 찾은 브레이킹 체인지에 대한 마이그레이션 단계 표시
- API가 존재한다고 발명하거나 가정하지 않음

### 3. 스타일 및 구조

- 일관된 문서 구조 유지: 프론트매터, 소개, 주요 섹션
- 능동태와 개발자 중심 언어 사용
- 코드 요소는 백틱으로 포맷: `ErrorCodeFactory`, `CreateFromException()`
- API를 처음 참조하거나 새 API인 경우 문서 링크 제공
- 영향별로 구성: 브레이킹 체인지, 주요 기능, 개선사항

### 4. 사용자 가치 명시 (필수)

모든 새로운 기능은 **단순 사실 나열을 넘어 가치를 명확히 전달**해야 합니다.

#### 가치 전달 구조

각 기능은 다음 요소를 **모두** 포함해야 합니다:

1. **기능 설명** (What) - 무엇을 하는가?
2. **코드 샘플** (How) - 어떻게 사용하는가?
3. **사용자 가치** (Why) - 왜 중요한가? 어떤 문제를 해결하는가?
4. **API 참조** (Reference) - 정확한 API 시그니처

#### 가치 설명 방법

**"Why this matters" 섹션 필수:**

모든 주요 기능 다음에 `**Why this matters (왜 중요한가):**` 섹션을 추가하여 다음을 설명합니다:

- **해결하는 문제**: 이 기능이 없으면 개발자가 직면하는 문제
- **개발자 생산성**: 시간 절약, 보일러플레이트 감소, 복잡성 제거
- **코드 품질 향상**: 타입 안전성, 유지보수성, 가독성 개선
- **비즈니스 가치**: 신뢰성, 성능, 관측성 향상

**나쁜 예시 (단순 사실 나열):**

```markdown
### 함수형 오류 처리

ErrorCodeFactory를 통한 구조화된 오류 생성 기능을 제공합니다.

[코드 샘플]
```

**좋은 예시 (가치 중심):**

```markdown
### 함수형 오류 처리

ErrorCodeFactory를 통한 구조화된 오류 생성 기능을 제공합니다.

[코드 샘플]

**Why this matters (왜 중요한가):**
- 예외를 함수형 오류로 변환하여 타입 안전성 향상
- Serilog와 자동 통합으로 구조화된 로깅 지원
- 보일러플레이트 코드 제거 (try-catch 반복 감소)
- 일관된 오류 코드 체계로 디버깅 시간 단축
- 비즈니스 로직과 오류 처리 분리로 코드 가독성 개선
```

#### 가치 설명 체크리스트

각 기능에 대해:

- [ ] 개발자가 직면하는 **구체적인 문제** 명시
- [ ] 이 기능이 그 문제를 **어떻게 해결**하는지 설명
- [ ] **정량적 이점** 포함 (가능한 경우: 시간 절약, 코드 감소)
- [ ] **정성적 이점** 포함 (가독성, 유지보수성, 타입 안전성)
- [ ] **실제 유스케이스** 제시 (언제 이 기능을 사용하는가?)

#### 피해야 할 표현

- ❌ "기능을 제공합니다" (사실만 나열)
- ❌ "지원합니다" (가치 없음)
- ❌ "추가되었습니다" (수동적)
- ❌ "~할 수 있습니다" (모호함)

#### 권장 표현

- ✅ "~하여 [구체적 이점]을 제공합니다"
- ✅ "[문제]를 해결하여 [결과]를 달성합니다"
- ✅ "[복잡한 작업]을 [간단한 방법]으로 간소화합니다"
- ✅ "[시간/코드] 절감으로 생산성을 향상시킵니다"

#### 가치 설명 템플릿

다음 질문에 답하여 가치를 추출하십시오:

1. **문제**: 이 기능이 없으면 개발자는 무엇을 해야 하나? (고통 포인트)
2. **해결**: 이 기능은 그 문제를 어떻게 해결하나?
3. **영향**: 개발자 경험 또는 코드 품질이 어떻게 개선되나?
4. **결과**: 궁극적으로 어떤 비즈니스 가치를 제공하나?

**예시 적용 (OpenTelemetry 통합):**

1. **문제**: 수동으로 OpenTelemetry + Serilog를 설정하려면 복잡한 구성과 여러 단계 필요
2. **해결**: 단일 확장 메서드로 Fluent API 제공
3. **영향**: 설정 시간 10분 → 1분, 구성 오류 가능성 제거
4. **결과**: 빠른 관측성 도입, 프로덕션 문제 신속 대응

## API 문서화 규칙

### API 소스

**UBER 파일** - 완전한 API 참조 (단일 진실 소스):

```text
.analysis-output/api-changes-build-current/all-api-changes.txt
```

**정확한 코드 샘플 작성에 사용** - 이 파일은 현재 빌드의 **모든 API**를 포함합니다:

- 모든 통합을 위한 메서드 시그니처가 포함된 **완전한 API 파일**
- **모든 기능**: 오류 처리, 로깅, 유틸리티
- 매개변수 이름과 타입을 포함한 **정확한 메서드 시그니처**
- **코드 샘플 검증에 중요** - 이 파일에 없으면 문서화하지 않습니다

**개별 API 파일** - 어셈블리별 API 정의:

```text
Src/
├── Functorium/.api/
│   └── Functorium.cs              # 핵심 라이브러리 API
└── Functorium.Testing/.api/
    └── Functorium.Testing.cs      # 테스트 유틸리티 API
```

### 정확한 문서 작성

#### 1단계: Uber 파일에서 API 검색

```bash
grep -A 10 -B 2 "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt
```

#### 2단계: 개별 API 파일에서 상세 확인

```bash
cat Src/Functorium/.api/Functorium.cs | grep -A 5 "ErrorCodeFactory"
```

#### 3단계: 코드 샘플을 위한 완전한 API 시그니처 추출

```csharp
// Uber 파일에서 - 정확한 사용 예시를 위한 완전한 API 시그니처:
public static LanguageExt.Common.Error Create(string errorCode, string errorCurrentValue, string errorMessage)
public static LanguageExt.Common.Error CreateFromException(string errorCode, System.Exception exception)
```

#### 4단계: 올바른 API 시그니처로 사용 예시 작성

```csharp
// 올바름: Uber 파일의 실제 API 시그니처 기반 사용 예시
var error = ErrorCodeFactory.Create("VALIDATION_001", invalidValue, "값이 유효하지 않습니다");
var errorFromEx = ErrorCodeFactory.CreateFromException("SYSTEM_001", exception);
```

### API를 발명하지 마세요

```csharp
// 잘못됨: 이 메서드들은 API diff나 Uber 파일에서 찾을 수 없음
ErrorCodeFactory.Create("error")
    .WithDetails(details: "추가 정보")      // 발명됨 - Uber 파일에 없음
    .WithInnerError(inner: innerError)     // 발명됨 - Uber 파일에 없음
    .Build();                               // 발명됨 - Uber 파일에 없음
```

### 핵심 규칙

1. **Uber 파일에서 API 존재 확인** - `all-api-changes.txt`가 단일 진실 소스
2. **매개변수 이름과 타입을 정확히 일치** - Uber 파일에 표시된 대로
3. **브레이킹 체인지에 대한 마이그레이션 단계 표시**
4. **API가 존재한다고 발명하거나 가정하지 않음** - Uber 파일에 없으면 문서화하지 않음

### 빌더 컨텍스트 중요

**IServiceCollection vs 직접 사용**: 확장 메서드가 대상으로 하는 인터페이스에 주의하세요.

**`IServiceCollection`** - 서비스 등록용 확장 메서드:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog();  // IServiceCollection의 확장 메서드
```

**직접 사용** - 비즈니스 로직에서 직접 호출:

```csharp
// 비즈니스 로직에서 직접 사용
var error = ErrorCodeFactory.Create("ERR001", value, "오류 메시지");
```

**일반적인 실수** - 존재하지 않는 확장 메서드 사용:

```csharp
// 잘못됨: AddErrorCodeFactory는 존재하지 않음
builder.Services.AddErrorCodeFactory();  // 존재하지 않는 메서드
```

## 작성 스타일 가이드라인

### 언어 및 톤

- **능동태 사용**: "이제 오류를 생성할 수 있습니다" - "오류가 생성될 수 있습니다" 대신
- **개발자 중심**: 실용적인 예시로 개발자 대상 작성
- **명확하고 간결하게**: 불필요한 전문 용어나 장황한 설명 피하기
- **행동 지향적**: 개발자가 새 기능으로 무엇을 할 수 있는지에 초점

### 코드 포맷팅

- **적절한 구문 강조 사용**: 항상 언어 지정 (```csharp, ```bash, ```json)
- **완전한 예시 포함**: 가능하면 전체, 작동하는 코드 샘플 표시
- **새 기능 강조**: `// ✨ 새 기능`과 같은 주석으로 변경 강조
- **일관성 유지**: 전체에서 동일한 변수 이름과 패턴 사용

### 섹션 구성

- **가장 영향력 있는 변경부터 시작**: 브레이킹 체인지 먼저, 그 다음 주요 기능
- **관련 기능 그룹화**: 유사한 기능을 통합 섹션으로 결합
- **설명적인 제목 사용**: 스캔하고 특정 개선사항을 찾기 쉽게
- **마이그레이션 가이드 포함**: 브레이킹 체인지에 대해 기존 코드 업데이트 방법 항상 표시

### 선택적 이모지 사용 가이드라인

필요한 경우 다음 이모지 패턴 사용:

- **✨** - 새 기능 및 능력
- **🔧** - 구성 및 설정 개선
- **⚠️** - 브레이킹 체인지 및 중요 공지
- **📊** - 로깅 및 관찰성 기능
- **🛡️** - 오류 처리 및 유효성 검사

## 예시 기능 문서화

### 완전한 예시: 가치 중심 기능 문서화

**필수 구조: 설명 → 코드 샘플 → 장점 → API**

```markdown
### 예외에서 구조화된 오류 생성

LanguageExt Error 타입을 기반으로 예외를 구조화된 오류 코드로 변환하고 Serilog와 통합합니다.

```csharp
using Functorium.Abstractions.Errors;
using LanguageExt.Common;

try
{
    await HttpClient.GetAsync(url);
}
catch (HttpRequestException ex)
{
    // 예외에서 구조화된 오류 생성
    var error = ErrorCodeFactory.CreateFromException("HTTP_001", ex);

    // Serilog에 구조화된 형태로 자동 로깅
    Log.Error("API 요청 실패: {@Error}", error);

    return Fin<Response>.Fail(error);
}
```

**Why this matters (왜 중요한가):**
- 예외를 함수형 오류로 변환하여 타입 안전성 향상
- Serilog와 자동 통합으로 구조화된 로깅 지원
- try-catch 보일러플레이트 코드 제거
- 일관된 오류 코드 체계로 디버깅 시간 단축
- 비즈니스 로직과 오류 처리 명확히 분리

**API:**
```csharp
namespace Functorium.Abstractions.Errors
{
    public static class ErrorCodeFactory
    {
        public static Error CreateFromException(string errorCode, Exception exception);
        public static Error Create(string errorCode, string errorCurrentValue, string errorMessage);
        public static Error Create<T>(string errorCode, T errorCurrentValue, string errorMessage) where T : notnull;
    }
}
```
```

### 예시: OpenTelemetry 통합

```markdown
### OpenTelemetry 통합

OpenTelemetry 및 Serilog를 단일 확장 메서드로 통합하여 분산 추적, 메트릭, 로깅을 지원합니다.

```csharp
using Functorium.Abstractions.Registrations;

var builder = WebApplication.CreateBuilder(args);

// 단일 확장 메서드로 관측성 설정
builder.Services
    .RegisterObservability(builder.Configuration)
    .ConfigureTraces(tracing => tracing
        .AddSource("MyApp")
        .Configure(t => t.AddHttpClientInstrumentation()))
    .ConfigureMetrics(metrics => metrics
        .AddMeter("MyApp.Metrics"))
    .ConfigureSerilog(logging => logging
        .AddEnricher<ThreadIdEnricher>())
    .Build();

var app = builder.Build();
app.Run();
```

**Why this matters (왜 중요한가):**
- 복잡한 OpenTelemetry 설정을 5줄로 간소화 (기존 50+ 줄)
- Fluent API로 타입 안전한 구성 제공 (컴파일 타임 검증)
- 기본 모범 사례 자동 적용 (샘플링, 리소스 속성)
- OTLP Collector 통합으로 벤더 중립적 아키텍처 지원
- FluentValidation 기반 구성 검증으로 런타임 오류 방지

**구성 (appsettings.json):**
```json
{
  "OpenTelemetry": {
    "ServiceName": "MyService",
    "CollectorEndpoint": "http://localhost:4317",
    "SamplingRate": 1.0,
    "EnablePrometheusExporter": true
  }
}
```

**API:**
```csharp
namespace Functorium.Abstractions.Registrations
{
    public static class OpenTelemetryRegistration
    {
        public static OpenTelemetryBuilder RegisterObservability(
            this IServiceCollection services,
            IConfiguration configuration);
    }
}

namespace Functorium.Adapters.Observabilities.Builders
{
    public class OpenTelemetryBuilder
    {
        public OpenTelemetryBuilder ConfigureTraces(Action<TracingConfigurator> configure);
        public OpenTelemetryBuilder ConfigureMetrics(Action<MetricsConfigurator> configure);
        public OpenTelemetryBuilder ConfigureSerilog(Action<LoggingConfigurator> configure);
        public IServiceCollection Build();
    }
}
```
```

### 예시: 테스트 유틸리티 기능

```markdown
### 통합 테스트 픽스처

ASP.NET Core 및 Quartz 스케줄러를 위한 xUnit 테스트 픽스처를 제공합니다.

```csharp
using Functorium.Testing.Arrangements.Hosting;

public class ApiTests : IClassFixture<HostTestFixture<Program>>
{
    private readonly HostTestFixture<Program> _fixture;

    public ApiTests(HostTestFixture<Program> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetUsers_ReturnsOk()
    {
        // HttpClient 즉시 사용 가능
        var response = await _fixture.Client.GetAsync("/api/users");
        response.EnsureSuccessStatusCode();

        // DI 컨테이너 직접 접근
        var userService = _fixture.Services.GetRequiredService<IUserService>();
        var users = await userService.GetAllAsync();

        Assert.NotEmpty(users);
    }
}
```

**Why this matters (왜 중요한가):**
- WebApplicationFactory 보일러플레이트 제거 (50+ 줄 → 5줄)
- HttpClient 및 DI 컨테이너 즉시 사용 가능
- 환경 및 구성 커스터마이징 간단
- 테스트 간 격리 자동 보장
- Quartz Job 실행 및 완료 추적 기능 내장

**API:**
```csharp
namespace Functorium.Testing.Arrangements.Hosting
{
    public class HostTestFixture<TProgram> : IAsyncLifetime, IAsyncDisposable
        where TProgram : class
    {
        public HttpClient Client { get; }
        public IServiceProvider Services { get; }
        protected virtual string EnvironmentName { get; }
        protected virtual void ConfigureHost(IWebHostBuilder builder);
    }
}
```
```

### 완전한 예시: 브레이킹 체인지

```markdown
### 오류 핸들러 인터페이스 이름 변경

오류 처리 API가 더 나은 일관성을 위해 통합되었습니다.

**이전 (Functorium 1.0)**:
```csharp
// 이전 명명
public class MyHandler : IErrorHandler
{
    public void Handle(Error error) { }
}
```

**이후 (Functorium 1.1)**:
```csharp
// 새로운 일관된 명명
public class MyDestructurer : IErrorDestructurer
{
    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory) { }
}
```

**마이그레이션 가이드**:
1. 모든 `IErrorHandler` 참조를 `IErrorDestructurer`로 업데이트
2. `Handle` 메서드를 `Destructure` 메서드로 교체
3. 반환 타입을 `LogEventPropertyValue`로 변경

이 변경으로 더 직관적이고 일관된 오류 처리 API가 제공됩니다.
```

## 중간 결과 저장

Phase 4의 작성 결과를 `.analysis-output/work/` 폴더에 저장합니다.

### 저장할 파일

```
.release-notes/scripts/.analysis-output/work/
├── phase4-draft.md              # 릴리스 노트 초안
├── phase4-api-references.md     # 사용된 API 목록
└── phase4-code-samples.md       # 모든 코드 샘플
```

### phase4-api-references.md 형식

```markdown
# Phase 4: 사용된 API 참조

## Functorium 어셈블리

### ErrorCodeFactory
- Location: Functorium.Abstractions.Errors.ErrorCodeFactory
- Methods:
  - Create(string, string, string)
  - Create<T>(string, T, string)
  - CreateFromException(string, Exception)
- Uber File: Line 75-89
- Status: ✓ 검증됨

### OpenTelemetryRegistration
- Location: Functorium.Abstractions.Registrations.OpenTelemetryRegistration
- Methods:
  - RegisterObservability(IServiceCollection, IConfiguration)
- Uber File: Line 93-96
- Status: ✓ 검증됨
```

## 콘솔 출력 형식

### 작성 완료

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 4: 릴리스 노트 작성 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

작성 통계:
  ✓ 전체 길이: 15,380 줄
  ✓ 섹션 수: 8개
  ✓ 코드 샘플: 24개
  ✓ API 참조: 30개 타입

주요 섹션:
  1. 개요 (버전: v1.0.0-alpha.1)
  2. Breaking Changes (0개)
  3. 새로운 기능 (8개)
  4. 버그 수정 (1개)
  5. API 변경사항 (요약)
  6. 문서화 (38개 문서)
  7. 알려진 제한사항
  8. 감사의 말

출력 파일:
  ✓ .release-notes/RELEASE-v1.0.0-alpha.1.md

중간 결과 저장:
  ✓ .release-notes/scripts/.analysis-output/work/phase4-draft.md
  ✓ .release-notes/scripts/.analysis-output/work/phase4-api-references.md
  ✓ .release-notes/scripts/.analysis-output/work/phase4-code-samples.md
```

## 품질 체크리스트

문서 완료 전:

### 정확성 검증

- [ ] **모든 코드 샘플** Uber 파일에서 검증됨
- [ ] **API 시그니처** 매개변수 이름 및 타입 정확히 일치
- [ ] **발명된 API나 명령 없음**
- [ ] **브레이킹 체인지에 대한 완전한 마이그레이션 가이드**

### 가치 전달 검증 (필수)

- [ ] **모든 주요 기능에 "Why this matters" 섹션** 포함됨
- [ ] **구체적인 문제 해결** 명시 (이 기능이 없으면 무엇이 어려운가?)
- [ ] **개발자 생산성 이점** 설명 (시간 절약, 코드 감소)
- [ ] **코드 품질 향상** 설명 (타입 안전성, 가독성, 유지보수성)
- [ ] **정량적 이점** 포함 (가능한 경우: 50줄 → 5줄, 10분 → 1분)
- [ ] **실제 유스케이스** 제시 (언제 이 기능을 사용하는가?)
- [ ] 단순 사실 나열 표현 제거 ("제공합니다", "지원합니다")

### 구조 및 포맷팅

- [ ] **일관된 포맷팅** - 기존 템플릿과 일치
- [ ] **명확하고 개발자 중심 언어** 전체에서
- [ ] **적절한 문서 링크** (상대 경로 또는 전체 URL)
- [ ] **영향 및 기능별 논리적 구성**
- [ ] **필수 구조 준수** (설명 → 코드 샘플 → 장점 → API)

### 코드 샘플 품질

- [ ] **완전하고 실행 가능한 코드** (컴파일 가능)
- [ ] **적절한 using 문** 포함
- [ ] **주석으로 핵심 포인트** 강조
- [ ] **실제 시나리오 반영** (Hello World 수준 아님)

## 성공 기준

- [ ] 릴리스 노트 파일 생성됨 (`.release-notes/RELEASE-$VERSION.md`)
- [ ] 모든 코드 샘플 Uber 파일에서 검증됨
- [ ] 모든 주요 기능에 가치 섹션 포함됨
- [ ] 중간 결과 저장됨

## 다음 단계

릴리스 노트 작성 완료 후 [Phase 5: 검증](phase5-validation.md)으로 진행합니다.
