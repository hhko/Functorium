# Build-CommitSummary.ps1 스크립트 매뉴얼

이 문서는 `Build-CommitSummary.ps1` 스크립트의 사용법과 기능을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [설치 및 요구사항](#설치-및-요구사항)
- [사용법](#사용법)
- [범위 결정 로직](#범위-결정-로직)
- [출력 형식](#출력-형식)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### 목적

Git 커밋 이력을 Conventional Commits 규격에 따라 분석하고, 타입별로 그룹화된 요약 문서를 생성합니다.

### 주요 기능

| 기능 | 설명 |
|------|------|
| 커밋 분석 | Conventional Commits 형식 자동 파싱 |
| 타입별 그룹화 | feat, fix, docs 등 타입별 분류 |
| 통계 생성 | 타입별 커밋 수 및 비율 계산 |
| 마크다운 출력 | 구조화된 요약 문서 생성 |

### 파일 구조

```
프로젝트루트/
├── Build-CommitSummary.ps1    # 스크립트
└── .commit-summaries/         # 출력 디렉토리 (자동 생성)
    └── summary-*.md           # 생성된 요약 문서
```

<br/>

## 요약

### 주요 명령

**기본 실행:**
```powershell
./Build-CommitSummary.ps1
```

**범위 지정:**
```powershell
./Build-CommitSummary.ps1 -Range "v1.0.0..HEAD"
./Build-CommitSummary.ps1 -r "HEAD~10..HEAD"
```

**브랜치 지정:**
```powershell
./Build-CommitSummary.ps1 -TargetBranch develop
./Build-CommitSummary.ps1 -t develop
```

**출력 디렉토리 지정:**
```powershell
./Build-CommitSummary.ps1 -OutputDir "./releases"
./Build-CommitSummary.ps1 -o "./releases"
```

**도움말:**
```powershell
./Build-CommitSummary.ps1 -Help
./Build-CommitSummary.ps1 -h
```

### 주요 절차

**1. 릴리스 노트 생성:**
```powershell
# 1. 마지막 태그 이후 커밋 요약
./Build-CommitSummary.ps1

# 2. 생성된 파일 확인
ls .commit-summaries/

# 3. 내용 검토
cat .commit-summaries/summary-*.md
```

**2. 특정 범위 분석:**
```powershell
# 1. 두 태그 사이 커밋 분석
./Build-CommitSummary.ps1 -Range "v1.0.0..v1.1.0"

# 2. 결과 확인
cat .commit-summaries/summary-v1.0.0..v1.1.0-*.md
```

### 주요 개념

**1. Conventional Commits**
- 커밋 메시지 형식: `type(scope): description`
- 지원 타입: feat, fix, docs, style, refactor, perf, test, build, ci, chore
- 규격 외 커밋은 `other`로 분류

**2. 범위 결정 우선순위**
- 태그가 있으면: `[마지막태그]..HEAD`
- 태그 없음 + 다른 브랜치: `[TargetBranch]..HEAD`
- 태그 없음 + 같은 브랜치: `HEAD` (전체 커밋)

**3. 출력 구조**
- 통계 테이블: 타입별 커밋 수와 비율
- 상세 목록: 타입별로 그룹화된 커밋 목록
- 메타 정보: 범위, 생성일, 작성자

<br/>

## 설치 및 요구사항

### 요구사항

| 항목 | 요구사항 |
|------|----------|
| PowerShell | 7.0 이상 |
| Git | 설치 필요 |
| 실행 위치 | Git 저장소 루트 |

### 버전 확인

```powershell
# PowerShell 버전 확인
$PSVersionTable.PSVersion

# Git 버전 확인
git --version
```

### 실행 권한

```powershell
# 실행 권한 확인
Get-ExecutionPolicy

# 권한 부여 (필요시)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

<br/>

## 사용법

### 파라미터

| 파라미터 | 별칭 | 설명 | 기본값 |
|----------|------|------|--------|
| `-Range` | `-r` | Git 범위 표현식 | 자동 결정 |
| `-TargetBranch` | `-t` | 대상 브랜치 | `main` |
| `-OutputDir` | `-o` | 출력 디렉토리 경로 | `.commit-summaries` |
| `-Help` | `-h`, `-?` | 도움말 표시 | - |

### 범위 표현식 예시

| 표현식 | 설명 |
|--------|------|
| `v1.0.0..HEAD` | 태그 v1.0.0 이후 커밋 |
| `v1.0.0..v1.1.0` | 두 태그 사이 커밋 |
| `HEAD~10..HEAD` | 최근 10개 커밋 |
| `main..feature` | main 이후 feature 브랜치 커밋 |
| `HEAD` | 전체 커밋 |

### 사용 예시

```powershell
# 기본 실행 (마지막 태그 이후)
./Build-CommitSummary.ps1

# 특정 범위 지정
./Build-CommitSummary.ps1 -Range "v1.0.0..HEAD"

# 최근 N개 커밋
./Build-CommitSummary.ps1 -r "HEAD~20..HEAD"

# 다른 브랜치 기준
./Build-CommitSummary.ps1 -TargetBranch develop

# 출력 디렉토리 지정
./Build-CommitSummary.ps1 -OutputDir "./releases"

# 전체 커밋 분석
./Build-CommitSummary.ps1 -Range "HEAD"
```

<br/>

## 범위 결정 로직

### 자동 범위 결정

`-Range` 파라미터를 지정하지 않으면 다음 우선순위로 범위를 결정합니다:

```
1. 태그가 있으면         → [마지막태그]..HEAD
2. 태그 없음 + 다른 브랜치 → [TargetBranch]..HEAD
3. 태그 없음 + 같은 브랜치 → HEAD (전체 커밋)
```

### 시나리오별 동작

**시나리오 1: 태그가 있는 경우**
```powershell
# 태그: v1.0.0, v1.1.0 존재
./Build-CommitSummary.ps1
# 범위: v1.1.0..HEAD (마지막 태그 이후)
```

**시나리오 2: 태그 없음 + feature 브랜치**
```powershell
# 현재 브랜치: feature/login
# 태그: 없음
./Build-CommitSummary.ps1
# 범위: main..HEAD (대상 브랜치 기준)
```

**시나리오 3: 태그 없음 + main 브랜치**
```powershell
# 현재 브랜치: main
# 태그: 없음
./Build-CommitSummary.ps1
# 범위: HEAD (전체 커밋)
```

### 마지막 태그 확인

```powershell
# 마지막 태그 확인
git tag --sort=-v:refname | Select-Object -First 1

# 태그 목록 확인
git tag -l
```

<br/>

## 출력 형식

### 콘솔 출력

스크립트 실행 시 다음 정보가 콘솔에 출력됩니다:

```
[DONE] Commit summary document generated

Target branch: main
Range: v1.0.0..HEAD
Period: 2025-01-01 ~ 2025-12-05

Commit statistics:
  feat:         5 ( 25.0%)
  fix:          3 ( 15.0%)
  docs:         2 ( 10.0%)
  ...
  ─────────────────────────
  total:       20 (100.0%)

Generated file:
  .commit-summaries/summary-v1.0.0..HEAD-20251205-143022.md
```

### 파일 이름

```
<OutputDir>/summary-{범위}-{타임스탬프}.md
```

**예시:**
- `.commit-summaries/summary-v1.0.0..HEAD-20251205-143022.md`
- `./releases/summary-HEAD-10..HEAD-20251205-150000.md`

### 문서 구조

생성된 마크다운 문서는 다음 구조를 가집니다:

```markdown
# Commit Summary

**Range**: v1.0.0..HEAD
**Generated**: 2025-12-05 14:30:22

## Commit Statistics by Type

| Type        | Description               | Count  | Ratio   |
|-------------|---------------------------|--------|---------|
| `feat`      | New features              | 5      | 25.0%   |
| `fix`       | Bug fixes                 | 3      | 15.0%   |
| ...         | ...                       | ...    | ...     |

---

## feat (New features) - 5 commits

- `abc1234` **AuthorName     ** feat(auth): Add login feature
- `def5678` **AuthorName     ** feat: Implement registration

## fix (Bug fixes) - 3 commits

- `ghi9012` **AuthorName     ** fix: Fix login error
...

---

**Output path**: `.commit-summaries/summary-v1.0.0..HEAD-20251205-143022.md`
```

### 커밋 타입

| 타입 | 설명 |
|------|------|
| `feat` | 새로운 기능 |
| `fix` | 버그 수정 |
| `docs` | 문서 변경 |
| `style` | 코드 포맷팅 |
| `refactor` | 리팩터링 |
| `perf` | 성능 개선 |
| `test` | 테스트 |
| `build` | 빌드/의존성 |
| `ci` | CI 설정 |
| `chore` | 기타 변경 |
| `other` | 규격 외 커밋 |

<br/>

## 트러블슈팅

### 커밋이 0개로 표시될 때

**원인 1**: 태그 없이 main 브랜치에서 실행

**해결**:
```powershell
# 전체 커밋 분석
./Build-CommitSummary.ps1 -Range "HEAD"

# 또는 최근 N개 커밋
./Build-CommitSummary.ps1 -r "HEAD~20..HEAD"
```

**원인 2**: 잘못된 범위 표현식

**해결**:
```powershell
# 범위 유효성 확인
git log v1.0.0..HEAD --oneline

# 태그 존재 확인
git tag -l
```

### Git 저장소가 아니라는 오류

**원인**: 스크립트를 Git 저장소 외부에서 실행

**해결**:
```powershell
# Git 저장소 루트로 이동
cd C:\path\to\your\repo

# .git 폴더 확인
Test-Path .git
```

### PowerShell 버전 오류

**원인**: PowerShell 7.0 미만 버전 사용

**해결**:
```powershell
# 버전 확인
$PSVersionTable.PSVersion

# PowerShell 7 설치 (Windows)
winget install Microsoft.PowerShell

# PowerShell 7로 실행
pwsh ./Build-CommitSummary.ps1
```

### 실행 권한 오류

**원인**: 스크립트 실행 정책 제한

**해결**:
```powershell
# 현재 사용자에 대해 실행 허용
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# 또는 일회성 실행
pwsh -ExecutionPolicy Bypass -File ./Build-CommitSummary.ps1
```

### 출력 디렉토리 권한 오류

**원인**: `.commit-summaries` 폴더 쓰기 권한 없음

**해결**:
```powershell
# 디렉토리 권한 확인
Get-Acl .commit-summaries

# 수동으로 디렉토리 생성
New-Item -ItemType Directory -Path .commit-summaries -Force
```

<br/>

## FAQ

### Q1. Conventional Commits 형식이 아닌 커밋은 어떻게 처리되나요?

**A:** `other` (기타) 타입으로 분류됩니다. 통계에서 "Non-conventional commits"로 표시됩니다.

```
# Conventional Commits 형식
feat: 새 기능 추가        → feat 타입
fix(auth): 로그인 수정    → fix 타입

# 비표준 형식
Update README             → other 타입
버그 수정                 → other 타입
```

### Q2. 브랜치 이름 정보는 어디서 오나요?

**A:** 머지 커밋에서 추출합니다. 다음 경우에는 브랜치 정보를 확인할 수 없습니다:

| 머지 방식 | 브랜치 추적 |
|----------|------------|
| 일반 머지 | 가능 |
| Squash 머지 | 불가능 |
| Rebase | 불가능 |
| Fast-forward | 불가능 |

### Q3. 기존 요약 파일을 덮어쓰나요?

**A:** 아니요. 파일명에 타임스탬프가 포함되어 항상 새 파일이 생성됩니다.

```
summary-v1.0.0..HEAD-20251205-140000.md  # 첫 번째 실행
summary-v1.0.0..HEAD-20251205-150000.md  # 두 번째 실행
```

### Q4. 특정 타입의 커밋만 분석할 수 있나요?

**A:** 현재 스크립트는 모든 타입을 분석합니다. 특정 타입만 필요하면 Git 명령어로 필터링하세요:

```powershell
# feat 타입만 조회
git log --oneline --no-merges --grep="^feat"
```

### Q5. 작성자 이름이 잘리는 이유는 무엇인가요?

**A:** 문서 가독성을 위해 작성자 이름을 15자로 제한합니다. 긴 이름은 `...`으로 표시됩니다.

```
# 원본: VeryLongAuthorName
# 출력: VeryLongAuth...
```

### Q6. `.commit-summaries` 폴더를 Git에서 제외해야 하나요?

**A:** 프로젝트 정책에 따라 결정하세요:

```gitignore
# 버전 관리에서 제외
.commit-summaries/

# 또는 유지 (릴리스 노트로 활용)
```

### Q7. CI/CD에서 자동으로 실행할 수 있나요?

**A:** 네. GitHub Actions 예시:

```yaml
- name: Generate commit summary
  shell: pwsh
  run: ./Build-CommitSummary.ps1 -Range "${{ github.event.before }}..${{ github.sha }}"

- name: Upload summary
  uses: actions/upload-artifact@v4
  with:
    name: commit-summary
    path: .commit-summaries/
```

### Q8. 머지 커밋은 포함되나요?

**A:** 아니요. `--no-merges` 옵션으로 머지 커밋은 제외됩니다. 실제 작업 커밋만 분석합니다.

### Q9. 다른 브랜치의 커밋을 분석하려면?

**A:** 해당 브랜치로 체크아웃하거나 범위를 직접 지정하세요:

```powershell
# 브랜치 전환 후 실행
git checkout feature/login
./Build-CommitSummary.ps1

# 또는 범위 직접 지정
./Build-CommitSummary.ps1 -Range "main..feature/login"
```

### Q10. 출력 디렉토리를 변경할 수 있나요?

**A:** 네, `-OutputDir` (또는 `-o`) 파라미터로 지정할 수 있습니다:

```powershell
# releases 폴더에 출력
./Build-CommitSummary.ps1 -OutputDir "./releases"

# 절대 경로 사용
./Build-CommitSummary.ps1 -o "C:/path/to/output"
```

## 참고 문서

- [Git 매뉴얼](./Manual-Git.md) - Git 범위 표현식
- [커밋 규칙 가이드](./Guide-Commit-Conventions.md) - Conventional Commits 규격
- [버전 관리 워크플로우 가이드](./Guide-Versioning-Workflow.md) - 태그 기반 버전 관리
