# Git 명령어 가이드

이 문서는 Git의 주요 명령어와 사용법을 설명합니다.

## 목차
- [요약](#요약)
- [변경사항 확인](#변경사항-확인)
- [스테이징](#스테이징)
- [커밋](#커밋)
- [태그](#태그)
- [브랜치](#브랜치)
- [원격 저장소](#원격-저장소)
- [실행 취소](#실행-취소)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

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
git tag --list                        # 태그 목록
git tag -l -n1                        # 태그 목록과 메시지 첫 줄
git tag v1.0.0                        # 태그 생성
git tag --sort=-v:refname             # 버전 역순 정렬
```

**긴급 상황:**
```bash
git restore <파일>                    # 변경 취소
git restore --staged <파일>           # 스테이징 취소
git commit --amend -m "새 메시지"     # 커밋 메시지 수정
git reset --soft HEAD~1               # 커밋 취소
git merge --abort                     # 병합 취소
```

### 주요 절차

**1. 기본 워크플로우:**
```bash
# 1. 변경사항 확인
git status
git diff

# 2. 파일 스테이징
git add <파일>

# 3. 커밋
git commit -m "feat: 새 기능 추가"

# 4. 원격에 푸시
git push
```

**2. 브랜치 작업:**
```bash
# 1. 새 브랜치 생성 및 전환
git switch -c feature/login

# 2. 작업 및 커밋
git add .
git commit -m "feat: 로그인 기능 추가"

# 3. main으로 전환
git switch main

# 4. 브랜치 병합
git merge feature/login

# 5. 브랜치 삭제
git branch -d feature/login
```

### 주요 개념

**1. Git 워크플로우 이해**
- 작업 디렉토리 → 스테이징 영역 → 로컬 저장소 → 원격 저장소
- `git add`로 스테이징, `git commit`으로 저장, `git push`로 공유

**2. 브랜치 전략**
- `main`: 안정 버전
- `feature/*`: 새 기능 개발
- 작업 완료 후 `main`에 병합

**3. 안전 수칙**
- ✓ 변경 전 `git status`로 현재 상태 확인
- ✓ 커밋 전 `git diff`로 변경 내용 검토
- ✓ 작고 자주 커밋 (논리적 단위)
- ✗ `--force` 금지 (개인 브랜치만 허용)
- ✗ `main` 직접 커밋 금지

<br/>

## 변경사항 확인

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git status` | 작업 디렉토리 상태 확인 |
| `git diff` | 스테이징되지 않은 변경 내용 확인 |
| `git diff --staged` | 스테이징된 변경 내용 확인 |
| `git log` | 커밋 이력 확인 |
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
| `git log --format="%ai"` | 커밋 날짜만 출력 |
| `git log --format="%h\|%an\|%s"` | 해시, 작성자, 제목 출력 (구분자: \|) |

**주요 포맷 지정자:**
- `%h` - 짧은 해시
- `%H` - 전체 해시
- `%an` - 작성자 이름
- `%ae` - 작성자 이메일
- `%ad` - 작성 날짜
- `%ai` - 작성 날짜 (ISO 8601 형식)
- `%s` - 커밋 제목
- `%b` - 커밋 본문
- `%D` - ref 이름 (브랜치, 태그)

### 범위 지정

| 명령어 | 설명 |
|--------|------|
| `git log v1.0.0..HEAD` | 태그 이후 커밋 확인 |
| `git log v1.0.0..v1.1.0` | 두 태그 사이 커밋 확인 |
| `git log HEAD~10..HEAD` | 최근 10개 커밋 확인 |
| `git log main..feature` | main 이후 feature 브랜치 커밋 확인 |

### 사용 예시

```bash
# 현재 작업 상태 확인
git status

# 변경된 내용 확인
git diff

# 스테이징된 내용 확인
git diff --staged

# 최근 커밋 5개 확인
git log --oneline -5

# 커밋 해시, 작성자, 제목 출력 (구분자: |)
git log --format="%h|%an|%s"

# 최근 10개 커밋의 해시, 작성자, 제목 확인
git log -10 --format="%h|%an|%s"

# 태그 이후 커밋 확인 (머지 제외)
git log v1.0.0..HEAD --oneline --no-merges

# 최근 10개 커밋 날짜 확인
git log HEAD~10..HEAD --format="%ai"
```

<br/>

## 스테이징

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git add <파일>` | 특정 파일 스테이징 |
| `git add .` | 현재 디렉토리의 모든 변경사항 스테이징 |
| `git add -A` | 저장소 전체의 모든 변경사항 스테이징 |
| `git add -p` | 대화형 스테이징 (변경사항 선택) |
| `git add *.js` | 특정 패턴의 파일 스테이징 |

### 사용 예시

```bash
# 특정 파일 스테이징
git add README.md

# 모든 변경사항 스테이징
git add .

# 변경사항 선택적으로 스테이징
git add -p
```

<br/>

## 커밋

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git commit -m "메시지"` | 메시지와 함께 커밋 |
| `git commit` | 에디터로 메시지 작성 후 커밋 |
| `git commit -am "메시지"` | 추적 중인 파일 스테이징 및 커밋 |
| `git commit --amend` | 마지막 커밋 수정 |
| `git commit --amend -m "메시지"` | 마지막 커밋 메시지 수정 |

### 사용 예시

```bash
# 간단한 커밋
git commit -m "feat: 로그인 기능 추가"

# 본문이 있는 커밋 (에디터 사용)
git commit

# 마지막 커밋 메시지 수정
git commit --amend -m "fix: 로그인 버그 수정"
```

<br/>

## 태그

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git tag` | 태그 목록 확인 |
| `git tag --list` | 태그 목록 확인 (동일) |
| `git tag -l -n1` | 태그 목록과 메시지 첫 줄 확인 |
| `git tag -l -n` | 태그 목록과 전체 메시지 확인 |
| `git tag <이름>` | 경량 태그 생성 |
| `git tag -a <이름> -m "메시지"` | 주석 태그 생성 |
| `git tag -d <이름>` | 태그 삭제 |
| `git push origin <태그>` | 태그 원격에 푸시 |
| `git push origin --tags` | 모든 태그 푸시 |
| `git push origin :refs/tags/<태그>` | 원격 태그 삭제 |

### 태그 정렬

| 명령어 | 설명 |
|--------|------|
| `git tag --sort=-v:refname` | 버전 역순 정렬 (최신 먼저) |
| `git tag --sort=v:refname` | 버전 순 정렬 (오래된 것 먼저) |
| `git tag --sort=-creatordate` | 생성일 역순 정렬 |

### 사용 예시

```bash
# 태그 목록 확인
git tag --list

# 태그 목록과 메시지 첫 줄 확인
git tag -l -n1

# 태그 목록과 전체 메시지 확인
git tag -l -n

# 최신 태그 확인 (버전 역순 정렬 후 첫 번째)
git tag --sort=-v:refname | head -1

# 주석 태그 생성
git tag -a v1.0.0 -m "버전 1.0.0 릴리스"

# 경량 태그 생성
git tag v1.0.1

# 태그 원격에 푸시
git push origin v1.0.0

# 모든 태그 푸시
git push origin --tags

# 로컬 태그 삭제
git tag -d v1.0.0

# 원격 태그 삭제 (방법 1)
git push origin --delete v1.0.0

# 원격 태그 삭제 (방법 2)
git push origin :refs/tags/v1.0.0
```

<br/>

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
| `git checkout <브랜치>` | 브랜치 전환 |
| `git checkout -b <이름>` | 브랜치 생성 및 전환 |
| `git switch <브랜치>` | 브랜치 전환 (Git 2.23+) |
| `git switch -c <이름>` | 브랜치 생성 및 전환 (Git 2.23+) |

### 브랜치 병합

| 명령어 | 설명 |
|--------|------|
| `git merge <브랜치>` | 현재 브랜치에 다른 브랜치 병합 |
| `git merge --no-ff <브랜치>` | Fast-forward 없이 병합 (병합 커밋 생성) |
| `git rebase <브랜치>` | 현재 브랜치를 다른 브랜치 위로 리베이스 |

### 사용 예시

```bash
# 브랜치 목록 확인
git branch

# 새 브랜치 생성 및 전환
git switch -c feature/login

# 브랜치 전환
git switch main

# 브랜치 병합
git merge feature/login

# 브랜치 삭제
git branch -d feature/login
```

<br/>

## 원격 저장소

### 기본 명령어

| 명령어 | 설명 |
|--------|------|
| `git remote -v` | 원격 저장소 목록 확인 |
| `git remote add <이름> <URL>` | 원격 저장소 추가 |
| `git remote remove <이름>` | 원격 저장소 제거 |
| `git remote rename <이전> <새이름>` | 원격 저장소 이름 변경 |

### 동기화 명령어

| 명령어 | 설명 |
|--------|------|
| `git fetch` | 원격 변경사항 가져오기 (병합 X) |
| `git fetch --all` | 모든 원격 저장소에서 가져오기 |
| `git pull` | 원격 변경사항 가져오기 및 병합 |
| `git pull --rebase` | 원격 변경사항 가져오기 및 리베이스 |
| `git push` | 로컬 커밋을 원격에 푸시 |
| `git push -u origin <브랜치>` | 업스트림 설정 후 푸시 |
| `git push --force` | 강제 푸시 (주의 필요) |

### 사용 예시

```bash
# 원격 저장소 확인
git remote -v

# 원격 저장소 추가
git remote add origin https://github.com/user/repo.git

# 원격 변경사항 가져오기
git fetch

# 원격 변경사항 가져오기 및 병합
git pull

# 로컬 커밋 푸시
git push

# 새 브랜치 첫 푸시 (업스트림 설정)
git push -u origin feature/login
```

<br/>

## 실행 취소

### 변경사항 취소

| 명령어 | 설명 |
|--------|------|
| `git restore <파일>` | 작업 디렉토리 변경사항 취소 |
| `git restore .` | 모든 변경사항 취소 |
| `git checkout -- <파일>` | 변경사항 취소 (구버전) |

### 스테이징 취소

| 명령어 | 설명 |
|--------|------|
| `git restore --staged <파일>` | 스테이징 취소 |
| `git restore --staged .` | 모든 스테이징 취소 |
| `git reset HEAD <파일>` | 스테이징 취소 (구버전) |

### 커밋 취소

| 명령어 | 설명 |
|--------|------|
| `git reset --soft HEAD~1` | 마지막 커밋 취소 (변경사항 스테이징 유지) |
| `git reset --mixed HEAD~1` | 마지막 커밋 취소 (변경사항 작업 디렉토리 유지) |
| `git reset --hard HEAD~1` | 마지막 커밋 취소 (변경사항 삭제) |
| `git revert <커밋>` | 특정 커밋을 되돌리는 새 커밋 생성 |

### 사용 예시

```bash
# 파일 변경사항 취소
git restore src/main.js

# 스테이징 취소
git restore --staged src/main.js

# 마지막 커밋 취소 (변경사항 유지)
git reset --soft HEAD~1

# 특정 커밋 되돌리기
git revert abc1234
```

<br/>

## 트러블슈팅

### 잘못된 파일을 스테이징했을 때

```bash
# 특정 파일 스테이징 취소
git restore --staged <파일>

# 모든 스테이징 취소
git restore --staged .
```

### 커밋 메시지를 잘못 작성했을 때

```bash
# 마지막 커밋 메시지 수정 (아직 push 하지 않은 경우)
git commit --amend -m "올바른 메시지"
```

> **주의**: 이미 push한 커밋의 메시지를 수정하면 force push가 필요하며, 다른 팀원에게 영향을 줄 수 있습니다.

### 커밋을 취소하고 싶을 때

```bash
# 변경사항을 유지하면서 커밋만 취소
git reset --soft HEAD~1

# 변경사항도 함께 삭제
git reset --hard HEAD~1
```

### 병합 충돌이 발생했을 때

1. 충돌 파일 확인:
   ```bash
   git status
   ```

2. 충돌 파일 열어서 수동으로 해결:
   ```
   <<<<<<< HEAD
   현재 브랜치의 내용
   =======
   병합하려는 브랜치의 내용
   >>>>>>> feature/branch
   ```

3. 충돌 해결 후 스테이징 및 커밋:
   ```bash
   git add <충돌해결파일>
   git commit -m "fix: 병합 충돌 해결"
   ```

4. 병합 중단이 필요한 경우:
   ```bash
   git merge --abort
   ```

### push가 거부되었을 때

원격 저장소에 로컬에 없는 커밋이 있는 경우:

```bash
# 원격 변경사항을 먼저 가져와서 병합
git pull

# 또는 리베이스로 가져오기
git pull --rebase

# 이후 다시 push
git push
```

### 브랜치 삭제가 실패할 때

병합되지 않은 브랜치를 삭제하려면:

```bash
# 강제 삭제 (데이터 손실 주의)
git branch -D <브랜치명>
```

<br/>

## FAQ

### Q1. `git add .`와 `git add -A`의 차이점은 무엇인가요?

| 명령어 | 범위 | 설명 |
|--------|------|------|
| `git add .` | 현재 디렉토리 | 현재 디렉토리와 하위의 변경사항만 스테이징 |
| `git add -A` | 저장소 전체 | 저장소 전체의 모든 변경사항 스테이징 |

Git 2.x 이상에서는 저장소 루트에서 실행할 경우 동일하게 동작합니다.

### Q2. `git diff`와 `git diff --staged`의 차이점은 무엇인가요?

| 명령어 | 비교 대상 |
|--------|----------|
| `git diff` | 작업 디렉토리 vs 스테이징 영역 |
| `git diff --staged` | 스테이징 영역 vs 마지막 커밋 |

### Q3. 커밋 메시지를 잘못 작성했을 때 어떻게 수정하나요?

아직 push하지 않은 경우:
```bash
git commit --amend -m "수정된 메시지"
```

이미 push한 경우:
```bash
git commit --amend -m "수정된 메시지"
git push --force  # 주의: 팀원과 협의 필요
```

### Q4. 특정 파일만 커밋에서 제외하려면 어떻게 하나요?

1. 이미 스테이징된 경우:
   ```bash
   git restore --staged <파일>
   ```

2. 항상 제외하려면 `.gitignore`에 추가:
   ```bash
   echo "제외할파일.txt" >> .gitignore
   ```

### Q5. 이전 커밋에 변경사항을 추가하려면 어떻게 하나요?

```bash
# 변경사항 스테이징
git add <파일>

# 이전 커밋에 추가 (메시지 유지)
git commit --amend --no-edit
```

### Q6. `git fetch`와 `git pull`의 차이점은 무엇인가요?

| 명령어 | 동작 |
|--------|------|
| `git fetch` | 원격 변경사항을 가져오기만 함 (로컬 브랜치 변경 X) |
| `git pull` | `git fetch` + `git merge` (원격 변경사항을 가져와서 병합) |

안전하게 원격 변경사항을 확인하고 싶다면 `git fetch` 후 `git log origin/main`으로 확인한 뒤 병합하세요.

### Q7. `git checkout`과 `git switch`의 차이점은 무엇인가요?

Git 2.23 버전부터 `checkout`의 기능이 분리되었습니다:

| 기존 명령어 | 새 명령어 | 용도 |
|------------|----------|------|
| `git checkout <브랜치>` | `git switch <브랜치>` | 브랜치 전환 |
| `git checkout -b <브랜치>` | `git switch -c <브랜치>` | 브랜치 생성 및 전환 |
| `git checkout -- <파일>` | `git restore <파일>` | 파일 변경사항 취소 |

새 명령어가 더 명확하고 안전하므로 사용을 권장합니다.

### Q8. 원격 브랜치를 로컬에서 추적하려면 어떻게 하나요?

```bash
# 원격 브랜치 목록 확인
git branch -r

# 원격 브랜치를 로컬에서 추적
git checkout -b <로컬브랜치> origin/<원격브랜치>

# 또는 (Git 2.23+)
git switch -c <로컬브랜치> --track origin/<원격브랜치>
```

### Q9. 강제 푸시(force push)는 언제 사용하나요?

**사용하는 경우:**
- 로컬에서 `commit --amend` 또는 `rebase`로 히스토리를 변경한 후
- 개인 브랜치에서 작업할 때

**주의사항:**
- `main`/`master` 브랜치에는 사용하지 마세요
- 팀원이 같은 브랜치에서 작업 중이라면 협의 후 사용하세요
- 가능하면 `--force-with-lease` 옵션 사용을 권장합니다

```bash
# 안전한 강제 푸시 (원격에 예상치 못한 변경이 있으면 실패)
git push --force-with-lease
```

### Q10. 병합(merge)과 리베이스(rebase)의 차이점은 무엇인가요?

| 구분 | merge | rebase |
|------|-------|--------|
| 히스토리 | 병합 커밋 생성, 히스토리 보존 | 히스토리를 선형으로 재작성 |
| 장점 | 안전, 협업에 적합 | 깔끔한 히스토리 |
| 단점 | 복잡한 히스토리 | 히스토리 변경으로 충돌 가능 |

**권장 사용법:**
- `merge`: 공유 브랜치(main)에 기능 브랜치 병합 시
- `rebase`: 개인 브랜치를 최신 main에 맞춰 때

```bash
# merge 사용
git checkout main
git merge feature/login

# rebase 사용 (개인 브랜치에서)
git checkout feature/login
git rebase main
```

## 참고 문서

- [Git 커밋 가이드](./Guide-Commit-Conventions.md) - Git 커밋 메시지 작성 규칙
