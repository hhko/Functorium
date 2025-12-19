# 4.1 Phase 1: 환경 검증 및 준비

> Phase 1에서는 릴리스 노트 생성에 필요한 환경을 검증하고, 비교 범위(Base/Target)를 결정합니다.

---

## 목표

릴리스 노트 생성 전 필수 환경을 검증하고 비교 범위를 결정합니다.

---

## 전제조건 확인

다음 조건을 **모두** 확인해야 합니다:

### 1. Git 저장소 확인

```bash
git status
```

**확인 사항:**
- 현재 디렉터리가 Git 저장소인지
- Git이 설치되어 있는지

### 2. 스크립트 디렉터리 확인

```bash
ls .release-notes/scripts
```

**확인 사항:**
- `.release-notes/scripts` 디렉터리 존재
- `config/component-priority.json` 파일 존재

### 3. .NET SDK 확인

```bash
dotnet --version
```

**확인 사항:**
- .NET 10.x 이상 설치됨
- 설치되지 않은 경우 오류 메시지 출력

### 4. 버전 파라미터 검증

```bash
/release-note v1.2.0
#             ^^^^^^
#             버전 파라미터
```

**확인 사항:**
- 버전이 비어있지 않음
- 유효한 형식 (예: `v1.2.0`, `v1.0.0-alpha.1`)

---

## Base Branch 결정

릴리스 간 비교를 위한 Base Branch를 **자동으로** 결정합니다.

### 결정 로직

```txt
버전 파라미터: v1.2.0
       │
       ▼
┌─────────────────────────────────────┐
│ origin/release/1.0 브랜치 존재?     │
└─────────────────────────────────────┘
       │
       ├── Yes ──▶ Base: origin/release/1.0
       │           Target: HEAD
       │
       └── No ───▶ Base: 초기 커밋 (첫 배포)
                   Target: HEAD
```

### 브랜치 확인 명령어

```bash
# release 브랜치 존재 확인
git rev-parse --verify origin/release/1.0
```

### 시나리오 1: 후속 릴리스

`origin/release/1.0` 브랜치가 존재하는 경우:

```txt
비교 범위:
  Base: origin/release/1.0
  Target: HEAD
  버전: v1.2.0
```

이전 릴리스 이후의 변경사항만 분석합니다.

### 시나리오 2: 첫 번째 릴리스

브랜치가 없는 경우 (첫 배포):

```bash
# 초기 커밋 SHA 찾기
git rev-list --max-parents=0 HEAD
```

```txt
첫 배포로 감지되었습니다

비교 범위:
  Base: abc1234 (초기 커밋)
  Target: HEAD
  버전: v1.0.0
```

저장소의 모든 커밋을 분석합니다.

---

## 환경 검증 실패 처리

환경 검증 실패 시 명확한 오류 메시지를 출력하고 **즉시 중단**합니다.

### 오류 1: Git 저장소 아님

```txt
오류: Git 저장소가 아닙니다

현재 디렉터리에서 'git status'를 실행할 수 없습니다.
Git 저장소 루트 디렉터리에서 명령을 실행하십시오.
```

**해결:**
```bash
cd /path/to/your/project
```

### 오류 2: .NET SDK 없음

```txt
오류: .NET 10 SDK가 필요합니다

'dotnet --version' 명령을 실행할 수 없습니다.

설치 방법:
  https://dotnet.microsoft.com/download/dotnet/10.0
```

**해결:**
- .NET 10 SDK 설치
- 환경 변수 PATH 확인

### 오류 3: 스크립트 디렉터리 없음

```txt
오류: 릴리스 노트 스크립트를 찾을 수 없습니다

'.release-notes/scripts' 디렉터리가 존재하지 않습니다.
프로젝트 루트 디렉터리에서 명령을 실행하십시오.
```

**해결:**
- 프로젝트 루트 디렉터리에서 명령 실행
- `.release-notes` 폴더 존재 확인

### 오류 4: 버전 파라미터 없음

```txt
오류: 버전 파라미터가 필요합니다

사용법:
  /release-note v1.2.0        # 정규 릴리스
  /release-note v1.0.0        # 첫 배포
  /release-note v1.2.0-beta.1 # 프리릴리스
```

**해결:**
```bash
/release-note v1.2.0
```

---

## 콘솔 출력 형식

### 환경 검증 성공

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 1: 환경 검증 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

전제조건:
  Git 저장소
  .NET SDK 10.x
  스크립트 디렉터리

비교 범위:
  Base: origin/release/1.0
  Target: HEAD
  버전: v1.2.0
```

### 첫 배포 감지

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 1: 환경 검증 완료
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

전제조건:
  Git 저장소
  .NET SDK 10.x
  스크립트 디렉터리

첫 배포로 감지되었습니다

비교 범위:
  Base: abc1234 (초기 커밋)
  Target: HEAD
  버전: v1.0.0
```

---

## 성공 기준 체크리스트

Phase 1 완료를 위해 다음을 모두 확인하세요:

- [ ] Git 저장소 확인됨
- [ ] .NET SDK 버전 확인됨 (10.x 이상)
- [ ] 스크립트 디렉터리 존재 확인됨
- [ ] 버전 파라미터 유효함
- [ ] Base/Target 브랜치 결정됨

---

## 실제 예시

### 예시 1: 후속 릴리스 (v1.2.0)

```bash
$ /release-note v1.2.0

Phase 1: 환경 검증 중...
  Checking Git repository... OK
  Checking .NET SDK... 10.0.100
  Checking scripts directory... OK
  Checking version parameter... v1.2.0
  Determining base branch...
    Found: origin/release/1.0

비교 범위 결정:
  Base: origin/release/1.0
  Target: HEAD
```

### 예시 2: 첫 배포 (v1.0.0)

```bash
$ /release-note v1.0.0

Phase 1: 환경 검증 중...
  Checking Git repository... OK
  Checking .NET SDK... 10.0.100
  Checking scripts directory... OK
  Checking version parameter... v1.0.0
  Determining base branch...
    No release branch found
    Using initial commit as base

첫 배포로 감지됨:
  Base: 7a8b9c0 (initial commit)
  Target: HEAD
```

---

## 요약

| 항목 | 설명 |
|------|------|
| 목표 | 환경 검증 및 비교 범위 결정 |
| 전제조건 | Git, .NET 10, 스크립트 디렉터리 |
| 비교 범위 | Base Branch 자동 결정 |
| 오류 처리 | 명확한 메시지 및 해결 방법 |
| 출력 | Base/Target, 버전 정보 |

---

## 다음 단계

환경 검증 완료 후 [4.2 Phase 2: 데이터 수집](02-phase2-collection.md)으로 진행합니다.
