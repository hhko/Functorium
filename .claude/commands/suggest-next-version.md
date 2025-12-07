---
title: SUGGEST-NEXT-VERSION
description: MinVer 버전 관리를 위한 다음 태그 버전을 제안합니다.
argument-hint: "[prerelease]를 전달하면 프리릴리스 버전(alpha/beta/rc)을 제안합니다"
---

# 다음 버전 태그 제안

MinVer 패키지 기반의 Semantic Versioning에 따라 다음 릴리스 버전 태그를 제안합니다.

## 프리릴리스 파라미터 (`$ARGUMENTS`)

**프리릴리스 타입이 지정된 경우:** $ARGUMENTS

프리릴리스 파라미터를 사용하면 정식 버전 대신 프리릴리스 버전을 제안합니다.

**지원되는 프리릴리스 타입:**
- `alpha`: 알파 버전 (초기 개발 단계)
- `beta`: 베타 버전 (기능 완료, 테스트 단계)
- `rc`: Release Candidate (출시 후보)

**사용 예시:**
```
/suggest-next-version          # 정식 버전 제안 (v1.3.0)
/suggest-next-version alpha    # 알파 버전 제안 (v1.3.0-alpha.0)
/suggest-next-version beta     # 베타 버전 제안 (v1.3.0-beta.0)
/suggest-next-version rc       # RC 버전 제안 (v1.3.0-rc.0)
```

## 프로젝트 MinVer 설정

이 프로젝트의 MinVer 설정:
- **태그 접두사**: `v` (예: v1.0.0)
- **최소 버전**: 1.0
- **기본 프리릴리스**: alpha.0
- **자동 증가**: patch

## 버전 증가 규칙

Conventional Commits 타입에 따른 Semantic Versioning 적용:

| 커밋 타입 | 버전 증가 | 설명 |
|-----------|-----------|------|
| `feat!`, `fix!`, `BREAKING CHANGE` | **Major** (x.0.0) | 호환성을 깨는 변경 |
| `feat` | **Minor** (1.x.0) | 새로운 기능 추가 |
| `fix`, `perf` | **Patch** (1.0.x) | 버그 수정, 성능 개선 |
| `docs`, `style`, `refactor`, `test`, `build`, `ci`, `chore` | 버전 증가 없음 | 릴리스 불필요 변경 |

**우선순위:** Major > Minor > Patch (가장 높은 수준의 변경을 기준으로 버전 결정)

## 실행 절차

다음 절차에 따라 버전을 분석하고 제안합니다:

### 0. 파라미터 유효성 검사

`$ARGUMENTS`가 지정된 경우, 다음 값만 허용합니다:
- `alpha`
- `beta`
- `rc`

**유효하지 않은 값이 입력된 경우:**
- 즉시 오류 메시지를 출력하고 실행을 중단합니다
- 버전 분석을 진행하지 않습니다

```
오류: 유효하지 않은 프리릴리스 타입입니다.

입력값: {입력된 값}
허용값: alpha, beta, rc

사용 예시:
  /suggest-next-version          # 정식 버전 제안
  /suggest-next-version alpha    # 알파 버전 제안
  /suggest-next-version beta     # 베타 버전 제안
  /suggest-next-version rc       # RC 버전 제안
```

### 1. 현재 버전 확인

```bash
# 최신 태그 확인
git describe --tags --abbrev=0 2>/dev/null || echo "태그 없음"

# 모든 태그 목록
git tag --list --sort=-v:refname | head -5
```

- 태그가 없으면 v1.0.0을 기준 버전으로 사용 (MinVer 최소 버전)
- 태그가 있으면 최신 태그를 기준 버전으로 사용

### 2. 커밋 히스토리 분석

```bash
# 마지막 태그 이후의 커밋 목록 (태그가 있는 경우)
git log <last-tag>..HEAD --oneline

# 태그가 없는 경우 전체 커밋
git log --oneline
```

### 3. 커밋 타입 분류

각 커밋 메시지를 분석하여 분류:

1. **Breaking Changes 확인**: "!" 접미사 또는 "BREAKING CHANGE" 푸터
2. **feat 커밋 수**: Minor 버전 증가 대상
3. **fix/perf 커밋 수**: Patch 버전 증가 대상
4. **기타 커밋**: 버전 증가 대상 아님

### 4. 버전 증가 결정

- Breaking Change가 있으면 → Major 증가
- feat 커밋이 있으면 → Minor 증가
- fix/perf 커밋만 있으면 → Patch 증가
- 그 외에는 → 버전 증가 불필요 알림

### 5. 프리릴리스 처리

`$ARGUMENTS`가 지정된 경우:
- 제안 버전에 `-{prerelease}.0` 접미사 추가
- 예: v1.3.0 → v1.3.0-alpha.0

### 6. 결과 출력

다음 형식으로 결과를 출력합니다:

```
태그 제안 결과

현재 버전: v1.2.3 (또는 "태그 없음")
제안 버전: v1.3.0

버전 증가 이유:
  - feat 커밋 N개 발견 (Minor 증가)
  - fix 커밋 N개 발견
  - Breaking Change 없음

주요 변경사항:
  - feat: 변경사항 설명
  - fix: 변경사항 설명
  ...

태그 생성 명령어:
  git tag v1.3.0
  git push origin v1.3.0
```

## 결과 형식

### 버전 증가가 필요한 경우

```
태그 제안 결과

현재 버전: {현재 태그 또는 "태그 없음"}
제안 버전: {제안 버전}

버전 증가 이유:
  - {커밋 타입별 수 및 증가 이유}

주요 변경사항:
  - {feat/fix 커밋 목록, 최대 10개}

태그 생성 명령어:
  git tag {제안 버전}
  git push origin {제안 버전}
```

### 버전 증가가 불필요한 경우

```
태그 제안 결과

현재 버전: {현재 태그}
제안 버전: 없음

분석 결과:
  - 마지막 태그 이후 {N}개 커밋 발견
  - 버전 증가가 필요한 커밋 타입 없음 (docs, style, refactor 등만 존재)

권장 사항:
  - 현재 변경사항은 버전 증가 없이 유지
  - 기능 추가(feat) 또는 버그 수정(fix) 후 다시 실행
```

## 주의사항

1. **태그는 직접 생성하지 않습니다**: 제안만 하고, 실제 태그 생성은 사용자가 결정
2. **푸시 전 확인**: 태그 생성 전 CI/CD 빌드 상태 확인 권장
3. **프리릴리스 순서**: alpha → beta → rc → 정식 버전 순서로 진행

## 참고 자료

- [Semantic Versioning 2.0.0](https://semver.org/)
- [MinVer GitHub](https://github.com/adamralph/minver)
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/)
