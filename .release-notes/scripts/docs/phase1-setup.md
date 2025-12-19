# Phase 1: 환경 검증 및 준비

## 목표

릴리스 노트 생성 전 필수 환경을 검증하고 비교 범위를 결정합니다.

## 전제조건 확인

다음 조건을 모두 확인하십시오:

### 1. Git 저장소 확인

```bash
git status
```

- 현재 디렉터리가 Git 저장소인지 확인
- Git이 설치되어 있는지 확인

### 2. 스크립트 디렉터리 확인

- `.release-notes/scripts` 디렉터리 존재 확인
- `config/component-priority.json` 파일 존재 확인

### 3. .NET SDK 확인

```bash
dotnet --version
```

- .NET 10.x 이상 필요
- 설치되지 않았으면 오류 메시지 출력

### 4. 버전 파라미터 검증

- 버전이 비어있지 않은지 확인
- 버전 형식이 유효한지 확인 (예: v1.2.0, v1.0.0-alpha.1)

## Base Branch 결정

릴리스 간 비교를 위한 base branch를 결정합니다.

### 기본 전략

1. `origin/release/1.0` 브랜치 존재 확인:

   ```bash
   git rev-parse --verify origin/release/1.0
   ```

2. **브랜치가 존재하는 경우:**
   - Base: `origin/release/1.0`
   - Target: `HEAD`

   ```
   릴리스 노트 생성 시작

   비교 범위:
     Base: origin/release/1.0
     Target: HEAD
     버전: v1.2.0
   ```

3. **브랜치가 없는 경우 (첫 배포):**
   - Base: 초기 커밋 (`git rev-list --max-parents=0 HEAD`)
   - Target: `HEAD`

   ```
   첫 배포로 감지되었습니다

   초기 커밋부터 분석합니다:
     Base: <initial-commit-sha>
     Target: HEAD
     버전: v1.0.0
   ```

## 환경 검증 실패 처리

환경 검증 실패 시 명확한 오류 메시지를 출력하고 중단합니다.

### Git 저장소 아님

```
오류: Git 저장소가 아닙니다

현재 디렉터리에서 'git status'를 실행할 수 없습니다.
Git 저장소 루트 디렉터리에서 명령을 실행하십시오.
```

### .NET SDK 없음

```
오류: .NET 10 SDK가 필요합니다

'dotnet --version' 명령을 실행할 수 없습니다.

설치 방법:
  https://dotnet.microsoft.com/download/dotnet/10.0
```

### 스크립트 디렉터리 없음

```
오류: 릴리스 노트 스크립트를 찾을 수 없습니다

'.release-notes/scripts' 디렉터리가 존재하지 않습니다.
프로젝트 루트 디렉터리에서 명령을 실행하십시오.
```

### 버전 파라미터 없음

```
오류: 버전 파라미터가 필요합니다

사용법:
  /release-note v1.2.0        # 정규 릴리스
  /release-note v1.0.0        # 첫 배포
  /release-note v1.2.0-beta.1 # 프리릴리스
```

## 콘솔 출력 형식

### 환경 검증 성공

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 1: 환경 검증 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

전제조건:
  ✓ Git 저장소
  ✓ .NET SDK 10.x
  ✓ 스크립트 디렉터리

비교 범위:
  Base: origin/release/1.0
  Target: HEAD
  버전: v1.2.0
```

### 첫 배포 감지

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 1: 환경 검증 완료 ✓
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

전제조건:
  ✓ Git 저장소
  ✓ .NET SDK 10.x
  ✓ 스크립트 디렉터리

첫 배포로 감지되었습니다

비교 범위:
  Base: abc1234 (초기 커밋)
  Target: HEAD
  버전: v1.0.0
```

## 성공 기준

- [ ] Git 저장소 확인됨
- [ ] .NET SDK 버전 확인됨 (10.x 이상)
- [ ] 스크립트 디렉터리 존재 확인됨
- [ ] 버전 파라미터 유효함
- [ ] Base/Target 브랜치 결정됨

## 다음 단계

환경 검증 완료 후 [Phase 2: 데이터 수집](phase2-collection.md)으로 진행합니다.
