# 구현 계획: [기능명]

**상태**: 🔄 진행 중
**시작일**: YYYY-MM-DD
**최종 업데이트**: YYYY-MM-DD
**예상 완료일**: YYYY-MM-DD

---

**⚠️ 중요 지침**: 각 단계 완료 후:
1. ✅ 완료된 작업 체크박스 선택
2. 🧪 모든 품질 게이트 검증 명령 실행
3. ⚠️ 모든 품질 게이트 항목 통과 확인
4. 📅 위의 "최종 업데이트" 날짜 업데이트
5. 📝 노트 섹션에 학습 내용 기록
6. ➡️ 그런 다음에만 다음 단계로 진행

⛔ **품질 게이트를 건너뛰거나 실패한 검사를 무시하고 진행하지 마세요**

---

## 📋 개요

### 기능 설명
[이 기능이 무엇을 하고 왜 필요한지]

### 성공 기준
- [ ] 기준 1
- [ ] 기준 2
- [ ] 기준 3

### 사용자 영향
[이것이 사용자에게 어떤 이점을 주거나 제품을 어떻게 개선하는지]

---

## 🏗️ 아키텍처 결정

| 결정 | 근거 | 트레이드오프 |
|------|------|-------------|
| [결정 1] | [이 접근 방식을 선택한 이유] | [포기하는 것] |
| [결정 2] | [이 접근 방식을 선택한 이유] | [포기하는 것] |

---

## 📦 의존성

### 시작 전 필요 사항
- [ ] 의존성 1: [설명]
- [ ] 의존성 2: [설명]

### 외부 의존성
- 패키지/라이브러리 1: 버전 X.Y.Z
- 패키지/라이브러리 2: 버전 X.Y.Z

## 🧪 테스트 전략

### 테스트 접근 방식
**TDD 원칙**: 먼저 테스트를 작성하고, 그 다음 테스트를 통과시키기 위해 구현

### 이 기능의 테스트 피라미드
| 테스트 유형 | 커버리지 목표 | 목적 |
|------------|--------------|------|
| **단위 테스트** | ≥80% | 비즈니스 로직, 모델, 핵심 알고리즘 |
| **통합 테스트** | 핵심 경로 | 컴포넌트 상호작용, 데이터 흐름 |
| **E2E 테스트** | 주요 사용자 흐름 | 전체 시스템 동작 검증 |

### 테스트 파일 구조
```
{솔루션}/
├── Services/
│   └── {서비스명}/
│       ├── Src/                                          # 소스 코드
│       │   ├── {서비스명}/                               # 메인 프로젝트 (Entry Point)
│       │   ├── {서비스명}.Domain/                        # 도메인 레이어
│       │   ├── {서비스명}.Application/                   # 애플리케이션 레이어
│       │   ├── {서비스명}.Adapters.Infrastructure/       # 인프라 어댑터
│       │   ├── {서비스명}.Adapters.Persistence/          # 영속성 어댑터
│       │   └── {서비스명}.Adapters.Presentation/         # 프레젠테이션 어댑터
│       │
│       └── Tests/                                        # 테스트 코드
│           ├── {서비스명}.Tests.Unit/                    # 단위 테스트
│           │   └── LayerTests/                           # 레이어별 테스트
│           │       ├── Domain/                           # 💼 비즈니스: 도메인 규칙
│           │       ├── Application/                      # 💼 비즈니스: 유스케이스
│           │       └── Adapters/                         # 🔧 기술: 어댑터
│           │           ├── Infrastructure/
│           │           ├── Persistence/
│           │           └── Presentation/
│           │
│           └── {서비스명}.Tests.Integration/             # 통합 테스트
│               └── [feature_name]/
│
└── {솔루션명}.Tests.E2E/                                 # E2E 테스트 (솔루션 레벨)
    └── [user_flows]/
```

**예시** (Fun 서비스):
```
Hello.Fun.Service/
├── Services/
│   └── Loader/
│       ├── Src/
│       │   ├── Hello.Fun.Loader/
│       │   ├── Hello.Fun.Loader.Domain/
│       │   ├── Hello.Fun.Loader.Application/
│       │   ├── Hello.Fun.Loader.Adapters.Infrastructure/
│       │   ├── Hello.Fun.Loader.Adapters.Persistence/
│       │   └── Hello.Fun.Loader.Adapters.Presentation/
│       │
│       └── Tests/
│           ├── Mirero.Fun.Loader.Tests.Unit/
│           └── Mirero.Fun.Loader.Tests.Integration/
│
└── Hello.Pwm.Service.Tests.E2E/
```

### 단계별 커버리지 요구사항

**💼 비즈니스 관심사** (먼저 구현):
- **1단계 (Domain)**: 엔티티, 값 객체, 도메인 서비스 단위 테스트 (≥90%)
- **2단계 (Application)**: 유스케이스 단위 테스트 (≥80%)

**🔧 기술 관심사** (비즈니스 완성 후 구현):
- **3단계 (Adapters)**: 어댑터 단위/통합 테스트 (≥70%)
- **4단계 (E2E)**: 엔드투엔드 사용자 흐름 테스트 (1개 이상 핵심 경로)

### 테스트 명명 규칙

**T1_T2_T3 규칙**:
- **T1**: 테스트 대상 (메서드/기능명)
- **T2**: 예상 결과
- **T3**: 테스트 시나리오

```csharp
// 💼 Domain 테스트 예시
[Fact]
public void CalculateDiscount_Returns10Percent_WhenOrderAmountExceeds100()

// 💼 Application 테스트 예시 (유스케이스)
[Fact]
public void Execute_ReturnsSuccess_WhenValidOrderIsPlaced()

// 🔧 Adapter 테스트 예시
[Fact]
public void GetById_ReturnsEntity_WhenEntityExists()
```

---

## 🚀 구현 단계

### 1단계: [기반 단계 이름]
**목표**: [이 단계가 제공하는 구체적인 작동 기능]
**예상 시간**: X시간
**상태**: ⏳ 대기 중 | 🔄 진행 중 | ✅ 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 1.1**: [특정 기능]에 대한 단위 테스트 작성
  - 파일: `Tests/{서비스명}.Tests.Unit/LayerTests/[Layer]/[Component]Tests.cs`
  - 예상: 기능이 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - 정상 경로 시나리오
    - 엣지 케이스
    - 오류 조건

- [ ] **테스트 1.2**: [컴포넌트 상호작용]에 대한 통합 테스트 작성
  - 파일: `Tests/{서비스명}.Tests.Integration/[Feature]/[Feature]Tests.cs`
  - 예상: 통합이 아직 없으므로 테스트 실패(red)
  - 세부사항: [컴포넌트 목록] 간 상호작용 테스트

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [ ] **작업 1.3**: [컴포넌트/모듈] 구현
  - 파일: `Src/{서비스명}.[Layer]/[Component].cs`
  - 목표: 최소한의 코드로 테스트 1.1 통과
  - 세부사항: [구현 노트]

- [ ] **작업 1.4**: [통합/연결 코드] 구현
  - 파일: `Src/{서비스명}.Adapters.[Adapter]/[Integration].cs`
  - 목표: 테스트 1.2 통과
  - 세부사항: [구현 노트]

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 1.5**: 코드 품질을 위한 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 중복 제거 (DRY 원칙)
    - [ ] 명명 명확성 개선
    - [ ] 재사용 가능한 컴포넌트 추출
    - [ ] 인라인 문서 추가
    - [ ] 필요시 성능 최적화

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 2단계로 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족
  ```bash
  # .NET (Microsoft Testing Platform)
  dotnet test --configuration Release -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx

  # HTML 리포트 생성
  dotnet reportgenerator -reports:**/coverage.cobertura.xml -targetdir:.coverage/reports -reporttypes:"Html;Cobertura;MarkdownSummaryGithub" -assemblyfilters:"-*.Tests*"
  ```

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 기능이 예상대로 작동
- [ ] **엣지 케이스**: 경계 조건 테스트
- [ ] **오류 상태**: 오류 처리 확인

**검증 명령어** (.NET 프로젝트용):
```bash
# 테스트 실행
dotnet test --configuration Release

# 커버리지 수집
dotnet test --configuration Release -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml

# 코드 품질
dotnet format --verify-no-changes

# 빌드 검증
dotnet build --configuration Release --no-restore

# 보안 감사
dotnet list package --vulnerable
```

**수동 테스트 체크리스트**:
- [ ] 테스트 케이스 1: [확인할 특정 시나리오]
- [ ] 테스트 케이스 2: [확인할 엣지 케이스]
- [ ] 테스트 케이스 3: [확인할 오류 처리]

---

### 2단계: [핵심 기능 단계 이름]
**목표**: [구체적인 결과물]
**예상 시간**: X시간
**상태**: ⏳ 대기 중 | 🔄 진행 중 | ✅ 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 2.1**: [특정 기능]에 대한 단위 테스트 작성
  - 파일: `Tests/{서비스명}.Tests.Unit/LayerTests/[Layer]/[Component]Tests.cs`
  - 예상: 기능이 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - 정상 경로 시나리오
    - 엣지 케이스
    - 오류 조건

- [ ] **테스트 2.2**: [컴포넌트 상호작용]에 대한 통합 테스트 작성
  - 파일: `Tests/{서비스명}.Tests.Integration/[Feature]/[Feature]Tests.cs`
  - 예상: 통합이 아직 없으므로 테스트 실패(red)
  - 세부사항: [컴포넌트 목록] 간 상호작용 테스트

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [ ] **작업 2.3**: [컴포넌트/모듈] 구현
  - 파일: `Src/{서비스명}.[Layer]/[Component].cs`
  - 목표: 최소한의 코드로 테스트 2.1 통과
  - 세부사항: [구현 노트]

- [ ] **작업 2.4**: [통합/연결 코드] 구현
  - 파일: `Src/{서비스명}.Adapters.[Adapter]/[Integration].cs`
  - 목표: 테스트 2.2 통과
  - 세부사항: [구현 노트]

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 2.5**: 코드 품질을 위한 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 중복 제거 (DRY 원칙)
    - [ ] 명명 명확성 개선
    - [ ] 재사용 가능한 컴포넌트 추출
    - [ ] 인라인 문서 추가
    - [ ] 필요시 성능 최적화

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 3단계로 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 기능이 예상대로 작동
- [ ] **엣지 케이스**: 경계 조건 테스트
- [ ] **오류 상태**: 오류 처리 확인

**검증 명령어**:
```bash
[1단계와 동일 - 프로젝트에 맞게 커스터마이즈]
```

**수동 테스트 체크리스트**:
- [ ] 테스트 케이스 1: [확인할 특정 시나리오]
- [ ] 테스트 케이스 2: [확인할 엣지 케이스]
- [ ] 테스트 케이스 3: [확인할 오류 처리]

---

### 3단계: [개선 단계 이름]
**목표**: [구체적인 결과물]
**예상 시간**: X시간
**상태**: ⏳ 대기 중 | 🔄 진행 중 | ✅ 완료

#### 작업

**🔴 RED: 먼저 실패하는 테스트 작성**
- [ ] **테스트 3.1**: [특정 기능]에 대한 단위 테스트 작성
  - 파일: `Tests/{서비스명}.Tests.Unit/LayerTests/[Layer]/[Component]Tests.cs`
  - 예상: 기능이 아직 없으므로 테스트 실패(red)
  - 세부사항: 테스트 케이스:
    - 정상 경로 시나리오
    - 엣지 케이스
    - 오류 조건

- [ ] **테스트 3.2**: [컴포넌트 상호작용]에 대한 통합 테스트 작성
  - 파일: `Tests/{서비스명}.Tests.Integration/[Feature]/[Feature]Tests.cs`
  - 예상: 통합이 아직 없으므로 테스트 실패(red)
  - 세부사항: [컴포넌트 목록] 간 상호작용 테스트

**🟢 GREEN: 테스트를 통과시키기 위해 구현**
- [ ] **작업 3.3**: [컴포넌트/모듈] 구현
  - 파일: `Src/{서비스명}.[Layer]/[Component].cs`
  - 목표: 최소한의 코드로 테스트 3.1 통과
  - 세부사항: [구현 노트]

- [ ] **작업 3.4**: [통합/연결 코드] 구현
  - 파일: `Src/{서비스명}.Adapters.[Adapter]/[Integration].cs`
  - 목표: 테스트 3.2 통과
  - 세부사항: [구현 노트]

**🔵 REFACTOR: 코드 정리**
- [ ] **작업 3.5**: 코드 품질을 위한 리팩터링
  - 파일: 이 단계의 모든 새 코드 검토
  - 목표: 테스트를 깨뜨리지 않으면서 설계 개선
  - 체크리스트:
    - [ ] 중복 제거 (DRY 원칙)
    - [ ] 명명 명확성 개선
    - [ ] 재사용 가능한 컴포넌트 추출
    - [ ] 인라인 문서 추가
    - [ ] 필요시 성능 최적화

#### 품질 게이트 ✋

**⚠️ 중단: 모든 검사가 통과할 때까지 진행하지 마세요**

**TDD 준수** (필수):
- [ ] **Red 단계**: 테스트가 먼저 작성되고 초기에 실패함
- [ ] **Green 단계**: 테스트를 통과시키기 위해 프로덕션 코드 작성
- [ ] **Refactor 단계**: 테스트가 여전히 통과하면서 코드 개선
- [ ] **커버리지 확인**: 테스트 커버리지가 요구사항 충족

**빌드 및 테스트**:
- [ ] **빌드**: 프로젝트가 오류 없이 빌드/컴파일됨
- [ ] **모든 테스트 통과**: 100% 테스트 통과 (스킵된 테스트 없음)
- [ ] **테스트 성능**: 테스트 스위트가 허용 가능한 시간 내 완료
- [ ] **불안정한 테스트 없음**: 테스트가 일관되게 통과 (3회 이상 실행)

**코드 품질**:
- [ ] **린팅**: 린팅 오류나 경고 없음
- [ ] **포맷팅**: 프로젝트 표준에 따라 코드 포맷팅
- [ ] **타입 안전성**: 타입 검사기 통과 (해당되는 경우)
- [ ] **정적 분석**: 정적 분석 도구에서 심각한 문제 없음

**보안 및 성능**:
- [ ] **의존성**: 알려진 보안 취약점 없음
- [ ] **성능**: 성능 회귀 없음
- [ ] **메모리**: 메모리 누수나 리소스 문제 없음
- [ ] **오류 처리**: 적절한 오류 처리 구현

**문서화**:
- [ ] **코드 주석**: 복잡한 로직 문서화
- [ ] **API 문서**: 공개 인터페이스 문서화
- [ ] **README**: 필요시 사용 지침 업데이트

**수동 테스트**:
- [ ] **기능성**: 기능이 예상대로 작동
- [ ] **엣지 케이스**: 경계 조건 테스트
- [ ] **오류 상태**: 오류 처리 확인

**검증 명령어**:
```bash
[이전 단계와 동일 - 프로젝트에 맞게 커스터마이즈]
```

**수동 테스트 체크리스트**:
- [ ] 테스트 케이스 1: [확인할 특정 시나리오]
- [ ] 테스트 케이스 2: [확인할 엣지 케이스]
- [ ] 테스트 케이스 3: [확인할 오류 처리]

---

## ⚠️ 위험 평가

| 위험 | 확률 | 영향 | 완화 전략 |
|------|------|------|----------|
| [위험 1: 예) API 변경으로 통합 중단] | 낮음/중간/높음 | 낮음/중간/높음 | [구체적인 완화 단계] |
| [위험 2: 예) 성능 저하] | 낮음/중간/높음 | 낮음/중간/높음 | [구체적인 완화 단계] |
| [위험 3: 예) 데이터베이스 마이그레이션 문제] | 낮음/중간/높음 | 낮음/중간/높음 | [구체적인 완화 단계] |

---

## 🔄 롤백 전략

### 1단계 실패 시
**되돌리기 단계**:
- 다음 파일의 코드 변경 취소: [파일 목록]
- 구성 복원: [특정 설정]
- 의존성 제거: [추가된 경우]

### 2단계 실패 시
**되돌리기 단계**:
- 1단계 완료 상태로 복원
- 다음 파일의 변경 취소: [파일 목록]
- 데이터베이스 롤백: [해당되는 경우]

### 3단계 실패 시
**되돌리기 단계**:
- 2단계 완료 상태로 복원
- [추가 정리 단계]

---

## 📊 진행 상황 추적

### 완료 상태
- **1단계**: ⏳ 0% | 🔄 50% | ✅ 100%
- **2단계**: ⏳ 0% | 🔄 50% | ✅ 100%
- **3단계**: ⏳ 0% | 🔄 50% | ✅ 100%

**전체 진행률**: X% 완료

### 시간 추적
| 단계 | 예상 | 실제 | 차이 |
|------|------|------|------|
| 1단계 | X시간 | Y시간 | +/- Z시간 |
| 2단계 | X시간 | - | - |
| 3단계 | X시간 | - | - |
| **합계** | X시간 | Y시간 | +/- Z시간 |

---

## 📝 노트 및 학습

### 구현 노트
- [구현 중 발견한 인사이트 추가]
- [원래 계획에서 벗어난 결정 문서화]
- [유용한 디버깅 발견 기록]

### 발생한 블로커
- **블로커 1**: [설명] → [해결 방법]
- **블로커 2**: [설명] → [해결 방법]

### 향후 계획을 위한 개선점
- [다음에는 무엇을 다르게 하겠습니까?]
- [특히 잘 작동한 것은 무엇입니까?]

---

## 📚 참조

### 문서
- [관련 문서 링크]
- [API 참조 링크]
- [디자인 목업 링크]

### 관련 이슈
- 이슈 #X: [설명]
- PR #Y: [설명]

---

## ✅ 최종 체크리스트

**계획을 완료로 표시하기 전**:
- [ ] 모든 단계가 품질 게이트를 통과하며 완료됨
- [ ] 전체 통합 테스트 수행됨
- [ ] 문서 업데이트됨
- [ ] 성능 벤치마크가 목표 충족
- [ ] 보안 검토 완료됨
- [ ] 접근성 요구사항 충족 (UI 기능인 경우)
- [ ] 모든 이해관계자에게 알림
- [ ] 향후 참조를 위해 계획 문서 보관됨

---

## 📖 TDD 예제 워크플로우

### 예제: 사용자 인증 기능 추가

**1단계: RED (실패하는 테스트 작성)**

```csharp
// xUnit + Shouldly 사용 예제
[Fact]
public void Authenticate_ReturnsSuccess_WhenCredentialsAreValid()
{
    // Arrange
    var mockDatabase = Substitute.For<IUserDatabase>();
    var sut = new AuthService(mockDatabase);
    var validCredentials = new Credentials("user", "pass");

    // Act
    var actual = sut.Authenticate(validCredentials);

    // Assert
    actual.IsSuccess.ShouldBeTrue();
    actual.User.ShouldNotBeNull();
    // 테스트 실패 - AuthService가 아직 없음
}
```

**2단계: GREEN (최소 구현)**

```csharp
public class AuthService
{
    private readonly IUserDatabase _database;

    public AuthService(IUserDatabase database)
    {
        _database = database;
    }

    public AuthResult Authenticate(Credentials credentials)
    {
        // 테스트를 통과시키기 위한 최소한의 코드
        var user = _database.FindUser(credentials.Username);
        if (user != null && user.Password == credentials.Password)
        {
            return AuthResult.Success(user);
        }
        return AuthResult.Failure("Invalid credentials");
        // 테스트 통과 - 최소 기능 작동
    }
}
```

**3단계: REFACTOR (설계 개선)**

```csharp
public class AuthService
{
    private readonly IUserDatabase _database;
    private readonly ILogger _logger;

    public AuthService(IUserDatabase database, ILogger logger)
    {
        _database = database;
        _logger = logger;
    }

    public AuthResult Authenticate(Credentials credentials)
    {
        // 검증 추가
        if (!ValidateCredentials(credentials))
        {
            return AuthResult.Failure("Invalid input");
        }

        // 오류 처리 추가
        try
        {
            var user = _database.FindUser(credentials.Username);

            // 보안 비밀번호 비교 사용
            if (user != null && SecureCompare(user.Password, credentials.Password))
            {
                return AuthResult.Success(user);
            }

            return AuthResult.Failure("Invalid credentials");
        }
        catch (DatabaseException ex)
        {
            _logger.Error(ex);
            return AuthResult.Failure("Authentication failed");
        }
        // 테스트 여전히 통과 - 코드 품질 개선됨
    }

    private bool ValidateCredentials(Credentials credentials)
    {
        return !string.IsNullOrEmpty(credentials.Username)
            && !string.IsNullOrEmpty(credentials.Password);
    }

    private bool SecureCompare(string stored, string provided)
    {
        // 타이밍 공격 방지를 위한 보안 비교
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(stored),
            Encoding.UTF8.GetBytes(provided));
    }
}
```

### TDD Red-Green-Refactor 사이클 시각화

```
1단계: 🔴 RED
├── 기능 X에 대한 테스트 작성
├── 테스트 실행 → 실패 ❌
└── 커밋: "X에 대한 실패하는 테스트 추가"

2단계: 🟢 GREEN
├── 최소한의 코드 작성
├── 테스트 실행 → 통과 ✅
└── 커밋: "테스트를 통과시키기 위해 X 구현"

3단계: 🔵 REFACTOR
├── 코드 품질 개선
├── 테스트 실행 → 여전히 통과 ✅
├── 헬퍼 메서드 추출
├── 테스트 실행 → 여전히 통과 ✅
├── 명명 개선
├── 테스트 실행 → 여전히 통과 ✅
└── 커밋: "더 나은 설계를 위해 X 리팩터링"

다음 기능으로 반복 →
```

### 이 접근 방식의 이점

**안전성**: 테스트가 회귀를 즉시 감지
**설계**: 테스트가 API 설계를 먼저 생각하도록 강제
**문서화**: 테스트가 예상 동작을 문서화
**자신감**: 두려움 없이 리팩터링 가능
**품질**: 처음부터 높은 코드 커버리지
**디버깅**: 실패가 정확한 문제 영역을 가리킴

---

**계획 상태**: 🔄 진행 중
**다음 작업**: [다음에 해야 할 일]
**블로커**: [현재 블로커] 또는 없음

