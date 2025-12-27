# 4.5 Phase 5: 검증

> Phase 5에서는 생성된 릴리스 노트의 품질 및 정확성을 검증합니다.

---

## 목표

생성된 릴리스 노트의 품질 및 정확성을 검증합니다.

---

## 검증 항목

### 1. 포괄적인 분석

- [ ] 모든 컴포넌트 분석 파일이 검토됨
- [ ] 모든 중요한 커밋이 분석됨
- [ ] 커밋 패턴이 응집력 있는 기능 섹션으로 그룹화됨
- [ ] 멀티 컴포넌트 기능이 통합됨

### 2. API 정확성

- [ ] API 변경 요약이 새 API 및 변경 식별에 사용됨
- [ ] 모든 코드 샘플이 Uber 파일에서 검증된 API 사용
- [ ] 모든 API 참조가 정확하고 완전함
- [ ] 허구의 기능이 문서화되지 않음

### 3. Breaking Changes 완전성

- [ ] Breaking Changes가 실제 API diff 및 커밋 반영
- [ ] 모든 Breaking Changes에 대한 마이그레이션 가이드 제공
- [ ] API 변경에 대한 이전/이후 예시 포함

### 4. 구조 및 품질

- [ ] 문서가 확립된 템플릿 구조를 따름
- [ ] 일관된 포맷팅
- [ ] 전문적이고 개발자 중심 언어

---

## 검증 프로세스

### 1. 코드 샘플 교차 참조

모든 코드 샘플을 Uber 파일과 교차 참조합니다:

```bash
# API 존재 확인
grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt

# 시그니처 확인
grep -A 5 "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. Breaking Changes 확인

Breaking Changes 확인 위치:

| 파일 | 용도 |
|------|------|
| `all-api-changes.txt` | 현재 API 정의 |
| `api-changes-diff.txt` | Git Diff 기반 API 변경 (권장) |
| `Src/*/.api/*.cs` | 개별 어셈블리 API |
| `.analysis-output/*.md` | 커밋 분석 (Breaking changes 섹션) |

#### Git Diff 기반 검증 (권장)

```bash
# api-changes-diff.txt 확인
cat .release-notes/scripts/.analysis-output/api-changes-build-current/api-changes-diff.txt

# 삭제된 API (-) 검색
grep "^-.*public" api-changes-diff.txt

# 변경된 메서드 시그니처 검색
grep -A 1 "^-.*public.*(" api-changes-diff.txt
```

**검증 항목:**
- [ ] Git Diff에서 감지된 모든 삭제 API가 문서화됨
- [ ] Git Diff에서 감지된 모든 시그니처 변경이 문서화됨
- [ ] 커밋 메시지 패턴(`!:`, `breaking`)으로 표시된 커밋도 포함됨
- [ ] 각 Breaking Change에 마이그레이션 가이드 존재
- [ ] 이전/이후 코드 비교 포함

### 3. Markdown 검증

```bash
# markdownlint 실행
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-v1.2.0.md --disable MD013
```

### 4. 추적성 검증

문서화된 각 변경에 대해 다음을 확인:

- **커밋 SHA 또는 메시지** - 컴포넌트 분석에서
- **GitHub 이슈 ID** (참조된 경우)
- **GitHub Pull Request 번호** (가능한 경우)
- **컴포넌트 이름** - 변경이 발견된 곳

### 5. 콘텐츠 품질 검토

- [ ] **정확성**: 모든 API가 Uber 파일에 존재
- [ ] **완전성**: 모든 주요 커밋 포함
- [ ] **명확성**: 개발자 중심 언어, 명확한 예시
- [ ] **일관성**: 템플릿 구조 준수
- [ ] **추적성**: 모든 기능이 커밋/PR로 추적 가능

---

## 게시 전 체크리스트

### 프론트매터 검증

```bash
# 프론트매터 확인
head -10 .release-notes/RELEASE-v1.2.0.md
```

- [ ] 버전 번호가 포함된 올바른 제목
- [ ] 정확한 설명
- [ ] 현재 날짜

### 필수 섹션 확인

```bash
# 섹션 제목 목록
grep "^##" .release-notes/RELEASE-v1.2.0.md
```

- [ ] 개요
- [ ] Breaking Changes (없으면 명시)
- [ ] 새로운 기능
- [ ] API 변경사항
- [ ] 설치

### "Why this matters" 섹션 확인

```bash
# "Why this matters" 섹션 개수 확인
grep -c "**Why this matters" .release-notes/RELEASE-v1.2.0.md
```

- [ ] 모든 주요 기능에 "Why this matters" 섹션 존재

### 코드 블록 언어 지정

```bash
# 언어 미지정 코드 블록 검색
grep -n "^\`\`\`$" .release-notes/RELEASE-v1.2.0.md
```

- [ ] 모든 코드 블록에 언어 지정 (```csharp, ```bash, ```json)

---

## 피해야 할 일반적인 문제

### API 문서화 오류

| 문제 | 해결 |
|------|------|
| Uber 파일에 없는 API 문서화 | Uber 파일에서 검증 |
| 잘못된 매개변수 이름/타입 | Uber 파일 시그니처 확인 |
| 존재하지 않는 플루언트 체인 | 실제 API만 문서화 |

### 구조 문제

| 문제 | 해결 |
|------|------|
| 마이그레이션 가이드 누락 | 모든 Breaking Change에 추가 |
| 좋지 않은 구성 | Breaking Changes → 주요 기능 순서 |
| 프론트매터 누락 | YAML 헤더 추가 |

### 콘텐츠 품질 문제

| 문제 | 해결 |
|------|------|
| 모호한 언어 | 개발자 중심으로 수정 |
| 코드 예시 누락 | 모든 API 변경에 코드 추가 |
| 추적성 부족 | 커밋 SHA/PR 번호 추가 |

---

## 중간 결과 저장

Phase 5의 검증 결과를 저장합니다:

```txt
.release-notes/scripts/.analysis-output/work/
├── phase5-validation-report.md   # 검증 결과 보고서
├── phase5-api-validation.md      # API 검증 상세
└── phase5-errors.md              # 발견된 오류 (실패 시)
```

### phase5-validation-report.md 형식

````markdown
# Phase 5: 검증 결과 보고서

## 검증 일시
2025-12-19T10:30:00

## 검증 대상
.release-notes/RELEASE-v1.2.0.md

## 검증 결과 요약
- API 정확성: 통과 (30개 타입 검증)
- Breaking Changes: 통과 (0개)
- Markdown 포맷: 통과
- 체크리스트: 100%

## 상세 결과
[각 검증 항목별 상세 결과]
````

---

## 콘솔 출력 형식

### 검증 통과

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: 검증 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

검증 항목 통과:
  API 정확성 (0 오류)
    - ErrorCodeFactory
    - OpenTelemetryRegistration
    - ArchitectureValidationEntryPoint
    - HostTestFixture
    - QuartzTestFixture

  Breaking Changes 완전성
    - 첫 릴리스, Breaking Changes 없음

  Markdown 포맷
    - H1 제목: 1개
    - 일관된 제목 계층
    - 코드 블록 언어 지정: 100%

  체크리스트 (100%)
    - 포괄적인 분석
    - API 정확성
    - 구조 및 품질

검증 결과 저장:
  .analysis-output/work/phase5-validation-report.md
  .analysis-output/work/phase5-api-validation.md

상태: 게시 가능
```

### 검증 실패

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: 검증 실패
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

발견된 문제:

API 정확성 (2 오류):
  ErrorCodeFactory.FromException (line 123)
    위치: RELEASE-v1.2.0.md:123
    문제: Uber 파일에 없는 API
    제안: ErrorCodeFactory.CreateFromException 사용

  OpenTelemetryBuilder.Register (line 456)
    위치: RELEASE-v1.2.0.md:456
    문제: 매개변수 불일치
    Uber: RegisterObservability(IServiceCollection, IConfiguration)
    문서: Register(IServiceCollection)

Breaking Changes (1 오류):
  IErrorHandler → IErrorDestructurer 이름 변경
    문제: 마이그레이션 가이드 누락
    필요: 이전/이후 코드 예시 및 단계별 가이드

조치 필요:
  1. 문서 수정
  2. 검증 재실행
```

---

## 품질 지표

다음 품질 지표를 추적합니다:

| 지표 | 설명 | 목표 |
|------|------|------|
| 커버리지 | 문서화된 중요한 커밋 비율 | 100% |
| 정확성 | 발명된 API 없음 | 0개 |
| 추적성 | 소스 커밋/PR로 추적 가능 | 100% |
| 완전성 | Breaking Changes 마이그레이션 가이드 | 100% |

---

## 핵심 원칙

> **완전성보다 정확성**

많은 추측성 기능보다 **더 적지만 정확한 기능**이 낫습니다.

문서화된 모든 기능은:
1. **추적 가능** - 실제 커밋으로
2. **검증 가능** - Uber 파일 또는 커밋 분석을 통해
3. **실행 가능** - 작동하는 코드 예시와 함께
4. **가치 있음** - 개발자에게

---

## 통과 기준

- [ ] Uber 파일에 없는 API 사용: 0개
- [ ] Git Diff에서 감지된 모든 Breaking Changes 문서화됨
- [ ] 각 Breaking Change에 마이그레이션 가이드 포함
- [ ] 검증 결과 저장됨

---

## 완료 메시지

검증 통과 후 다음 형식으로 완료 메시지를 출력합니다:

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
릴리스 노트 생성 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

버전: v1.2.0
파일: .release-notes/RELEASE-v1.2.0.md

통계 요약
| 항목 | 값 |
|------|-----|
| Functorium | 31 files, 19 commits |
| Functorium.Testing | 18 files, 13 commits |
| Breaking Changes | 0개 |

다음 단계
1. 생성된 릴리스 노트 검토
2. 필요시 수동 수정
3. Git에 커밋
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## 요약

| 항목 | 설명 |
|------|------|
| 목표 | 품질 및 정확성 검증 |
| 검증 항목 | API 정확성, Breaking Changes, 구조 |
| 통과 기준 | Uber 파일 검증, 마이그레이션 가이드 |
| 출력 | 검증 보고서, 완료 메시지 |

---

## 5-Phase 워크플로우 완료

축하합니다! 5-Phase 워크플로우를 모두 완료했습니다.

```txt
Phase 1: 환경 검증    ──▶ 완료
Phase 2: 데이터 수집  ──▶ 완료
Phase 3: 커밋 분석    ──▶ 완료
Phase 4: 문서 작성    ──▶ 완료
Phase 5: 검증         ──▶ 완료

결과: 고품질 릴리스 노트 생성!
```

---

## 다음 단계

5-Phase 워크플로우를 지원하는 C# 스크립트에 대해 알아봅니다:

- [5.1 .NET 10 File-based App 소개](../05-csharp-scripts/01-file-based-apps.md)
