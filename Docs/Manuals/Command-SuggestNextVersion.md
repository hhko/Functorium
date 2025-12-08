# Claude Suggest Next Version 명령 매뉴얼

이 문서는 MinVer 패키지 기반의 버전 관리를 위한 다음 태그 버전 제안 명령을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [프로젝트 MinVer 설정](#프로젝트-minver-설정)
- [버전 증가 규칙](#버전-증가-규칙)
- [프리릴리스 파라미터](#프리릴리스-파라미터)
- [실행 절차](#실행-절차)
- [출력 형식](#출력-형식)
- [사용 예시](#사용-예시)
- [FAQ](#faq)

<br/>

## 개요

`/suggest-next-version` 명령은 Conventional Commits 히스토리를 분석하여 Semantic Versioning에 따른 다음 릴리스 버전 태그를 제안합니다.

### 주요 기능

| 기능 | 설명 |
|------|------|
| 최신 태그 확인 | `git describe` 및 `git tag`로 현재 버전 파악 |
| 커밋 히스토리 분석 | 마지막 태그 이후의 커밋 분석 |
| 버전 증가 판단 | Conventional Commits 타입에 따른 SemVer 적용 |
| 태그 제안 | 다음 버전 태그 및 git 명령어 제시 |
| 프리릴리스 지원 | alpha, beta, rc 버전 태그 옵션 |

<br/>

## 요약

### 주요 명령

**기본 사용:**
```bash
/suggest-next-version          # 정식 버전 제안
/suggest-next-version alpha    # 알파 버전 제안
/suggest-next-version beta     # 베타 버전 제안
/suggest-next-version rc       # RC 버전 제안
```

### 버전 증가 규칙

| 커밋 타입 | 버전 증가 | 예시 |
|-----------|-----------|------|
| `feat!`, `BREAKING CHANGE` | Major | v1.0.0 → v2.0.0 |
| `feat` | Minor | v1.0.0 → v1.1.0 |
| `fix`, `perf` | Patch | v1.0.0 → v1.0.1 |

### 출력 예시

```
태그 제안 결과

현재 버전: v1.2.3
제안 버전: v1.3.0

버전 증가 이유:
  - feat 커밋 3개 발견 (Minor 증가)
  - fix 커밋 5개 발견

태그 생성 명령어:
  git tag v1.3.0
  git push origin v1.3.0
```

<br/>

## 프로젝트 MinVer 설정

이 프로젝트의 `Directory.Build.props`에 정의된 MinVer 설정:

| 설정 | 값 | 설명 |
|------|-----|------|
| `MinVerTagPrefix` | `v` | 태그 접두사 (예: v1.0.0) |
| `MinVerMinimumMajorMinor` | `1.0` | 최소 버전 |
| `MinVerDefaultPreReleaseIdentifiers` | `alpha.0` | 기본 프리릴리스 식별자 |
| `MinVerAutoIncrement` | `patch` | 자동 증가 대상 |

### MinVer 버전 할당

MinVer는 git 태그를 기반으로 자동으로 버전을 할당합니다:

```
v1.2.3                  → 1.2.3 (정식 버전)
v1.2.3-alpha.0          → 1.2.3-alpha.0 (프리릴리스)
v1.2.3-alpha.0+5        → 1.2.3-alpha.0 + 5 커밋 후
```

<br/>

## 버전 증가 규칙

### Semantic Versioning + Conventional Commits

| 커밋 타입 | 버전 증가 | 설명 |
|-----------|-----------|------|
| `feat!`, `fix!`, `BREAKING CHANGE`, `feat` | **Minor** (1.X.0) | 호환성을 깨는 변경, 새로운 기능 추가 |
| `fix`, `perf` | **Patch** (1.0.X) | 버그 수정, 성능 개선 |
| `docs`, `style`, `refactor`, `test`, `build`, `ci`, `chore` | 없음 | 버전 증가 불필요 |

### 우선순위

버전 증가 결정 시 가장 높은 수준의 변경을 기준으로 합니다:

```
Major > Minor > Patch
```

예시:
- feat 2개 + fix 5개 → Minor 증가
- feat! 1개 + feat 10개 → Major 증가

<br/>

## 프리릴리스 파라미터

### 지원되는 프리릴리스 타입

| 타입 | 설명 | 버전 예시 |
|------|------|----------|
| `alpha` | 알파 버전 (초기 개발 단계) | v1.3.0-alpha.0 |
| `beta` | 베타 버전 (기능 완료, 테스트 단계) | v1.3.0-beta.0 |
| `rc` | Release Candidate (출시 후보) | v1.3.0-rc.0 |

### 프리릴리스 순서

일반적인 릴리스 프로세스:

```
alpha → beta → rc → 정식 버전
```

예시:
```
v1.3.0-alpha.0
v1.3.0-alpha.1
v1.3.0-beta.0
v1.3.0-rc.0
v1.3.0-rc.1
v1.3.0
```

<br/>

## 실행 절차

### 1. 현재 버전 확인

```bash
# 최신 태그 확인
git describe --tags --abbrev=0 2>/dev/null

# 모든 태그 목록
git tag --list --sort=-v:refname | head -5
```

### 2. 커밋 히스토리 분석

```bash
# 마지막 태그 이후의 커밋 목록
git log <last-tag>..HEAD --oneline

# 태그가 없는 경우
git log --oneline
```

### 3. 커밋 타입 분류

각 커밋 메시지를 분석하여 분류:

1. **Breaking Changes 확인**: `!` 접미사 또는 `BREAKING CHANGE` 푸터
2. **feat 커밋 수**: Minor 버전 증가 대상
3. **fix/perf 커밋 수**: Patch 버전 증가 대상

### 4. 버전 증가 결정

| 조건 | 결과 |
|------|------|
| Breaking Change가 있음 | Major 증가 |
| feat 커밋이 있음 | Minor 증가 |
| fix/perf 커밋만 있음 | Patch 증가 |
| 그 외 | 버전 증가 불필요 |

### 5. 결과 출력

제안된 버전과 git 명령어를 출력합니다.

<br/>

## 출력 형식

### 버전 증가가 필요한 경우

```
태그 제안 결과

현재 버전: v1.2.3
제안 버전: v1.3.0

버전 증가 이유:
  - feat 커밋 3개 발견 (Minor 증가)
  - fix 커밋 5개 발견
  - Breaking Change 없음

주요 변경사항:
  - feat: 사용자 로그인 기능 추가
  - feat(api): REST API 엔드포인트 구현
  - fix: null 참조 예외 처리

태그 생성 명령어:
  git tag v1.3.0
  git push origin v1.3.0
```

### 프리릴리스 버전인 경우

```
태그 제안 결과

현재 버전: v1.2.3
제안 버전: v1.3.0-alpha.0

버전 증가 이유:
  - feat 커밋 2개 발견 (Minor 증가)
  - 프리릴리스 타입: alpha

태그 생성 명령어:
  git tag v1.3.0-alpha.0
  git push origin v1.3.0-alpha.0
```

### 버전 증가가 불필요한 경우

```
태그 제안 결과

현재 버전: v1.2.3
제안 버전: 없음

분석 결과:
  - 마지막 태그 이후 5개 커밋 발견
  - 버전 증가가 필요한 커밋 타입 없음 (docs, style, refactor 등만 존재)

권장 사항:
  - 현재 변경사항은 버전 증가 없이 유지
  - 기능 추가(feat) 또는 버그 수정(fix) 후 다시 실행
```

### 태그가 없는 경우 (첫 릴리스)

```
태그 제안 결과

현재 버전: 태그 없음
제안 버전: v1.0.0

버전 증가 이유:
  - 첫 번째 릴리스
  - MinVer 최소 버전 (1.0) 적용

주요 변경사항:
  - feat: 초기 프로젝트 설정
  - feat: 핵심 기능 구현

태그 생성 명령어:
  git tag v1.0.0
  git push origin v1.0.0
```

<br/>

## 사용 예시

### 정식 버전 제안

```bash
# 커밋 히스토리 분석 후 정식 버전 제안
/suggest-next-version
```

### 알파 버전 제안

```bash
# 초기 개발 단계에서 테스트용 버전
/suggest-next-version alpha
```

### 베타 버전 제안

```bash
# 기능 완료 후 테스트 단계
/suggest-next-version beta
```

### Release Candidate 제안

```bash
# 정식 출시 전 최종 검증
/suggest-next-version rc
```

<br/>

## FAQ

### Q1. MinVer는 무엇인가요?

MinVer는 Git 태그를 기반으로 .NET 프로젝트의 버전을 자동으로 관리하는 NuGet 패키지입니다.

| 특징 | 설명 |
|------|------|
| 태그 기반 | git 태그에서 버전 정보를 읽음 |
| 자동 증가 | 태그 후 커밋 수를 자동 추가 |
| SemVer 호환 | Semantic Versioning 2.0.0 준수 |

### Q2. 태그를 직접 생성하나요?

**아니요.** `/suggest-next-version` 명령은 제안만 합니다. 실제 태그 생성은 사용자가 제시된 명령어를 실행해야 합니다.

```bash
# 명령어가 제안한 후, 사용자가 직접 실행
git tag v1.3.0
git push origin v1.3.0
```

### Q3. 프리릴리스 버전은 언제 사용하나요?

| 단계 | 프리릴리스 타입 | 설명 |
|------|----------------|------|
| 초기 개발 | `alpha` | 기능이 불완전하거나 불안정한 상태 |
| 기능 완료 | `beta` | 기능은 완료되었으나 버그가 있을 수 있음 |
| 출시 직전 | `rc` | 정식 출시 후보, 심각한 버그만 수정 |

### Q4. docs나 refactor 커밋만 있으면 버전을 올리지 않나요?

맞습니다. Semantic Versioning에 따르면 사용자에게 영향을 주는 변경사항(기능 추가, 버그 수정)이 있을 때만 버전을 올립니다.

```
docs, style, refactor, test, build, ci, chore
→ 버전 증가 없음
```

### Q5. Breaking Change는 어떻게 감지하나요?

두 가지 방법으로 감지합니다:

1. **타입 뒤 느낌표**: `feat!`, `fix!`, `refactor!`
2. **푸터의 BREAKING CHANGE**: 커밋 메시지 푸터에 `BREAKING CHANGE:` 포함

```
feat!: API 응답 형식 변경

BREAKING CHANGE: 응답이 배열에서 객체로 변경됨
```

### Q6. 태그 접두사 'v'는 필수인가요?

이 프로젝트에서는 `MinVerTagPrefix=v`로 설정되어 있으므로 필수입니다.

```
v1.0.0   ← 올바름
1.0.0    ← MinVer가 인식하지 못함
```

### Q7. 여러 사람이 동시에 작업할 때 충돌은 어떻게 하나요?

태그는 한 번에 한 사람만 생성해야 합니다. 릴리스 담당자를 지정하거나 CI/CD 파이프라인에서 자동으로 태그를 생성하는 것을 권장합니다.

### Q8. 태그를 잘못 생성했을 때 어떻게 수정하나요?

```bash
# 로컬 태그 삭제
git tag -d v1.3.0

# 원격 태그 삭제 (주의: 다른 사람이 이미 가져갔을 수 있음)
git push origin --delete v1.3.0

# 올바른 태그 생성
git tag v1.3.0
git push origin v1.3.0
```

> **주의**: 이미 배포된 태그를 삭제하면 다른 개발자나 CI/CD에 영향을 줄 수 있습니다.

### Q9. CI/CD에서 자동으로 버전을 올릴 수 있나요?

네, GitHub Actions 등에서 조건부로 태그를 생성할 수 있습니다:

```yaml
- name: Create tag on main
  if: github.ref == 'refs/heads/main'
  run: |
    git tag v${{ steps.version.outputs.next }}
    git push origin v${{ steps.version.outputs.next }}
```

<br/>

## 참고 문서

- [MinVer GitHub](https://github.com/adamralph/minver) - MinVer 공식 저장소
- [Semantic Versioning 2.0.0](https://semver.org/) - SemVer 공식 문서
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/) - Conventional Commits 공식 문서
- [Claude Commit 명령 매뉴얼](./Claude-Commit-Command.md) - 커밋 명령 사용법
