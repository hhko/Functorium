# Phase 5: 검증

## 목표

생성된 릴리스 노트의 품질 및 정확성을 검증합니다.

## 성공 기준

### 포괄적인 분석

- [ ] 모든 컴포넌트 분석 파일이 커밋 기반 기능을 위해 검토됨
- [ ] 모든 중요한 커밋이 분석되고 사용자 대면 기능으로 변환됨
- [ ] 커밋 패턴이 식별되고 응집력 있는 기능 섹션으로 그룹화됨
- [ ] 멀티 컴포넌트 기능이 컴포넌트 간 관련 커밋에서 통합됨

### API 정확성

- [ ] API 변경 요약이 새 API 및 변경 식별에 사용됨
- [ ] 모든 코드 샘플이 Uber 파일에서 검증된 API 사용
- [ ] 모든 API 참조가 정확하고 완전하며 작동하는 코드 샘플 포함
- [ ] 허구의 기능이 문서화되지 않음

### 브레이킹 체인지 및 마이그레이션

- [ ] 브레이킹 체인지가 실제 API diff 및 커밋 반영
- [ ] 모든 브레이킹 체인지에 대한 마이그레이션 가이드 제공
- [ ] API 변경에 대한 이전/이후 예시 포함

### 구조 및 품질

- [ ] 문서가 확립된 템플릿 구조를 따름
- [ ] 독자가 기능에 대한 추가 정보를 어디서 찾을 수 있는지 앎
- [ ] 일관된 포맷팅
- [ ] 전문적이고 개발자 중심 언어

## 검증 프로세스

릴리스 노트 게시 전:

### 1. 모든 코드 샘플을 Uber 파일과 교차 참조

```bash
grep -n "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
```

### 2. 브레이킹 체인지 확인

브레이킹 체인지 확인 위치:

- `.analysis-output/api-changes-build-current/all-api-changes.txt` - 현재 API 정의
- `.analysis-output/api-changes-build-current/api-changes-diff.txt` - **Git Diff 기반 API 변경** (권장)
- `Src/*/.api/*.cs` - 개별 어셈블리 API
- `.analysis-output/*.md` - 컴포넌트별 커밋 분석 (Breaking changes 섹션)

#### Git Diff 기반 Breaking Changes 검증 (권장)

`api-changes-diff.txt` 파일을 분석하여 Breaking Changes를 객관적으로 검증합니다:

```bash
# api-changes-diff.txt 확인
cat .release-notes/scripts/.analysis-output/api-changes-build-current/api-changes-diff.txt

# 삭제된 API (-) 검색
grep "^-.*public" api-changes-diff.txt

# 변경된 메서드 시그니처 검색
grep -A 1 "^-.*public.*(\" api-changes-diff.txt
```

**검증 항목:**

- [ ] Git Diff에서 감지된 모든 삭제 API가 문서화됨
- [ ] Git Diff에서 감지된 모든 시그니처 변경이 문서화됨
- [ ] 커밋 메시지 패턴(`!:`, `breaking`)으로 표시된 커밋도 포함됨
- [ ] 각 Breaking Change에 마이그레이션 가이드 존재
- [ ] 이전/이후 코드 비교 포함 (Git Diff에서 추출)

### 3. 생성된 문서에 markdownlint 실행

```bash
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-{version}.md --disable MD013
```

### 4. 추적성 참조 검증

문서화된 각 변경에 대해 다음을 확인:

- **커밋 SHA 또는 메시지** - 컴포넌트 분석에서
- **GitHub 이슈 ID** (커밋 메시지에 참조된 경우)
- **GitHub Pull Request 번호** (가능한 경우)
- **컴포넌트 이름** - 변경이 발견된 곳

예시 형식:

```
기능: 오류 디스트럭처링 개선
소스: Functorium의 "Add ManyErrorsDestructurer" 커밋
GitHub PR: #98
GitHub 이슈: #95 (참조된 경우)
```

### 5. 콘텐츠 품질 검토

- [ ] **정확성**: 모든 API가 Uber 파일에 존재
- [ ] **완전성**: 모든 주요 커밋 표현, 중요한 기능 누락 없음
- [ ] **명확성**: 개발자 중심 언어, 명확한 예시, 실행 가능한 가이드
- [ ] **일관성**: 템플릿 구조 따름, 일관된 포맷팅
- [ ] **추적성**: 모든 문서화된 기능을 커밋/PR로 추적 가능

## 게시 전 체크리스트

### 최종 검토 항목

1. **프론트매터 검증**
   - [ ] 버전 번호가 포함된 올바른 제목
   - [ ] 정확한 설명
   - [ ] 현재 날짜

2. **콘텐츠 구조**
   - [ ] 버전 정보가 포함된 소개 단락
   - [ ] 주요 섹션
   - [ ] 브레이킹 체인지 섹션 (해당되는 경우)
   - [ ] 적절한 제목 계층 구조

3. **코드 예시**
   - [ ] 모든 코드 블록에 언어 지정
   - [ ] 모든 API가 Uber 파일에서 검증됨
   - [ ] 가능한 경우 완전하고 실행 가능한 예시
   - [ ] 일관된 변수 명명

4. **링크 및 참조**
   - [ ] 내부 링크는 상대 경로 사용
   - [ ] 외부 링크는 전체 URL 사용
   - [ ] 주요 기능에 GitHub 이슈/PR 링크 포함

## 피해야 할 일반적인 문제

### API 문서화 오류

- Uber 파일에 존재하지 않는 API 문서화
- 잘못된 매개변수 이름이나 타입 사용
- 존재하지 않는 플루언트 체인 생성

### 구조 문제

- 브레이킹 체인지에 대한 마이그레이션 가이드 누락
- 좋지 않은 구성 (주요 기능 전에 사소한 기능)
- 프론트매터 누락 또는 잘못된 YAML

### 콘텐츠 품질 문제

- 개발자 중심 대신 모호하거나 기술적인 언어
- API 변경에 대한 코드 예시 누락
- 관련 문서 링크 없음
- 소스 커밋에 대한 불충분한 추적성

## 중간 결과 저장

Phase 5의 검증 결과를 `.analysis-output/work/` 폴더에 저장합니다.

### 저장할 파일

```
.release-notes/scripts/.analysis-output/work/
├── phase5-validation-report.md   # 검증 결과 보고서
├── phase5-api-validation.md      # API 검증 상세
└── phase5-errors.md              # 발견된 오류 (실패 시)
```

### phase5-validation-report.md 형식

```markdown
# Phase 5: 검증 결과 보고서

## 검증 일시
2025-12-18T10:30:00

## 검증 대상
.release-notes/RELEASE-v1.0.0-alpha.1.md

## 검증 결과 요약
- API 정확성: ✓ 통과 (30개 타입 검증)
- Breaking Changes: ✓ 통과 (0개)
- Markdown 포맷: ✓ 통과
- 체크리스트: ✓ 100%

## 상세 결과
[각 검증 항목별 상세 결과]
```

## 콘솔 출력 형식

### 검증 통과

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: 검증 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

검증 항목 통과:
  ✓ API 정확성 (0 오류)
    - ErrorCodeFactory ✓
    - OpenTelemetryRegistration ✓
    - ArchitectureValidationEntryPoint ✓
    - HostTestFixture ✓
    - QuartzTestFixture ✓
    - LogEventPropertyExtractor ✓
    - FinTUtilites ✓

  ✓ Breaking Changes 완전성
    - 첫 릴리스, Breaking Changes 없음

  ✓ Markdown 포맷
    - H1 제목: 1개
    - 일관된 제목 계층
    - 코드 블록 언어 지정: 100%

  ✓ 체크리스트 (100%)
    - 포괄적인 분석 ✓
    - API 정확성 ✓
    - 구조 및 품질 ✓

검증 결과 저장:
  ✓ .release-notes/scripts/.analysis-output/work/phase5-validation-report.md
  ✓ .release-notes/scripts/.analysis-output/work/phase5-api-validation.md

상태: 게시 가능 ✓
```

### 검증 실패

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 5: 검증 실패 ✗
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

발견된 문제:

API 정확성 (2 오류):
  ✗ ErrorCodeFactory.FromException (line 123)
    위치: RELEASE-v1.0.0-alpha.1.md:123
    문제: Uber 파일에 없는 API
    제안: ErrorCodeFactory.CreateFromException 사용

  ✗ OpenTelemetryBuilder.Register (line 456)
    위치: RELEASE-v1.0.0-alpha.1.md:456
    문제: 매개변수 불일치
    Uber: RegisterObservability(IServiceCollection, IConfiguration)
    문서: Register(IServiceCollection)

Breaking Changes (1 오류):
  ✗ IErrorHandler → IErrorDestructurer 이름 변경
    문제: 마이그레이션 가이드 누락
    필요: 이전/이후 코드 예시 및 단계별 가이드

Markdown 포맷 (경고):
  ⚠ 코드 블록 언어 미지정: 2개
    - Line 234: ```
    - Line 567: ```

검증 결과 저장:
  ✓ .release-notes/scripts/.analysis-output/work/phase5-validation-report.md
  ✓ .release-notes/scripts/.analysis-output/work/phase5-errors.md

조치 필요:
  1. 문서 수정
  2. 검증 재실행
```

## 품질 지표

다음 품질 지표 추적:

- **커버리지**: 문서화된 중요한 커밋의 비율
- **정확성**: 발명된 API 또는 존재하지 않는 명령 없음
- **추적성**: 모든 기능이 소스 커밋/PR로 추적 가능
- **완전성**: 모든 브레이킹 체인지가 마이그레이션 경로와 함께 문서화됨
- **개발자 중심**: 모든 주요 기능에 대한 명확하고 실행 가능한 예시

## 기억하세요: 완전성보다 정확성

많은 추측성 기능보다 실제 커밋 기반 개선을 나타내는 더 적지만 정확한 기능이 낫습니다. 문서화된 모든 기능은 다음을 충족해야 합니다:

1. **추적 가능** - 실제 커밋으로
2. **검증 가능** - Uber 파일 또는 커밋 분석을 통해
3. **실행 가능** - 작동하는 코드 예시와 함께
4. **가치 있음** - 개발자 커뮤니티에

## 게시 후 검증

게시 후 확인:

1. **문서가 올바르게 렌더링됨**
2. **모든 링크가 작동**하고 올바른 대상을 가리킴
3. **코드 예시가 컴파일**되고 성공적으로 실행됨
4. **내부 링크 깨짐 없음** 또는 누락된 페이지 없음

## 통과 기준

- [ ] Uber 파일에 없는 API 사용: 0개
- [ ] Git Diff에서 감지된 모든 Breaking Changes 문서화됨
- [ ] 각 Breaking Change에 마이그레이션 가이드 포함
- [ ] 검증 결과 저장됨

## 완료

검증 통과 후 릴리스 노트 생성이 완료됩니다. [README.md](README.md)에서 완료 메시지 형식을 참조하세요.
