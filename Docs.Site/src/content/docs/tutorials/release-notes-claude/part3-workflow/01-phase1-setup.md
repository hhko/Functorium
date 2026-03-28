---
title: "환경 검증 및 준비"
---

어떤 것도 실행되기 전에, 시스템은 릴리스 노트를 생성할 수 있는 상태인지부터 확인해야 합니다. Git 저장소가 아닌 디렉터리에서 커밋을 분석할 수 없고, .NET SDK가 없으면 API를 추출할 수 없으며, 스크립트가 없으면 데이터를 수집할 수 없습니다. Phase 1은 이런 전제조건을 빠짐없이 검증하고, 어떤 범위의 커밋을 분석할지 결정하는 단계입니다.

## 전제조건 확인

다음 네 가지 조건을 **모두** 확인해야 합니다.

### 1. Git 저장소 확인

```bash
git status
```

현재 디렉터리가 Git 저장소인지, Git이 설치되어 있는지 확인합니다. 릴리스 노트의 모든 데이터는 Git 히스토리에서 나오기 때문에, 이 확인이 가장 먼저 이루어집니다.

### 2. 스크립트 디렉터리 확인

```bash
ls .release-notes/scripts
```

`.release-notes/scripts` 디렉터리와 `config/component-priority.json` 파일이 존재하는지 확인합니다. Phase 2에서 실행할 C# 스크립트들이 이 디렉터리에 있습니다.

### 3. .NET SDK 확인

```bash
dotnet --version
```

.NET 10.x 이상이 설치되어 있어야 합니다. C# 스크립트 실행과 프로젝트 빌드에 필요합니다.

### 4. 버전 파라미터 검증

```bash
/release-note v1.2.0
#             ^^^^^^
#             버전 파라미터
```

버전이 비어있지 않고, 유효한 형식(예: `v1.2.0`, `v1.0.0-alpha.1`)인지 확인합니다.

## Base Branch 결정

전제조건이 통과되면, 릴리스 간 비교를 위한 Base Branch를 **자동으로** 결정합니다. 이 결정이 중요한 이유는 Base Branch에 따라 분석 대상 커밋의 범위가 완전히 달라지기 때문입니다.

### 결정 로직

```txt
버전 파라미터: v1.2.0
       │
       ▼
┌─────────────────────────────────────┐
│ origin/release/1.0 브랜치 존재?    │
└─────────────────────────────────────┘
       │
       ├── Yes ──▶ Base: origin/release/1.0
       │           Target: HEAD
       │
       └── No ───▶ Base: 초기 커밋 (첫 배포)
                   Target: HEAD
```

브랜치 존재 여부는 다음 명령어로 확인합니다.

```bash
# release 브랜치 존재 확인
git rev-parse --verify origin/release/1.0
```

### 시나리오 1: 후속 릴리스

`origin/release/1.0` 브랜치가 존재하면, 이전 릴리스 이후의 변경사항만 분석합니다.

```txt
비교 범위:
  Base: origin/release/1.0
  Target: HEAD
  버전: v1.2.0
```

### 시나리오 2: 첫 번째 릴리스

브랜치가 없으면 첫 배포로 판단하고, 저장소의 모든 커밋을 분석 대상으로 삼습니다.

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

## 환경 검증 실패 처리

환경 검증에 실패하면 명확한 오류 메시지를 출력하고 **즉시 중단합니다.** 각 오류가 발생하는 상황과 해결 방법을 살펴보겠습니다.

### 오류 1: Git 저장소 아님

프로젝트 루트가 아닌 다른 디렉터리에서 명령을 실행했을 때 발생합니다.

```txt
오류: Git 저장소가 아닙니다

현재 디렉터리에서 'git status'를 실행할 수 없습니다.
Git 저장소 루트 디렉터리에서 명령을 실행하십시오.
```

해결하려면 프로젝트 루트로 이동합니다.

```bash
cd /path/to/your/project
```

### 오류 2: .NET SDK 없음

.NET 10 SDK가 설치되지 않았거나 PATH에 등록되지 않았을 때 발생합니다. Phase 2의 C# 스크립트와 API 추출 모두 .NET SDK에 의존합니다.

```txt
오류: .NET 10 SDK가 필요합니다

'dotnet --version' 명령을 실행할 수 없습니다.

설치 방법:
  https://dotnet.microsoft.com/download/dotnet/10.0
```

### 오류 3: 스크립트 디렉터리 없음

`.release-notes/scripts` 디렉터리가 없다는 것은 릴리스 노트 자동화가 설정되지 않은 프로젝트이거나, 잘못된 디렉터리에서 실행한 것입니다.

```txt
오류: 릴리스 노트 스크립트를 찾을 수 없습니다

'.release-notes/scripts' 디렉터리가 존재하지 않습니다.
프로젝트 루트 디렉터리에서 명령을 실행하십시오.
```

### 오류 4: 버전 파라미터 없음

명령을 실행할 때 버전을 지정하지 않았을 때 발생합니다.

```txt
오류: 버전 파라미터가 필요합니다

사용법:
  /release-note v1.2.0        # 정규 릴리스
  /release-note v1.0.0        # 첫 배포
  /release-note v1.2.0-beta.1 # 프리릴리스
```

## 콘솔 출력 형식

환경 검증이 성공적으로 완료되면, 확인된 전제조건과 결정된 비교 범위가 출력됩니다.

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

## FAQ

### Q1: 첫 배포와 후속 배포에서 Base Branch가 달라지면 분석 범위는 어떻게 달라지나요?
**A**: 첫 배포에서는 초기 커밋(`git rev-list --max-parents=0 HEAD`)부터 현재(`HEAD`)까지 **전체 히스토리를** 분석합니다. 후속 배포에서는 이전 릴리스 브랜치(`origin/release/1.0`)부터 현재까지 **변경분만** 분석합니다. 첫 배포는 프로젝트의 모든 기능을 문서화하고, 후속 배포는 새로 추가된 변경사항만 다룹니다.

### Q2: 환경 검증에서 실패하면 왜 즉시 중단하나요?
**A**: **"실패를 빨리 발견하라"는 원칙을** 따른 것입니다. Git 저장소가 아니거나, .NET SDK가 없거나, 스크립트 디렉터리가 없으면 이후 Phase 2~5가 모두 실패합니다. 수분간 스크립트를 실행한 뒤 실패하는 것보다, 10초 안에 문제를 발견하고 해결하는 것이 훨씬 효율적입니다.

### Q3: release 브랜치 이름이 `origin/release/1.0`이 아닌 다른 형식이면 어떻게 하나요?
**A**: `release-note.md` Command의 Phase 1 가이드(`phase1-setup.md`)에서 Base Branch 결정 로직을 수정하면 됩니다. 또는 스크립트를 수동으로 실행할 때 `--base` 옵션으로 원하는 브랜치를 직접 지정할 수 있습니다: `dotnet AnalyzeAllComponents.cs --base origin/main --target HEAD`.

환경 검증이 완료되면, 결정된 Base/Target 범위를 가지고 [Phase 2: 데이터 수집](02-phase2-collection.md)으로 진행합니다.
