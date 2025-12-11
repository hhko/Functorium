# API 문서화 및 코드 샘플

## API 소스

### **API 변경 Diff** - 새로운 내용의 진실 소스:

```text
analysis-output/api-changes-build-current/api-changes-diff.txt
```

**여기서 시작** - 이 파일은 릴리스 간 **새로 추가/변경된 API만** 포함합니다:
- 이번 릴리스에 **추가된 새 API**
- **수정된 API 시그니처**
- **브레이킹 체인지** 및 제거
- 개발자가 알아야 할 **실제 API diff**

### **UBER 파일** - 사용 예시를 위한 완전한 API 참조:

```text
analysis-output/api-changes-build-current/all-api-changes.txt
```

**정확한 코드 샘플 작성에 사용** - 이 파일은 현재 빌드의 **모든 API**를 포함합니다:
- 모든 통합을 위한 메서드 시그니처가 포함된 **완전한 API 파일**
- **모든 기능**: 오류 처리, 로깅, 유틸리티
- 매개변수 이름과 타입을 포함한 **정확한 메서드 시그니처**

## 정확한 문서 작성

### API 변경 문서화 워크플로우

#### 1단계: API 변경 Diff로 새로운 내용 식별

```bash
grep -A 5 -B 2 "ErrorCodeFactory" analysis-output/api-changes-build-current/api-changes-diff.txt
```

#### 2단계: 정확한 사용 예시를 위해 Uber 파일만 사용

```bash
grep -A 10 -B 2 "ErrorCodeFactory" analysis-output/api-changes-build-current/all-api-changes.txt
```

#### 3단계: 코드 샘플을 위한 완전한 API 시그니처 추출

```csharp
// Uber 파일에서 - 정확한 사용 예시를 위한 완전한 API 시그니처:
public static LanguageExt.Common.Error Create(string errorCode, string errorCurrentValue, string errorMessage)
public static LanguageExt.Common.Error CreateFromException(string errorCode, System.Exception exception)
```

#### 4단계: 올바른 API 시그니처로 사용 예시 작성

```csharp
// ✅ 올바름: Uber 파일의 실제 API 시그니처 기반 사용 예시
var error = ErrorCodeFactory.Create("VALIDATION_001", invalidValue, "값이 유효하지 않습니다");
var errorFromEx = ErrorCodeFactory.CreateFromException("SYSTEM_001", exception);
```

## API를 발명하지 마세요

```csharp
// ❌ 잘못됨: 이 메서드들은 API diff나 Uber 파일에서 찾을 수 없음
ErrorCodeFactory.Create("error")
    .WithDetails(details: "추가 정보")      // 발명됨 - Uber 파일에 없음
    .WithInnerError(inner: innerError)     // 발명됨 - Uber 파일에 없음
    .Build();                               // 발명됨 - Uber 파일에 없음
```

## 빌더 컨텍스트 중요

**IServiceCollection vs 직접 사용**: 확장 메서드가 대상으로 하는 인터페이스에 주의하세요:

### **`IServiceCollection`** - 서비스 등록용 확장 메서드

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog();  // ✅ IServiceCollection의 확장 메서드
```

### **직접 사용** - 비즈니스 로직에서 직접 호출

```csharp
// ✅ 비즈니스 로직에서 직접 사용
var error = ErrorCodeFactory.Create("ERR001", value, "오류 메시지");
```

### **❌ 일반적인 실수**: 존재하지 않는 확장 메서드 사용:

```csharp
// ❌ 잘못됨: AddErrorCodeFactory는 존재하지 않음
builder.Services.AddErrorCodeFactory();  // 존재하지 않는 메서드
```

## 문서 링크 가이드라인

### 관련 문서 찾기

- 기능에 대한 기존 문서 검색
- 문서는 여러 방법으로 참조 가능:
  - 프로젝트 내부 문서: 상대 경로 사용
  - 외부 문서 (예: Microsoft Docs): 전체 URL 사용
- 새 API가 명시적으로 언급되면 완전한 네임스페이스로 참조
- 샘플 코드 내에 링크를 넣지 마세요. 위의 설명이나 후속 콘텐츠에만 넣으세요
- 링크 내용이 적합한지 확실하지 않으면 링크를 추가하지 마세요

## 검증 프로세스

API 샘플 작성 전:

1. **API 변경 diff를 먼저 확인**하여 실제로 새로운 것이 무엇인지 식별:

   ```bash
   grep -n "MethodName" analysis-output/api-changes-build-current/api-changes-diff.txt
   ```

2. **Uber 파일에서 완전한 API 세부정보** 가져오기:

   ```bash
   grep -n "MethodName" analysis-output/api-changes-build-current/all-api-changes.txt
   ```

3. **커밋 분석과 교차 참조**하여 변경의 컨텍스트 이해

## API 문서화 규칙

### 핵심 규칙

1. **항상 API 변경 diff로 시작**하여 이번 릴리스의 새로운 내용 식별
2. **Uber 파일은 사용 예시 작성에만 사용** - 올바른 API 시그니처로
3. **매개변수 이름과 타입을 정확히 일치** - Uber 파일에 표시된 대로
4. **diff에서 찾은 브레이킹 체인지에 대한 마이그레이션 단계 표시**
5. **API가 존재한다고 발명하거나 가정하지 않음** - 모든 것이 diff에서 시작되는지 확인

### 정확성 표준

- **DIFF로 시작**: api-changes-diff.txt를 먼저 확인하여 실제로 새로운 것 식별
- **샘플에 UBER 파일 사용**: all-api-changes.txt에서 완전한 API 시그니처 가져오기
- **API 변경에 샘플 제공**: 모든 새 API에 코드 샘플 포함
- **API를 발명하지 않음**: 만들어낸 메서드, 매개변수, 플루언트 체인 금지

## 추적성 참조

문서화된 각 변경에 대한 추적성 참조 저장:

- **커밋 SHA 또는 메시지** - 컴포넌트 분석에서
- **GitHub 이슈 ID** (커밋 메시지에 참조된 경우)
- **GitHub Pull Request 번호** (가능한 경우)
- **컴포넌트 이름** - 변경이 발견된 곳

이를 통해 실제 소스 변경에 대한 검증 및 역추적이 가능합니다. 예시 형식:

```
기능: 오류 디스트럭처링 개선
소스: Functorium의 "Add ManyErrorsDestructurer" 커밋
GitHub PR: #98
GitHub 이슈: #95 (참조된 경우)
```
