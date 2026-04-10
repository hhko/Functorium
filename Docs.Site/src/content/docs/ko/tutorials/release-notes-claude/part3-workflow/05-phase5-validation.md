---
title: "검증"
---

릴리스 노트는 정확해야만 가치가 있습니다. 존재하지 않는 API를 문서화하거나, Breaking Change를 빠뜨리거나, 마이그레이션 가이드가 없다면 개발자에게 혼란만 줍니다. Phase 5는 완성된 릴리스 노트가 이런 문제 없이 게시될 수 있는지 확인하는 품질 보증 단계입니다.

## 검증 항목

### 1. 포괄적인 분석

- [ ] 모든 컴포넌트 분석 파일이 검토됨
- [ ] 모든 중요한 커밋이 분석됨
- [ ] 커밋 패턴이 응집력 있는 기능 섹션으로 그룹화됨
- [ ] 멀티 컴포넌트 기능이 통합됨

### 2. API 정확성

- [ ] API 변경 요약이 새 API 및 변경 식별에 사용됨
- [ ] 모든 코드 예제이 Uber 파일에서 검증된 API 사용
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

## 검증 프로세스

### 1. 코드 예제 교차 참조

릴리스 노트에 포함된 모든 코드 예제을 Uber 파일과 교차 참조합니다. 이 단계가 가장 중요한데, Phase 4에서 AI가 작성한 코드 예제에 실제로 존재하지 않는 메서드나 매개변수가 포함될 수 있기 때문입니다.

```bash
# API 존재 확인
grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt

# 시그니처 확인
grep -A 5 "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. Breaking Changes 확인

Breaking Changes를 확인할 때는 여러 소스를 교차 검토합니다.

| 파일 | 용도 |
|------|------|
| `all-api-changes.txt` | 현재 API 정의 |
| `api-changes-diff.txt` | Git Diff 기반 API 변경 (권장) |
| `Src/*/.api/*.cs` | 개별 어셈블리 API |
| `.analysis-output/*.md` | 커밋 분석 (Breaking changes 섹션) |

Git Diff 기반 검증이 가장 신뢰할 수 있습니다.

```bash
# api-changes-diff.txt 확인
cat .release-notes/scripts/.analysis-output/api-changes-build-current/api-changes-diff.txt

# 삭제된 API (-) 검색
grep "^-.*public" api-changes-diff.txt

# 변경된 메서드 시그니처 검색
grep -A 1 "^-.*public.*(" api-changes-diff.txt
```

검증해야 할 사항은 다음과 같습니다.

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

문서화된 각 변경에 대해 커밋 SHA 또는 메시지, GitHub 이슈 ID(참조된 경우), GitHub Pull Request 번호(가능한 경우), 컴포넌트 이름이 추적 가능한지 확인합니다.

### 5. 콘텐츠 품질 검토

- [ ] **정확성:** 모든 API가 Uber 파일에 존재
- [ ] **완전성:** 모든 주요 커밋 포함
- [ ] **명확성:** 개발자 중심 언어, 명확한 예시
- [ ] **일관성:** 템플릿 구조 준수
- [ ] **추적성:** 모든 기능이 커밋/PR로 추적 가능

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

## 피해야 할 일반적인 문제

### API 문서화 오류

Uber 파일에 없는 API를 문서화하는 것은 가장 빈번하게 발생하는 문제입니다. AI가 기존 API의 패턴을 보고 존재하지 않는 메서드를 유추하거나, 플루언트 체인을 만들어내는 경우가 있습니다. 반드시 Uber 파일에서 검증해야 합니다. 매개변수 이름이나 타입이 틀리는 것도 흔한 문제인데, 특히 비슷한 이름의 매개변수가 여러 개 있을 때 순서가 뒤바뀌기 쉽습니다.

### 구조 문제

Breaking Change에 마이그레이션 가이드가 누락되면, 개발자는 어떻게 업그레이드해야 할지 알 수 없습니다. 또한 섹션 순서가 잘못되면 중요도가 낮은 변경이 먼저 나와 가독성이 떨어집니다. Breaking Changes를 먼저 배치하고, 그다음 주요 기능 순으로 구성해야 합니다.

### 콘텐츠 품질 문제

모호한 언어("기능을 제공합니다")는 구체적인 개발자 중심 표현으로 바꿔야 합니다. 코드 예시가 없는 API 설명은 개발자가 실제로 활용하기 어렵습니다. 커밋 SHA나 PR 번호 없이 기능만 나열하면, 나중에 해당 변경의 맥락을 확인할 수 없습니다.

## 중간 결과 저장

Phase 5의 검증 결과를 저장합니다.

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

## 품질 지표

다음 품질 지표를 추적합니다.

| 지표 | 설명 | 목표 |
|------|------|------|
| 커버리지 | 문서화된 중요한 커밋 비율 | 100% |
| 정확성 | 발명된 API 없음 | 0개 |
| 추적성 | 소스 커밋/PR로 추적 가능 | 100% |
| 완전성 | Breaking Changes 마이그레이션 가이드 | 100% |

## 완전성보다 정확성

이 워크플로우 전체를 관통하는 핵심 원칙은 **완전성보다 정확성입니다.** 많은 추측성 기능을 나열하는 것보다, 더 적지만 확실히 정확한 기능을 문서화하는 것이 낫습니다.

이 원칙이 중요한 이유는, 릴리스 노트에 잘못된 정보가 하나라도 있으면 문서 전체의 신뢰도가 떨어지기 때문입니다. 개발자가 문서를 보고 코드를 작성했는데 실제 API와 다르다면, 그 다음부터는 릴리스 노트를 신뢰하지 않게 됩니다. 반면 일부 기능이 빠져 있더라도 문서화된 내용이 모두 정확하다면, 개발자는 릴리스 노트를 믿고 활용할 수 있습니다.

문서화된 모든 기능은 네 가지 조건을 충족해야 합니다. 실제 커밋으로 **추적 가능하고,** Uber 파일 또는 커밋 분석을 통해 **검증 가능하며,** 작동하는 코드 예시와 함께 **실행 가능하고,** 개발자에게 **가치 있어야** 합니다.

## 통과 기준

- [ ] Uber 파일에 없는 API 사용: 0개
- [ ] Git Diff에서 감지된 모든 Breaking Changes 문서화됨
- [ ] 각 Breaking Change에 마이그레이션 가이드 포함
- [ ] 검증 결과 저장됨

## 완료 메시지

검증을 통과하면 다음과 같은 완료 메시지가 출력됩니다.

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

## FAQ

### Q1: Phase 5 검증에서 API 정확성 오류가 발견되면 어떻게 수정하나요?
**A**: 오류 메시지에 표시된 API 이름과 위치(라인 번호)를 확인한 뒤, Uber 파일(`all-api-changes.txt`)에서 올바른 시그니처를 검색합니다. 릴리스 노트의 해당 부분을 Uber 파일의 정확한 시그니처로 수정한 뒤, Phase 5 검증을 다시 실행하면 됩니다.

### Q2: "완전성보다 정확성" 원칙이 중요한 이유는 무엇인가요?
**A**: 릴리스 노트에 잘못된 정보가 하나라도 있으면 **문서 전체의 신뢰도가 떨어지기** 때문입니다. 개발자가 문서를 보고 코드를 작성했는데 실제 API와 다르다면, 그 이후에는 릴리스 노트를 신뢰하지 않게 됩니다. 일부 기능이 빠져 있더라도 문서화된 내용이 모두 정확하면 개발자가 안심하고 활용할 수 있습니다.

### Q3: Phase 5 검증 결과 파일은 왜 별도로 저장하나요?
**A**: 검증 보고서(`phase5-validation-report.md`, `phase5-api-validation.md`)를 저장하면, 나중에 릴리스 노트의 품질 이력을 추적하고 반복 발생하는 오류 패턴을 파악할 수 있습니다. 또한 검증 실패 시 어떤 항목이 문제였는지 정확히 확인하여 수정 후 재검증하는 데 활용됩니다.

### Q4: Breaking Changes 마이그레이션 가이드가 누락되면 Phase 5에서 반드시 실패하나요?
**A**: 네, Breaking Change가 감지되었는데 마이그레이션 가이드가 없으면 **"Breaking Changes 완전성" 항목에서 실패합니다.** 개발자가 업그레이드 방법을 모른 채 Breaking Change를 만나면 큰 혼란이 생기므로, 이전/이후 코드 비교와 단계별 마이그레이션 가이드를 반드시 포함해야 합니다.

이것으로 5-Phase 워크플로우가 모두 완료됩니다. `/release-note v1.2.0` 한 줄의 명령이 환경 검증, 데이터 수집, 커밋 분석, 문서 작성, 품질 검증을 거쳐 게시 가능한 릴리스 노트로 완성되었습니다. 이 워크플로우를 지원하는 C# 스크립트에 대해 더 알아보려면 [.NET 10 File-based App 소개](../Part4-Implementation/01-file-based-apps.md)를 참고하세요.
