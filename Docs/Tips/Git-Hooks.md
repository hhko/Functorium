# Git Hooks

이 문서는 Git Hooks의 개념과 프로젝트에서 활용하는 방법을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [프로젝트 설정](#프로젝트-설정)
- [Hook 종류](#hook-종류)
- [Hook 작성법](#hook-작성법)
- [프로젝트 Hook 설명](#프로젝트-hook-설명)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### Git Hooks란?

Git Hooks는 Git 작업(커밋, 푸시, 머지 등) 전후에 자동으로 실행되는 스크립트입니다. 코드 품질 검사, 커밋 메시지 검증, 자동 포맷팅 등에 활용됩니다.

### 동작 원리

```
Git 명령 실행 → Hook 스크립트 실행 → 성공(exit 0) → Git 작업 계속
                                   → 실패(exit 1) → Git 작업 중단
```

### 활용 사례

| 용도 | Hook | 설명 |
|------|------|------|
| 커밋 메시지 검증 | `commit-msg` | Conventional Commits 형식 검사 |
| 코드 포맷팅 | `pre-commit` | 커밋 전 자동 포맷팅 |
| 테스트 실행 | `pre-push` | 푸시 전 테스트 실행 |
| 브랜치 보호 | `pre-push` | main 브랜치 직접 푸시 방지 |

<br/>

## 요약

### 주요 명령

```bash
# Hook 경로 설정 (프로젝트별 설정)
git config core.hooksPath .githooks

# 전역 설정 (모든 프로젝트)
git config --global core.hooksPath ~/.githooks

# 현재 설정 확인
git config --get core.hooksPath

# 설정 제거
git config --unset core.hooksPath
```

### 주요 절차

**1. 새 프로젝트에서 Hook 활성화:**
```bash
# 1. 프로젝트 clone
git clone <repository-url>
cd <project>

# 2. Hook 경로 설정
git config core.hooksPath .githooks

# 3. 확인
git config --get core.hooksPath
```

**2. 새 Hook 추가:**
```bash
# 1. Hook 파일 생성
touch .githooks/<hook-name>

# 2. 실행 권한 부여 (Linux/Mac)
chmod +x .githooks/<hook-name>

# 3. 스크립트 작성

# 4. 테스트
git commit -m "test: hook 테스트"
```

### 주요 개념

**1. Hook 위치**

| 위치 | 설명 | 공유 여부 |
|------|------|----------|
| `.git/hooks/` | 기본 위치 | 공유 안됨 |
| `.githooks/` | 프로젝트 폴더 | Git으로 공유 |

**2. 종료 코드**

| 코드 | 의미 | Git 동작 |
|------|------|----------|
| `exit 0` | 성공 | 작업 계속 |
| `exit 1` | 실패 | 작업 중단 |

**3. Hook 실행 순서 (커밋)**
```
pre-commit → prepare-commit-msg → commit-msg → post-commit
```

<br/>

## 프로젝트 설정

### 폴더 구조

```
프로젝트/
├── .git/
│   └── hooks/          # 기본 위치 (공유 안됨)
├── .githooks/          # 프로젝트 Hook (Git으로 공유)
│   ├── commit-msg      # 커밋 메시지 처리
│   ├── pre-commit      # 커밋 전 검사
│   └── pre-push        # 푸시 전 검사
└── ...
```

### 초기 설정

프로젝트를 clone한 후 한 번만 실행:

```bash
git config core.hooksPath .githooks
```

### 설정 확인

```bash
# Hook 경로 확인
git config --get core.hooksPath
# 출력: .githooks

# Hook 파일 목록
ls -la .githooks/
```

<br/>

## Hook 종류

### 클라이언트 Hook

| Hook | 실행 시점 | 용도 |
|------|----------|------|
| `pre-commit` | 커밋 메시지 입력 전 | 코드 검사, 포맷팅 |
| `prepare-commit-msg` | 커밋 메시지 편집기 실행 전 | 메시지 템플릿 |
| `commit-msg` | 커밋 메시지 입력 후 | 메시지 검증/수정 |
| `post-commit` | 커밋 완료 후 | 알림, 로깅 |
| `pre-push` | 푸시 전 | 테스트, 브랜치 검사 |
| `post-checkout` | 체크아웃 후 | 환경 설정 |
| `post-merge` | 머지 후 | 의존성 설치 |

### 서버 Hook

| Hook | 실행 시점 | 용도 |
|------|----------|------|
| `pre-receive` | 푸시 수신 전 | 정책 검사 |
| `update` | 각 브랜치 업데이트 전 | 브랜치별 검사 |
| `post-receive` | 푸시 수신 후 | CI/CD 트리거 |

<br/>

## Hook 작성법

### 기본 구조

```bash
#!/usr/bin/env bash
#
# Hook 설명
# Setup: git config core.hooksPath .githooks
#

# 스크립트 내용

exit 0  # 성공
# exit 1  # 실패 (Git 작업 중단)
```

### commit-msg Hook

커밋 메시지를 검증하거나 수정합니다.

```bash
#!/usr/bin/env bash
#
# Conventional Commits 형식 검증
#

COMMIT_MSG_FILE="$1"
COMMIT_MSG=$(cat "$COMMIT_MSG_FILE")

# 패턴: type(scope): description
PATTERN="^(feat|fix|docs|style|refactor|perf|test|build|ci|chore)(\(.+\))?: .+"

if ! echo "$COMMIT_MSG" | grep -qE "$PATTERN"; then
    echo "ERROR: 커밋 메시지가 Conventional Commits 형식이 아닙니다."
    echo ""
    echo "형식: <type>(<scope>): <description>"
    echo "예시: feat(auth): 로그인 기능 추가"
    echo ""
    echo "타입: feat, fix, docs, style, refactor, perf, test, build, ci, chore"
    exit 1
fi

exit 0
```

### pre-commit Hook

커밋 전 코드를 검사합니다.

```bash
#!/usr/bin/env bash
#
# 커밋 전 코드 검사
#

# 스테이징된 파일 목록
STAGED_FILES=$(git diff --cached --name-only --diff-filter=ACM)

# .cs 파일 포맷 검사
CS_FILES=$(echo "$STAGED_FILES" | grep '\.cs$')
if [ -n "$CS_FILES" ]; then
    echo "C# 파일 포맷 검사 중..."
    dotnet format --verify-no-changes
    if [ $? -ne 0 ]; then
        echo "ERROR: 코드 포맷이 올바르지 않습니다."
        echo "실행: dotnet format"
        exit 1
    fi
fi

exit 0
```

### pre-push Hook

푸시 전 검사를 수행합니다.

```bash
#!/usr/bin/env bash
#
# main 브랜치 직접 푸시 방지
#

BRANCH=$(git rev-parse --abbrev-ref HEAD)
PROTECTED_BRANCHES="^(main|master)$"

if echo "$BRANCH" | grep -qE "$PROTECTED_BRANCHES"; then
    echo "ERROR: $BRANCH 브랜치에 직접 푸시할 수 없습니다."
    echo "PR을 통해 머지하세요."
    exit 1
fi

exit 0
```

<br/>

## 프로젝트 Hook 설명

### commit-msg

Claude Code가 생성하는 텍스트를 자동으로 제거합니다.

**파일:** `.githooks/commit-msg`

```bash
#!/usr/bin/env bash
#
# Git hook to remove Claude/AI generated text from commit messages
#

COMMIT_MSG_FILE="$1"

# Claude 관련 텍스트 제거
sed -i.bak \
    -e '/Generated with.*Claude/d' \
    -e '/Co-Authored-By:.*Claude/d' \
    -e '/Claude Code/d' \
    "$COMMIT_MSG_FILE"

# 후행 빈 줄 제거
sed -i.bak -e :a -e '/^\s*$/{$d;N;ba' -e '}' "$COMMIT_MSG_FILE" 2>/dev/null || true

# 백업 파일 정리
rm -f "$COMMIT_MSG_FILE.bak"

exit 0
```

**동작:**
1. 커밋 메시지에서 Claude 관련 텍스트 패턴 제거
2. 후행 빈 줄 정리
3. 백업 파일 삭제

**제거되는 패턴:**
- `Generated with [Claude Code]`
- `Co-Authored-By: Claude`
- `Claude Code` 포함 줄

<br/>

## 트러블슈팅

### Hook이 실행되지 않을 때

**원인:** `core.hooksPath` 설정이 안 됨

**해결:**
```bash
# 설정 확인
git config --get core.hooksPath

# 설정이 없으면 추가
git config core.hooksPath .githooks
```

### Permission denied 오류 (Linux/Mac)

**원인:** 실행 권한이 없음

**해결:**
```bash
chmod +x .githooks/*
```

### Windows에서 Hook이 동작하지 않을 때

**원인:** Git Bash가 아닌 CMD/PowerShell 사용

**해결:**
- Git Bash 사용
- 또는 shebang을 `#!/usr/bin/env bash`로 설정

### sed 명령어 호환성 문제

**원인:** GNU sed와 BSD sed의 차이

**해결:**
```bash
# GNU sed (Linux)
sed -i 's/pattern/replacement/' file

# BSD sed (Mac) - 백업 확장자 필요
sed -i '' 's/pattern/replacement/' file

# 호환 방식 - 백업 후 삭제
sed -i.bak 's/pattern/replacement/' file
rm -f file.bak
```

### Hook을 일시적으로 건너뛰기

**방법:**
```bash
# --no-verify 옵션 사용
git commit --no-verify -m "긴급 수정"

# 또는 환경 변수 설정
SKIP_HOOKS=1 git commit -m "긴급 수정"
```

> **주의:** 긴급 상황에서만 사용하세요.

<br/>

## FAQ

### Q1. .git/hooks와 .githooks의 차이점은 무엇인가요?

| 항목 | `.git/hooks/` | `.githooks/` |
|------|---------------|--------------|
| 위치 | Git 내부 폴더 | 프로젝트 루트 |
| Git 추적 | 추적 안됨 | 추적됨 (공유 가능) |
| 기본 설정 | 기본값 | `core.hooksPath` 설정 필요 |
| 용도 | 개인 설정 | 팀 공유 |

### Q2. Hook 설정을 모든 팀원이 공유하려면 어떻게 하나요?

**A:** `.githooks/` 폴더를 사용하고, README에 설정 방법을 안내합니다:

```bash
# 프로젝트 clone 후 실행
git config core.hooksPath .githooks
```

### Q3. 특정 Hook만 건너뛰려면 어떻게 하나요?

**A:** `--no-verify` 옵션을 사용합니다:

```bash
# pre-commit, commit-msg Hook 건너뛰기
git commit --no-verify -m "메시지"

# pre-push Hook 건너뛰기
git push --no-verify
```

### Q4. Hook에서 스테이징된 파일 목록을 가져오려면?

**A:** `git diff --cached` 명령을 사용합니다:

```bash
# 스테이징된 파일 목록
git diff --cached --name-only

# 추가/수정된 파일만
git diff --cached --name-only --diff-filter=ACM

# 특정 확장자 필터
git diff --cached --name-only | grep '\.cs$'
```

### Q5. Hook 스크립트를 디버깅하려면?

**A:** `set -x`로 디버그 모드를 활성화합니다:

```bash
#!/usr/bin/env bash
set -x  # 디버그 모드 활성화

# 스크립트 내용...

set +x  # 디버그 모드 비활성화
```

또는 수동 실행:

```bash
# commit-msg Hook 테스트
echo "test: 테스트 메시지" > /tmp/commit-msg
.githooks/commit-msg /tmp/commit-msg
cat /tmp/commit-msg
```

### Q6. PowerShell로 Hook을 작성할 수 있나요?

**A:** Windows에서 Git은 기본적으로 Git Bash를 통해 Hook을 실행합니다. PowerShell Hook을 사용하려면 래퍼가 필요합니다:

```bash
#!/usr/bin/env bash
# PowerShell 스크립트 호출
powershell.exe -ExecutionPolicy Bypass -File ".githooks/commit-msg.ps1" "$1"
```

Bash 스크립트를 권장합니다 (크로스 플랫폼 호환성).

### Q7. Hook에서 사용자 입력을 받을 수 있나요?

**A:** `pre-commit`과 `commit-msg` Hook에서는 stdin이 `/dev/null`로 리다이렉트되어 있습니다. 터미널에서 직접 읽으려면:

```bash
#!/usr/bin/env bash
exec < /dev/tty

read -p "계속하시겠습니까? (y/n) " answer
if [ "$answer" != "y" ]; then
    exit 1
fi
```

### Q8. 여러 Hook을 순차적으로 실행하려면?

**A:** 메인 Hook에서 다른 스크립트를 호출합니다:

```bash
#!/usr/bin/env bash
# .githooks/pre-commit

# 포맷 검사
.githooks/scripts/check-format.sh || exit 1

# 린트 검사
.githooks/scripts/check-lint.sh || exit 1

# 테스트
.githooks/scripts/run-tests.sh || exit 1

exit 0
```

### Q9. Hook 실행 시간을 측정하려면?

**A:** `time` 명령 또는 스크립트 내에서 측정합니다:

```bash
#!/usr/bin/env bash
START=$(date +%s)

# Hook 로직...

END=$(date +%s)
echo "Hook 실행 시간: $((END-START))초"
```

### Q10. CI 환경에서 Hook을 비활성화하려면?

**A:** CI 환경 변수를 확인합니다:

```bash
#!/usr/bin/env bash

# CI 환경에서는 건너뛰기
if [ -n "$CI" ] || [ -n "$GITHUB_ACTIONS" ]; then
    exit 0
fi

# 일반 환경에서 Hook 로직 실행
# ...
```

<br/>

## 참고 문서

- [Git Hooks 공식 문서](https://git-scm.com/docs/githooks)
- [Git 커밋 규칙](./../.claude/commands/commit.md)
