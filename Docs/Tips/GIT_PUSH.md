# Git Push 수정 가이드

## 이미 Push한 커밋 메시지 수정하기

이미 원격 저장소에 push한 커밋의 메시지를 수정해야 할 때 사용합니다.

> **주의:** `--force` push는 히스토리를 변경하므로 협업 시 주의가 필요합니다. 다른 팀원이 해당 브랜치를 pull한 경우 충돌이 발생할 수 있습니다.

## 방법 1: soft reset 후 재커밋

여러 커밋을 한 번에 수정하거나, 커밋을 합치거나 분리할 때 유용합니다.

### 1단계: 수정할 커밋 확인

```bash
# 최근 커밋 확인
git log --oneline -5

# 전체 커밋 메시지 확인
git log -3 --format="%H%n%B%n---"
```

### 2단계: soft reset으로 커밋 되돌리기

```bash
# 수정할 커밋 이전으로 되돌리기 (변경 내용은 staged 상태로 유지)
git reset --soft <수정할_커밋_이전_해시>
```

| 옵션 | 설명 |
|------|------|
| `--soft` | 커밋만 취소, 변경 내용은 staged 상태로 유지 |
| `--mixed` (기본값) | 커밋 취소, 변경 내용은 unstaged 상태로 유지 |
| `--hard` | 커밋 취소, 변경 내용도 삭제 (주의!) |

### 3단계: 새로운 메시지로 재커밋

```bash
# staged 상태 확인
git status

# 필요 시 파일별로 unstage
git restore --staged <파일명>

# 새 메시지로 커밋
git commit -m "새로운 커밋 메시지"
```

### 4단계: force push

```bash
git push --force
```

## 방법 2: rebase로 특정 커밋 수정

특정 커밋 하나만 수정할 때 유용합니다.

```bash
# 수정할 커밋의 부모 커밋부터 rebase 시작
git rebase -i <수정할_커밋_이전_해시>

# 에디터에서 수정할 커밋의 'pick'을 'reword'로 변경
# 저장 후 종료하면 커밋 메시지 편집 화면이 열림

# force push
git push --force
```

## 방법 3: 가장 최근 커밋만 수정

```bash
# 마지막 커밋 메시지만 수정
git commit --amend -m "새로운 커밋 메시지"

# force push
git push --force
```

## 예시: 3개 커밋을 2개로 정리

### 상황

```
96ab57c chore: Claude Code 로컬 설정 정리        # 불필요한 커밋
40d4bb5 docs: TRX 로거 옵션 문서화               # 유지
675718d chore: TRX 로거 개선                     # 유지
23129db chore: 이전 커밋                         # 기준점
```

### 실행

```bash
# 1. 3개 커밋 이전으로 soft reset
git reset --soft 23129db

# 2. 불필요한 파일 unstage
git restore --staged .claude/settings.local.json

# 3. 변경 내용 복원 (커밋하지 않을 파일)
git restore .claude/settings.local.json

# 4. 새 메시지로 커밋 (파일 지정)
git commit -m "chore: TRX 로거 및 커버리지 리포트 개선" -- .github/workflows/build.yml Build.ps1
git commit -m "docs: TRX 로거 옵션 및 리포트 타입 문서화" -- Docs/Tips/DOTNET_BUILD.md

# 5. force push
git push --force
```

### 결과

```
a244142 docs: TRX 로거 옵션 및 리포트 타입 문서화
5c0a726 chore: TRX 로거 및 커버리지 리포트 개선
23129db chore: 이전 커밋
```

## 주의사항

| 상황 | 권장 사항 |
|------|-----------|
| 혼자 작업하는 브랜치 | `--force` 사용 가능 |
| 팀원과 공유하는 브랜치 | 팀원에게 알린 후 진행 |
| main/master 브랜치 | 가급적 피하고, 필요 시 팀 합의 필요 |
| 이미 PR이 생성된 경우 | PR 리뷰어에게 알림 필요 |

## 참고

- [git reset](https://git-scm.com/docs/git-reset)
- [git rebase](https://git-scm.com/docs/git-rebase)
- [git push --force](https://git-scm.com/docs/git-push)
