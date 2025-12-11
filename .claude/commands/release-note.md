---
title: RELEASE-NOTE
description: Git log를 분석하여 고객 친화적인 릴리스 노트를 생성합니다.
argument-hint: "[version] [topic]을 전달하면 해당 버전으로 릴리스 노트를 생성합니다"
---

# 릴리스 노트 생성

Git log 정보를 수집하고 Conventional Commits를 분류하여 고객 친화적인 릴리스 노트를 `.release-notes` 폴더에 생성합니다.

## 파라미터 (`$ARGUMENTS`)

**전달된 파라미터:** $ARGUMENTS

### 파라미터 파싱 규칙

`$ARGUMENTS`를 분석하여 다음을 추출합니다:

1. **버전**: `v`로 시작하는 문자열 (예: `v1.3.0`, `v2.0.0-beta`)
2. **토픽**: 버전을 제외한 나머지 문자열

**파싱 예시:**
| 입력 | 버전 | 토픽 |
|------|------|------|
| (없음) | 자동 결정 | (없음) |
| `v1.3.0` | v1.3.0 | (없음) |
| `VSCode 개발 환경` | 자동 결정 | VSCode 개발 환경 |
| `v1.3.0 VSCode 개발 환경` | v1.3.0 | VSCode 개발 환경 |
| `VSCode v1.3.0` | v1.3.0 | VSCode |

### 버전 자동 결정 (버전 미지정 시)

- 마지막 태그를 기반으로 다음 버전을 자동 결정합니다
- feat 커밋이 있으면 Minor 버전 증가
- fix/perf 커밋만 있으면 Patch 버전 증가

**사용 예시:**
```
/release-note                          # 자동 버전, 일반 Overview
/release-note v1.3.0                   # v1.3.0 버전, 일반 Overview
/release-note VSCode 개발 환경          # 자동 버전, VSCode 중심 Overview
/release-note v1.3.0 Observability     # v1.3.0 버전, Observability 중심 Overview
```

## 실행 절차

### 1. Git 저장소 확인

Git 저장소인지 확인합니다.

### 2. 현재 버전 및 범위 확인

```bash
# 최신 태그 확인
git describe --tags --abbrev=0 2>/dev/null

# 모든 태그 목록
git tag --list --sort=-v:refname | head -5
```

- 태그가 없으면 v1.0.0을 기준으로 사용
- 범위: `{마지막태그}..HEAD`

### 3. 커밋 수집

```bash
# 마지막 태그 이후 커밋 목록 (해시|제목 형식)
git log {last-tag}..HEAD --format="%h|%s" --no-merges
```

### 4. Conventional Commits 분류

각 커밋을 다음 형식으로 파싱합니다:
```
<type>[optional scope][!]: <description>
```

**분류 기준:**

| 타입 | 릴리스 노트 섹션 | 포함 여부 |
|------|-----------------|----------|
| feat | New Features | 포함 |
| fix | Bug Fixes | 포함 |
| perf | Performance Improvements | 포함 |
| docs | Documentation | 선택적 포함 |
| ! (Breaking) | Breaking Changes | 별도 섹션으로 강조 |
| refactor, test, ci, chore, style, build | - | 제외 (내부 변경) |

**Breaking Changes 감지:**
- 타입 뒤에 느낌표(!)가 있는 경우: feat!:, fix!:
- 커밋 본문에 BREAKING CHANGE: 푸터가 있는 경우

### 5. 버전 결정 (파라미터 미지정 시)

`$ARGUMENTS`가 비어있으면:
1. 마지막 태그에서 버전 추출
2. Breaking Change 또는 feat이 있으면 → Minor 증가
3. fix/perf만 있으면 → Patch 증가
4. 태그 형식: `v{major}.{minor}.{patch}`

### 6. 릴리스 노트 작성

**고객 친화적 작성 규칙:**

1. **기술 용어 최소화**: 내부 구현 세부사항 제외
2. **사용자 관점**: "무엇이 바뀌었는지" 중심으로 작성
3. **자연스러운 문장**: 커밋 메시지를 고객 언어로 변환
   - `feat(auth): 소셜 로그인 추가` → "소셜 로그인 기능이 추가되었습니다"
   - `fix: null 참조 오류` → "특정 상황에서 발생하던 오류가 수정되었습니다"
4. **Breaking Changes**: 영향과 마이그레이션 방법 명시
5. **scope 활용**: scope가 있으면 관련 기능/모듈 명시
6. **scope 기준 정렬**: 각 섹션 내 항목은 scope(볼드 표시된 모듈명) 기준으로 알파벳/가나다 순 정렬

**Overview(총평) 작성 규칙:**

이번 릴리스의 전체적인 방향과 주요 변경사항을 요약합니다.

### 토픽이 지정된 경우

**구조:** 토픽 중심 단락 + 기타 변경사항 단락

1. **첫 번째 단락 (토픽 중심):**
   - 토픽과 관련된 커밋들을 수집된 git log에서 찾아 분석
   - 토픽 관련 변경사항을 2~3문장으로 고객 친화적으로 서술
   - 토픽이 릴리스에서 어떤 가치를 제공하는지 강조

2. **두 번째 단락 (기타 변경사항):**
   - 토픽 외의 주요 변경사항을 1~2문장으로 요약
   - "또한", "이 외에도" 등의 연결어로 시작
   - 토픽 관련 커밋을 제외한 나머지 feat, fix, perf 커밋 기반

**토픽 관련 커밋 식별 방법:**
- scope가 토픽과 일치하는 커밋 (예: 토픽이 "VSCode"면 `feat(vscode):` 커밋)
- 커밋 메시지에 토픽 키워드가 포함된 커밋
- 대소문자 구분 없이 매칭

**예시 (토픽: "VSCode 개발 환경"):**
```
이번 릴리스에서는 VSCode 개발 환경 지원이 대폭 강화되었습니다. 복수 프로젝트 동시 실행, 단축키 설정, 프로젝트 자동 추가/제거 스크립트 등 개발 생산성을 높이는 기능들이 추가되었습니다.

또한 Observability 기능이 새롭게 추가되어 OpenTelemetry와 Serilog를 통한 애플리케이션 모니터링이 가능해졌으며, 여러 빌드 관련 버그가 수정되었습니다.
```

### 토픽이 지정되지 않은 경우

**구조:** 전체 변경사항 요약 (2~3문장)

1. **핵심 변경 요약**: 가장 중요한 변경사항(새 기능, 주요 수정)을 먼저 언급
2. **영향받는 영역**: 변경이 집중된 모듈/기능 영역 언급
3. **고객 가치 중심**: 기술적 세부사항보다 사용자에게 주는 이점 강조

**예시:**
```
이번 릴리스에서는 Observability 기능이 새롭게 추가되어 OpenTelemetry와 Serilog를 통한 애플리케이션 모니터링이 가능해졌습니다. VSCode 개발 환경 지원이 대폭 강화되어 복수 프로젝트 동시 실행, 단축키 설정, 자동화 스크립트 등 개발 생산성을 높이는 기능들이 추가되었습니다. 또한 빌드 시스템과 NuGet 패키지 생성 기능이 개선되었습니다.
```

**릴리스 노트 구조:**

```markdown
# Release Notes - {version}

**Release Date**: {오늘 날짜, yyyy-MM-dd 형식}

## Overview

{이번 릴리스에 대한 총평 - 2~3문장의 고객 친화적 서술}

---

## Breaking Changes

> 이 버전에서는 다음과 같은 호환성을 깨는 변경이 있습니다.

- **{scope}**: {설명}

---

## New Features

- **{scope}**: {고객 친화적 설명}
<!-- scope 기준 알파벳/가나다 순 정렬 -->

## Bug Fixes

- **{scope}**: {고객 친화적 설명}
<!-- scope 기준 알파벳/가나다 순 정렬 -->

## Performance Improvements

- **{scope}**: {고객 친화적 설명}
<!-- scope 기준 알파벳/가나다 순 정렬 -->

---

**Full Changelog**: {이전태그}...{현재버전}
```

**섹션 표시 규칙:**
- 해당 유형의 커밋이 없으면 섹션 자체를 생략
- Breaking Changes가 없으면 해당 섹션 생략
- 최소 하나의 섹션이 있어야 릴리스 노트 생성

**항목 정렬 규칙:**
- 각 섹션 내 항목은 `**{scope}**:` 형식의 scope 기준으로 정렬
- 영문 scope는 알파벳 순(A-Z), 한글 scope는 가나다 순
- scope가 없는 항목은 맨 마지막에 배치
- 동일 scope 내에서는 커밋 순서 유지

### 7. 파일 저장

```
출력 폴더: .release-notes/
파일명: RELEASE-{version}.md
예: .release-notes/RELEASE-v1.3.0.md
```

- `.release-notes` 폴더가 없으면 생성
- 동일 버전 파일이 있으면 덮어쓰기

### 8. 결과 출력

```
릴리스 노트 생성 완료

버전: {version}
범위: {이전태그}..HEAD
기간: {첫 커밋 날짜} ~ {마지막 커밋 날짜}

분석 결과:
  - Breaking Changes: N개
  - New Features: N개
  - Bug Fixes: N개
  - Performance: N개

생성 파일:
  .release-notes/RELEASE-{version}.md
```

## 오류 처리

### 태그가 없는 경우

```
태그가 없습니다. 전체 커밋을 대상으로 릴리스 노트를 생성합니다.
기준 버전: v1.0.0
```

### 릴리스 노트에 포함할 커밋이 없는 경우

```
릴리스 노트에 포함할 변경사항이 없습니다.

분석 결과:
  - 총 커밋: N개
  - 포함 대상 (feat, fix, perf): 0개
  - 제외됨 (refactor, test, ci 등): N개

feat, fix, perf 타입의 커밋이 있을 때 다시 실행하세요.
```

## 주의사항

1. **버전 형식**: `v` 접두사 포함 (예: v1.2.0)
2. **Conventional Commits 필수**: 커밋이 형식을 따르지 않으면 분류에서 제외
3. **머지 커밋 제외**: `--no-merges` 옵션으로 머지 커밋은 제외
4. **동일 버전 덮어쓰기**: 같은 버전의 릴리스 노트가 있으면 덮어씀
