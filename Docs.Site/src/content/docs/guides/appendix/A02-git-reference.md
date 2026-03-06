---
title: "Git 명령어 참조"
---

이 문서는 Git의 주요 명령어, 사용법, Git Hooks 설정을 설명합니다.

## 요약

### 주요 명령

**상태 확인:**
```bash
git status
git diff
git log --oneline -5
```

**스테이징 및 커밋:**
```bash
git add <파일>
git add .
git commit -m "feat: 새 기능 추가"
```

**브랜치:**
```bash
git switch -c <브랜치>
git merge <브랜치>
git branch -d <브랜치>
```

**원격 동기화:**
```bash
git pull
git push
git fetch
```

**태그:**
```bash
git tag --list
git tag -l -n1
git tag v1.0.0
git tag --sort=-v:refname
```

**커밋 합치기:**
```bash
git rebase -i HEAD~3
git reset --soft HEAD~3
```

**임시 저장:**
```bash
git stash
git stash pop
git stash list
```

**긴급 상황:**
```bash
git restore <파일>                    # 변경 취소
git restore --staged <파일>           # 스테이징 취소
git commit --amend -m "새 메시지"     # 커밋 메시지 수정
git reset --soft HEAD~1               # 커밋 취소
git merge --abort                     # 병합 취소
```

### 안전 수칙

- 변경 전 `git status`로 현재 상태 확인
- 커밋 전 `git diff`로 변경 내용 검토
- 작고 자주 커밋 (논리적 단위)
- `--force` 금지 (개인 브랜치만 `--force-with-lease` 허용)
- `main` 직접 커밋 금지




요약에서 주요 Git 명령어와 안전 수칙을 확인했습니다. 이제 각 작업 흐름별로 명령어를 상세히 살펴봅니다.

## 변경사항 확인

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git status` | 작업 디렉토리 상태 확인 |
| `git diff` | 스테이징되지 않은 변경 내용 확인 |
| `git diff --staged` | 스테이징된 변경 내용 확인 |
| `git log --oneline` | 커밋 이력 한 줄로 확인 |
| `git log --oneline -5` | 최근 5개 커밋 이력 확인 |
| `git log --graph` | 브랜치 그래프와 함께 이력 확인 |
| `git log --no-merges` | 머지 커밋 제외하고 확인 |

### 커스텀 포맷

| 명령어 | 설명 |
|--------|------|
| `git log --format="%h"` | 짧은 커밋 해시만 출력 |
| `git log --format="%an"` | 작성자 이름만 출력 |
| `git log --format="%s"` | 커밋 제목만 출력 |
| `git log --format="%h\|%an\|%s"` | 해시, 작성자, 제목 출력 |

**주요 포맷 지정자:** `%h` (짧은 해시), `%H` (전체 해시), `%an` (작성자), `%ae` (이메일), `%ai` (날짜 ISO 8601), `%s` (제목), `%b` (본문), `%D` (ref 이름)

### 범위 지정

| 명령어 | 설명 |
|--------|------|
| `git log v1.0.0..HEAD` | 태그 이후 커밋 확인 |
| `git log v1.0.0..v1.1.0` | 두 태그 사이 커밋 확인 |
| `git log main..feature` | main 이후 feature 브랜치 커밋 확인 |




## 스테이징

| 명령어 | 설명 |
|--------|------|
| `git add <파일>` | 특정 파일 스테이징 |
| `git add .` | 현재 디렉토리의 모든 변경사항 스테이징 |
| `git add -A` | 저장소 전체의 모든 변경사항 스테이징 |
| `git add -p` | 대화형 스테이징 (변경사항 선택) |




## 커밋

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git commit -m "메시지"` | 메시지와 함께 커밋 |
| `git commit` | 에디터로 메시지 작성 후 커밋 |
| `git commit -am "메시지"` | 추적 중인 파일 스테이징 및 커밋 |
| `git commit --amend` | 마지막 커밋 수정 |
| `git commit --amend -m "메시지"` | 마지막 커밋 메시지 수정 |

### 커밋 합치기 (Squash)

**방법 1: Interactive Rebase**

```bash
# 최근 3개 커밋을 하나로 합치기
git rebase -i HEAD~3

# 에디터에서 첫 번째를 제외한 나머지를 squash(또는 s)로 변경
# pick abc1234 첫 번째 커밋
# squash def5678 두 번째 커밋
# squash ghi9012 세 번째 커밋
```

**방법 2: Soft Reset (더 간단)**

```bash
# 최근 3개 커밋을 취소하고 변경사항은 스테이징 유지
git reset --soft HEAD~3

# 하나의 새 커밋으로 다시 커밋
git commit -m "feat: 기능 구현 완료"
```

> **주의**: 이미 push한 커밋을 squash하면 `--force-with-lease`가 필요합니다. 공유 브랜치에서는 사용하지 마세요.




## 태그

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git tag --list` | 태그 목록 확인 |
| `git tag -l -n1` | 태그 목록과 메시지 첫 줄 확인 |
| `git tag <이름>` | 경량 태그 생성 |
| `git tag -a <이름> -m "메시지"` | 주석 태그 생성 |
| `git tag -d <이름>` | 태그 삭제 |
| `git push origin <태그>` | 태그 원격에 푸시 |
| `git push origin --tags` | 모든 태그 푸시 |
| `git push origin --delete <태그>` | 원격 태그 삭제 |

### 태그 정렬

| 명령어 | 설명 |
|--------|------|
| `git tag --sort=-v:refname` | 버전 역순 정렬 (최신 먼저) |
| `git tag --sort=v:refname` | 버전 순 정렬 |
| `git tag --sort=-creatordate` | 생성일 역순 정렬 |




## 브랜치

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git branch` | 로컬 브랜치 목록 확인 |
| `git branch -a` | 모든 브랜치 목록 확인 (원격 포함) |
| `git branch <이름>` | 새 브랜치 생성 |
| `git branch -d <이름>` | 브랜치 삭제 (병합된 경우) |
| `git branch -D <이름>` | 브랜치 강제 삭제 |
| `git branch -m <이전> <새이름>` | 브랜치 이름 변경 |

### 브랜치 전환

| 명령어 | 설명 |
|--------|------|
| `git switch <브랜치>` | 브랜치 전환 (Git 2.23+) |
| `git switch -c <이름>` | 브랜치 생성 및 전환 |
| `git checkout <브랜치>` | 브랜치 전환 (구버전) |
| `git checkout -b <이름>` | 브랜치 생성 및 전환 (구버전) |

### 브랜치 병합

| 명령어 | 설명 |
|--------|------|
| `git merge <브랜치>` | 현재 브랜치에 다른 브랜치 병합 |
| `git merge --no-ff <브랜치>` | Fast-forward 없이 병합 (병합 커밋 생성) |
| `git rebase <브랜치>` | 현재 브랜치를 다른 브랜치 위로 리베이스 |




## 원격 저장소

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git remote -v` | 원격 저장소 목록 확인 |
| `git remote add <이름> <URL>` | 원격 저장소 추가 |
| `git remote remove <이름>` | 원격 저장소 제거 |

### 동기화 명령어

| 명령어 | 설명 |
|--------|------|
| `git fetch` | 원격 변경사항 가져오기 (병합 X) |
| `git pull` | 원격 변경사항 가져오기 및 병합 |
| `git pull --rebase` | 원격 변경사항 가져오기 및 리베이스 |
| `git push` | 로컬 커밋을 원격에 푸시 |
| `git push -u origin <브랜치>` | 업스트림 설정 후 푸시 |
| `git push --force-with-lease` | 안전한 강제 푸시 |




## 릴리스 노트 및 분석

### 저장소 정보

| 명령어 | 설명 |
|--------|------|
| `git rev-parse --show-toplevel` | Git 저장소의 루트 디렉토리 경로 |
| `git rev-parse --verify <브랜치>` | 브랜치/커밋 존재 확인 |
| `git rev-list --max-parents=0 HEAD` | 초기 커밋 해시 |

### 특정 경로 분석

| 명령어 | 설명 |
|--------|------|
| `git diff --name-status <base>..<target> -- <경로>/` | 파일 변경 상태 확인 |
| `git diff --stat <base>..<target> -- <경로>/` | 변경 통계 |
| `git log --oneline --no-merges <base>..<target> -- <경로>/` | 커밋 이력 |

### 커밋 검색 및 필터링

| 명령어 | 설명 |
|--------|------|
| `git log --grep="pattern"` | 커밋 메시지에서 패턴 검색 |
| `git log --author="이름"` | 특정 작성자의 커밋만 표시 |
| `git log --since="2024-01-01"` | 특정 날짜 이후 커밋 |
| `git shortlog -sn --no-merges` | 작성자별 커밋 수 (머지 제외) |

### 릴리스 분석 예시

```bash
# 변경된 파일 수 확인
git diff --name-status v1.0.0..v1.1.0 | wc -l

# 커밋 수 확인
git log --oneline --no-merges v1.0.0..v1.1.0 | wc -l

# 기능 커밋 찾기
git log --grep="feat" --oneline --no-merges v1.0.0..v1.1.0

# 상위 기여자
git shortlog -sn --no-merges v1.0.0..v1.1.0 | head -3
```

### 범위 지정 팁

- `A..B` - A 이후 B까지의 커밋 (A 제외, B 포함)
- `A...B` - A와 B의 대칭 차집합
- `-- <경로>/` - 특정 경로의 변경사항만 표시




## 임시 저장 (Stash)

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git stash` | 현재 변경사항을 임시 저장 |
| `git stash pop` | 가장 최근 stash 복원 및 삭제 |
| `git stash apply` | 가장 최근 stash 복원 (stash 유지) |
| `git stash list` | stash 목록 확인 |
| `git stash drop` | 가장 최근 stash 삭제 |
| `git stash clear` | 모든 stash 삭제 |
| `git stash show -p` | 가장 최근 stash 내용 상세 |

### 고급 명령어

| 명령어 | 설명 |
|--------|------|
| `git stash push -m "메시지"` | 메시지와 함께 stash |
| `git stash push <파일>` | 특정 파일만 stash |
| `git stash -u` | 추적되지 않는 파일도 포함 |
| `git stash apply stash@{N}` | 특정 stash 복원 |
| `git stash branch <브랜치>` | stash를 새 브랜치로 복원 |

### pop vs apply 차이점

| 명령어 | stash 목록 | 사용 시점 |
|--------|-----------|----------|
| `git stash pop` | 복원 후 삭제 | 한 번만 사용할 변경사항 |
| `git stash apply` | 복원 후 유지 | 여러 곳에 적용하거나 보관 필요 |

### 변경 전/후 코드 비교

```bash
# 1. 현재 변경사항을 임시 저장
git stash

# 2. 변경 전 버전 실행 확인
dotnet run --project <프로젝트경로>

# 3. 임시 저장한 변경사항 복원
git stash pop

# 4. 변경 후 버전 실행 비교
dotnet run --project <프로젝트경로>
```




## 실행 취소

### 변경사항 취소

| 명령어 | 설명 |
|--------|------|
| `git restore <파일>` | 작업 디렉토리 변경사항 취소 |
| `git restore .` | 모든 변경사항 취소 |

### 스테이징 취소

| 명령어 | 설명 |
|--------|------|
| `git restore --staged <파일>` | 스테이징 취소 |
| `git restore --staged .` | 모든 스테이징 취소 |

### 커밋 취소

| 명령어 | 설명 |
|--------|------|
| `git reset --soft HEAD~1` | 마지막 커밋 취소 (변경사항 스테이징 유지) |
| `git reset --mixed HEAD~1` | 마지막 커밋 취소 (변경사항 작업 디렉토리 유지) |
| `git reset --hard HEAD~1` | 마지막 커밋 취소 (변경사항 삭제) |
| `git revert <커밋>` | 특정 커밋을 되돌리는 새 커밋 생성 |

### Author 정보 수정

```bash
# 최근 커밋의 author 정보 확인
git log --oneline --format="%h %an <%ae> %s" -10

# 특정 커밋 이후의 모든 커밋 author를 현재 config 값으로 변경
git rebase <기준커밋> --exec 'git commit --amend --reset-author --no-edit'

# remote에 반영 (안전 모드)
git push --force-with-lease
```




지금까지 일상적인 Git 작업 흐름을 살펴봤습니다. 이제 커밋과 푸시 시 자동으로 실행되는 Git Hooks 설정을 알아봅니다.

## Git Hooks

### 개요

Git Hooks는 Git 작업(커밋, 푸시 등) 전후에 자동으로 실행되는 스크립트입니다.

```
Git 명령 실행 → Hook 스크립트 실행 → 성공(exit 0) → Git 작업 계속
                                   → 실패(exit 1) → Git 작업 중단
```

### 프로젝트 설정

```bash
# Hook 경로 설정 (프로젝트 clone 후 한 번 실행)
git config core.hooksPath .githooks

# 설정 확인
git config --get core.hooksPath
```

**폴더 구조:**
```
.githooks/
├── commit-msg      # 커밋 메시지 처리
├── pre-commit      # 커밋 전 검사
└── pre-push        # 푸시 전 검사
```

### Hook 위치 비교

| 위치 | 설명 | 공유 여부 |
|------|------|----------|
| `.git/hooks/` | 기본 위치 | 공유 안됨 |
| `.githooks/` | 프로젝트 폴더 | Git으로 공유 |

### Hook 종류

**클라이언트 Hook:**

| Hook | 실행 시점 | 용도 |
|------|----------|------|
| `pre-commit` | 커밋 메시지 입력 전 | 코드 검사, 포맷팅 |
| `commit-msg` | 커밋 메시지 입력 후 | 메시지 검증/수정 |
| `post-commit` | 커밋 완료 후 | 알림, 로깅 |
| `pre-push` | 푸시 전 | 테스트, 브랜치 검사 |

**커밋 Hook 실행 순서:**
```
pre-commit → prepare-commit-msg → commit-msg → post-commit
```

### Hook 작성 기본 구조

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

### commit-msg Hook 예시

Conventional Commits 형식 검증:

```bash
#!/usr/bin/env bash
COMMIT_MSG_FILE="$1"
COMMIT_MSG=$(cat "$COMMIT_MSG_FILE")

PATTERN="^(feat|fix|docs|style|refactor|perf|test|build|ci|chore)(\(.+\))?: .+"

if ! echo "$COMMIT_MSG" | grep -qE "$PATTERN"; then
    echo "ERROR: 커밋 메시지가 Conventional Commits 형식이 아닙니다."
    echo "형식: <type>(<scope>): <description>"
    exit 1
fi

exit 0
```

### pre-push Hook 예시

main 브랜치 직접 푸시 방지:

```bash
#!/usr/bin/env bash
BRANCH=$(git rev-parse --abbrev-ref HEAD)

if echo "$BRANCH" | grep -qE "^(main|master)$"; then
    echo "ERROR: $BRANCH 브랜치에 직접 푸시할 수 없습니다."
    exit 1
fi

exit 0
```

### 프로젝트 commit-msg Hook

Claude Code가 생성하는 텍스트를 자동으로 제거합니다 (`.githooks/commit-msg`):

**제거되는 패턴:**
- `Generated with [Claude Code]`
- `Co-Authored-By: Claude`
- `Claude Code` 포함 줄

### Hook 건너뛰기

```bash
# --no-verify 옵션 사용 (긴급 상황에서만)
git commit --no-verify -m "긴급 수정"
git push --no-verify
```




## 트러블슈팅

### 잘못된 파일을 스테이징했을 때

```bash
git restore --staged <파일>
git restore --staged .
```

### 커밋 메시지를 잘못 작성했을 때

```bash
# 아직 push 하지 않은 경우
git commit --amend -m "올바른 메시지"
```

> **주의**: 이미 push한 커밋의 메시지를 수정하면 force push가 필요합니다.

### 병합 충돌이 발생했을 때

```bash
# 1. 충돌 파일 확인
git status

# 2. 충돌 파일 열어서 수동 해결
# <<<<<<< HEAD / ======= / >>>>>>> 마커 제거

# 3. 충돌 해결 후 스테이징 및 커밋
git add <충돌해결파일>
git commit -m "fix: 병합 충돌 해결"

# 병합 중단이 필요한 경우
git merge --abort
```

### push가 거부되었을 때

```bash
# 원격 변경사항을 먼저 가져와서 병합
git pull

# 또는 리베이스로 가져오기
git pull --rebase

# 이후 다시 push
git push
```

### Hook이 실행되지 않을 때

```bash
# 설정 확인
git config --get core.hooksPath

# 설정이 없으면 추가
git config core.hooksPath .githooks
```

### Permission denied 오류 (Linux/Mac)

```bash
chmod +x .githooks/*
```

### sed 명령어 호환성 문제 (GNU vs BSD)

```bash
# 호환 방식 - 백업 후 삭제
sed -i.bak 's/pattern/replacement/' file
rm -f file.bak
```




## FAQ

### Q1. `git add .`와 `git add -A`의 차이점은?

| 명령어 | 범위 |
|--------|------|
| `git add .` | 현재 디렉토리와 하위의 변경사항만 |
| `git add -A` | 저장소 전체의 모든 변경사항 |

Git 2.x 이상에서 저장소 루트에서 실행할 경우 동일하게 동작합니다.

### Q2. `git fetch`와 `git pull`의 차이점은?

| 명령어 | 동작 |
|--------|------|
| `git fetch` | 원격 변경사항을 가져오기만 함 (로컬 브랜치 변경 X) |
| `git pull` | `git fetch` + `git merge` |

### Q3. `git checkout`과 `git switch`의 차이점은?

Git 2.23 버전부터 `checkout`의 기능이 분리되었습니다:

| 기존 명령어 | 새 명령어 | 용도 |
|------------|----------|------|
| `git checkout <브랜치>` | `git switch <브랜치>` | 브랜치 전환 |
| `git checkout -b <브랜치>` | `git switch -c <브랜치>` | 브랜치 생성 및 전환 |
| `git checkout -- <파일>` | `git restore <파일>` | 파일 변경사항 취소 |

### Q4. 강제 푸시는 언제 사용하나요?

로컬에서 `commit --amend` 또는 `rebase`로 히스토리를 변경한 후, 개인 브랜치에서만 사용합니다.

```bash
# 안전한 강제 푸시
git push --force-with-lease
```

> **주의**: `main`/`master` 브랜치에서는 사용하지 마세요.

### Q5. 병합(merge)과 리베이스(rebase)의 차이점은?

| 구분 | merge | rebase |
|------|-------|--------|
| 히스토리 | 병합 커밋 생성, 보존 | 선형으로 재작성 |
| 장점 | 안전, 협업에 적합 | 깔끔한 히스토리 |
| 권장 | 공유 브랜치에 병합 시 | 개인 브랜치를 최신 main에 맞출 때 |

### Q6. `.git/hooks`와 `.githooks`의 차이점은?

| 항목 | `.git/hooks/` | `.githooks/` |
|------|---------------|--------------|
| Git 추적 | 추적 안됨 | 추적됨 (공유 가능) |
| 기본 설정 | 기본값 | `core.hooksPath` 설정 필요 |
| 용도 | 개인 설정 | 팀 공유 |

### Q7. Hook에서 사용자 입력을 받을 수 있나요?

`pre-commit`과 `commit-msg` Hook에서는 stdin이 리다이렉트되어 있습니다:

```bash
#!/usr/bin/env bash
exec < /dev/tty

read -p "계속하시겠습니까? (y/n) " answer
if [ "$answer" != "y" ]; then
    exit 1
fi
```

### Q8. CI 환경에서 Hook을 비활성화하려면?

```bash
#!/usr/bin/env bash
if [ -n "$CI" ] || [ -n "$GITHUB_ACTIONS" ]; then
    exit 0
fi

# 일반 환경에서 Hook 로직 실행
```

## 참고 문서

- [Git Hooks 공식 문서](https://git-scm.com/docs/githooks)
