# Functorium 릴리스 노트 생성 문서

> **마지막 업데이트**: 2025-12-19

이 디렉터리는 전문적인 Functorium 릴리스 노트를 생성하기 위한 모듈화된 문서를 포함합니다.

## 문서 구조 (5-Phase 워크플로우)

| Phase | 문서 | 설명 |
|-------|------|------|
| - | [TEMPLATE.md](../../TEMPLATE.md) | 릴리스 노트 템플릿 (복사용) |
| 1 | [phase1-setup.md](phase1-setup.md) | 환경 검증 및 준비 |
| 2 | [phase2-collection.md](phase2-collection.md) | 데이터 수집 (컴포넌트/API 분석) |
| 3 | [phase3-analysis.md](phase3-analysis.md) | 커밋 분석 및 기능 추출 |
| 4 | [phase4-writing.md](phase4-writing.md) | 릴리스 노트 작성 규칙 |
| 5 | [phase5-validation.md](phase5-validation.md) | 검증 및 품질 확인 |

## 스크립트 구조

| 스크립트 | 설명 |
|----------|------|
| `AnalyzeAllComponents.cs` | 컴포넌트 변경사항 분석<br/>• Base branch 유효성 검사<br/>• 첫 배포 시나리오 자동 감지 및 안내 |
| `AnalyzeFolder.cs` | 개별 폴더 상세 분석 |
| `ExtractApiChanges.cs` | API 변경사항 추출 |
| `ApiGenerator.cs` | Public API 생성 |
| `SummarizeSlowestTests.cs` | TRX 테스트 결과 요약<br/>• 느린 테스트 식별 (기본 30초 이상)<br/>• 실패 테스트 목록<br/>• 테스트 통계 및 분포 |

> **Note**: 모든 스크립트는 .NET 10 file-based program으로 구현되어 있습니다.
>
> **AnalyzeAllComponents.cs의 자동 검증**:
> 스크립트는 base branch의 존재 여부를 자동으로 확인하고,
> 브랜치가 없으면 첫 배포를 위한 상세한 안내를 제공합니다.

## 빠른 시작

### 1. 데이터 수집

#### 첫 번째 실행 시

`AnalyzeAllComponents.cs`는 기본적으로 `origin/release/1.0` 브랜치를 base로 사용합니다.
처음 실행 시 이 브랜치가 없으면 자동으로 안내 메시지가 표시됩니다:

```bash
# 기본 실행 (브랜치가 없으면 안내 메시지 표시)
dotnet AnalyzeAllComponents.cs
```

안내 메시지에 따라 다음 중 하나를 선택하세요:

**옵션 1: 릴리스 브랜치 생성 (권장)**
```bash
# 릴리스 브랜치 생성 및 푸시
git checkout -b release/1.0
git push -u origin release/1.0

# 이후 분석 실행
dotnet AnalyzeAllComponents.cs
```

**옵션 2: 초기 커밋부터 분석 (첫 배포 시)**
```bash
# 초기 커밋부터 현재까지 분석
dotnet AnalyzeAllComponents.cs --base $(git rev-list --max-parents=0 HEAD) --target HEAD
dotnet ExtractApiChanges.cs
```

**옵션 3: 다른 base 브랜치 사용**
```bash
# origin/main을 base로 사용
dotnet AnalyzeAllComponents.cs --base origin/main --target HEAD

# 특정 커밋 해시 사용
dotnet AnalyzeAllComponents.cs --base <commit-hash> --target HEAD
```

#### 정규 릴리스 간 비교

```bash
# 릴리스 간 비교 (릴리스 브랜치가 존재하는 경우)
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target origin/main
dotnet ExtractApiChanges.cs
```

#### 테스트 결과 요약

```bash
# 기본 실행 (TestResults/**/*.trx 파일 검색, 30초 이상 테스트 식별)
dotnet SummarizeSlowestTests.cs

# 느린 테스트 기준 변경 (예: 60초 이상)
dotnet SummarizeSlowestTests.cs --threshold 60

# 특정 glob 패턴으로 TRX 파일 검색
dotnet SummarizeSlowestTests.cs "**/MyTests/**/*.trx"

# 옵션 조합
dotnet SummarizeSlowestTests.cs "**/TestResults/**/*.trx" -t 45
```

### 2. 출력 확인

```
.analysis-output/
├── analysis-summary.md              # 컴포넌트 분석 요약
├── Functorium.md                    # Src/Functorium 분석
├── Functorium.Testing.md            # Src/Functorium.Testing 분석
├── Docs.md                          # Docs 분석
├── test-summary.md                  # 테스트 결과 요약 (SummarizeSlowestTests.cs)
├── api-changes-build-current/
│   ├── all-api-changes.txt          # Uber API 파일 (단일 진실 소스)
│   ├── api-changes-summary.md       # API 요약
│   └── api-changes-diff.txt         # API 차이점 (Breaking Changes 감지)
└── work/                            # 중간 결과 저장 (Phase별)
    ├── phase3-commit-analysis.md    # 커밋 분류 및 우선순위
    ├── phase3-feature-groups.md     # 기능 그룹화 결과
    ├── phase3-api-mapping.md        # API와 커밋 매핑
    ├── phase4-draft.md              # 릴리스 노트 초안
    ├── phase4-api-references.md     # 사용된 API 목록
    ├── phase4-code-samples.md       # 모든 코드 샘플
    ├── phase5-validation-report.md  # 검증 결과 보고서
    └── phase5-api-validation.md     # API 검증 상세

Src/
├── Functorium/.api/
│   └── Functorium.cs                # Functorium Public API
└── Functorium.Testing/.api/
    └── Functorium.Testing.cs        # Functorium.Testing Public API
```

### 3. 릴리스 노트 작성

5-Phase 워크플로우를 따라 진행합니다:

1. [phase1-setup.md](phase1-setup.md) - 환경 검증 및 비교 범위 결정
2. [phase2-collection.md](phase2-collection.md) - 데이터 수집 (컴포넌트/API 분석)
3. [phase3-analysis.md](phase3-analysis.md) - 커밋 분석 및 기능 추출
4. [phase4-writing.md](phase4-writing.md) - 릴리스 노트 작성
   - **템플릿**: `.release-notes/TEMPLATE.md`를 복사하여 시작
5. [phase5-validation.md](phase5-validation.md) - 검증

## 핵심 원칙

> **정확성 우선**: 모든 문서화된 API는 Uber 파일에 존재해야 합니다.
> - API를 임의로 만들어내지 않습니다
> - 모든 기능은 커밋/PR로 추적 가능해야 합니다
> - 코드 샘플은 `all-api-changes.txt`에서 검증합니다

> **Breaking Changes는 Git Diff로 자동 감지합니다.**
> - `.api` 폴더의 Git diff 분석 우선 (`api-changes-diff.txt`)
> - 커밋 메시지 패턴은 보조 수단 (`!:`, `breaking`)
> - 삭제/변경된 API는 모두 Breaking Change로 처리
> - 상세 내용: [phase3-analysis.md#breaking-changes-감지](phase3-analysis.md#breaking-changes-감지)

## 설정

컴포넌트 분석 대상은 `Config/component-priority.json`에서 설정합니다:

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs"
  ]
}
```

## 트러블슈팅

### C# 10 File-based 프로그램 실행 문제

#### 1. .NET SDK 버전 오류

**증상:**
```
error CS8652: The feature 'top-level statements' is not available in C# 9.0
```

**해결 방법:**
```bash
# .NET 버전 확인
dotnet --version

# .NET 10 SDK가 설치되어 있는지 확인
dotnet --list-sdks

# .NET 10 SDK 설치 필요
# https://dotnet.microsoft.com/download/dotnet/10.0
```

#### 2. 패키지 참조 오류

**증상:**
```
error CS0246: The type or namespace name 'Spectre' could not be found
error CS0246: The type or namespace name 'CommandLine' could not be found
```

**원인:** `#:package` 지시어가 인식되지 않거나 패키지 다운로드 실패

**해결 방법:**
```bash
# NuGet 캐시 정리
dotnet nuget locals all --clear

# 스크립트 재실행 (패키지 자동 다운로드)
dotnet AnalyzeAllComponents.cs

# 또는 수동 패키지 복원
dotnet restore
```

#### 3. 실행 권한 문제 (Linux/macOS)

**증상:**
```
permission denied: ./AnalyzeAllComponents.cs
```

**해결 방법:**
```bash
# 실행 권한 부여
chmod +x AnalyzeAllComponents.cs
chmod +x ExtractApiChanges.cs

# shebang으로 직접 실행
./AnalyzeAllComponents.cs

# 또는 dotnet 명령 사용
dotnet AnalyzeAllComponents.cs
```

#### 4. Git 명령 오류

**증상:**
```
fatal: not a git repository
fatal: ambiguous argument 'origin/release/1.0..origin/main'
```

**해결 방법:**
```bash
# Git 저장소 확인
git status

# 원격 브랜치 가져오기
git fetch origin

# 브랜치 존재 확인
git branch -r

# 올바른 브랜치로 실행
dotnet AnalyzeAllComponents.cs --base <existing-branch> --target HEAD
```

#### 5. 작업 디렉터리 문제

**증상:**
```
Could not find file 'Config/component-priority.json'
Could not find directory '.analysis-output'
```

**해결 방법:**
```bash
# 반드시 .release-notes/scripts 디렉터리에서 실행
cd .release-notes/scripts

# 현재 위치 확인
pwd  # Linux/macOS
cd   # Windows

# Config 디렉터리 확인
ls Config/component-priority.json  # Linux/macOS
dir Config\component-priority.json # Windows
```

#### 6. Windows 경로 구분자 문제

**증상:**
```
DirectoryNotFoundException: Could not find a part of the path
```

**해결 방법:**
스크립트가 경로를 자동으로 처리하지만, 수동 입력 시:
```bash
# Windows에서 슬래시 사용
dotnet AnalyzeAllComponents.cs --base origin/release/1.0

# 또는 백슬래시 이스케이프
dotnet AnalyzeAllComponents.cs --base "origin\\release\\1.0"
```

#### 7. 메모리 부족 (대규모 저장소)

**증상:**
```
System.OutOfMemoryException
```

**해결 방법:**
```bash
# Git 이력 제한
git config diff.renameLimit 1000

# 또는 특정 커밋 범위만 분석
dotnet AnalyzeAllComponents.cs --base HEAD~100 --target HEAD
```

#### 8. 출력 파일 접근 거부

**증상:**
```
System.UnauthorizedAccessException: Access to the path '.analysis-output' is denied
System.IO.IOException: The process cannot access the file because it is being used by another process
```

**해결 방법:**
```bash
# .analysis-output 폴더 삭제 후 재실행
rm -rf .analysis-output  # Linux/macOS
rmdir /s /q .analysis-output  # Windows

# 또는 관리자 권한으로 실행
sudo dotnet AnalyzeAllComponents.cs  # Linux/macOS
```

#### 9. 파일 잠금 문제 (프로세스가 파일 사용 중)

**증상:**
```
System.IO.IOException: The process cannot access the file because it is being used by another process
Cannot delete directory '.analysis-output': Directory not empty
```

**원인:** 다른 프로세스(예: 텍스트 에디터, 탐색기, dotnet 프로세스)가 파일을 사용 중

**해결 방법 - Windows:**
```powershell
# 1. 파일을 사용 중인 프로세스 확인
# PowerShell 사용
Get-Process | Where-Object {$_.Path -like "*dotnet*"}

# 또는 tasklist 사용
tasklist | findstr dotnet

# 2. 프로세스 강제 종료
# PowerShell
Stop-Process -Name "dotnet" -Force

# 또는 taskkill 사용
taskkill /F /IM dotnet.exe

# 3. 특정 PID 종료
taskkill /F /PID <process-id>

# 4. 리소스 모니터로 확인 (GUI)
# Win+R → resmon → CPU 탭 → "dotnet" 검색
```

**해결 방법 - Linux/macOS:**
```bash
# 1. 파일을 사용 중인 프로세스 확인
lsof | grep ".analysis-output"
lsof +D .analysis-output

# 또는 fuser 사용
fuser -v .analysis-output

# 2. dotnet 프로세스 확인
ps aux | grep dotnet

# 3. 프로세스 강제 종료
# SIGTERM 전송 (정상 종료)
kill <process-id>

# SIGKILL 전송 (강제 종료)
kill -9 <process-id>

# 또는 pkill 사용
pkill -f dotnet

# 4. 파일 잠금 해제
fuser -k .analysis-output  # 사용 중인 모든 프로세스 종료
```

#### 10. 캐시 문제 및 오래된 파일

**증상:**
```
error NU1301: Unable to load the service index
error MSB4018: The "ResolvePackageAssets" task failed unexpectedly
File or assembly name 'Spectre.Console', or one of its dependencies, was not found
```

**원인:** 손상되거나 오래된 NuGet 캐시, MSBuild 캐시

**해결 방법:**
```bash
# === .NET 관련 캐시 삭제 ===

# 1. NuGet 캐시 완전 삭제
dotnet nuget locals all --clear

# 개별 캐시 삭제
dotnet nuget locals http-cache --clear       # HTTP 캐시
dotnet nuget locals global-packages --clear  # 글로벌 패키지
dotnet nuget locals temp --clear             # 임시 캐시

# 2. MSBuild 캐시 삭제 (Windows)
# PowerShell
Remove-Item -Recurse -Force $env:LOCALAPPDATA\Microsoft\MSBuild

# 3. MSBuild 캐시 삭제 (Linux/macOS)
rm -rf ~/.local/share/NuGet
rm -rf ~/.nuget/packages

# 4. .NET 임시 파일 삭제 (Windows)
del /s /q %TEMP%\*.tmp
del /s /q %TEMP%\NuGet\*

# 5. .NET 임시 파일 삭제 (Linux/macOS)
rm -rf /tmp/NuGet
rm -rf /tmp/.dotnet

# === Git 관련 캐시 삭제 ===

# 6. Git 캐시 정리
git gc --aggressive --prune=now

# Git 인덱스 재구축
git rm -r --cached .
git add .

# === 프로젝트 캐시 삭제 ===

# 7. 로컬 출력 폴더 삭제
rm -rf .analysis-output     # Linux/macOS
rmdir /s /q .analysis-output  # Windows

# 8. bin/obj 폴더 삭제 (해당되는 경우)
find . -name "bin" -o -name "obj" | xargs rm -rf  # Linux/macOS
for /d /r . %d in (bin,obj) do @if exist "%d" rd /s /q "%d"  # Windows
```

**전체 초기화 스크립트 (Windows):**
```powershell
# cleanup.ps1
Write-Host "Cleaning NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear

Write-Host "Stopping dotnet processes..." -ForegroundColor Yellow
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue

Write-Host "Removing output directory..." -ForegroundColor Yellow
if (Test-Path ".analysis-output") {
    Remove-Item -Recurse -Force ".analysis-output"
}

Write-Host "Cleanup completed!" -ForegroundColor Green
```

**전체 초기화 스크립트 (Linux/macOS):**
```bash
#!/bin/bash
# cleanup.sh

echo "Cleaning NuGet cache..."
dotnet nuget locals all --clear

echo "Stopping dotnet processes..."
pkill -f dotnet || true

echo "Removing output directory..."
rm -rf .analysis-output

echo "Cleanup completed!"
```

### 디버깅 팁

#### 상세 오류 메시지 확인
```bash
# 스택 트레이스 포함 실행
dotnet AnalyzeAllComponents.cs 2>&1 | tee error.log
```

#### Git 명령 테스트
```bash
# 스크립트에서 사용하는 Git 명령 수동 테스트
git diff --name-status origin/release/1.0..origin/main -- "Src/Functorium/"
git log --oneline --no-merges origin/release/1.0..origin/main -- "Src/Functorium/"
```

#### 환경 확인
```bash
# 필수 도구 버전 확인
dotnet --version        # .NET 10.x 필요
git --version          # Git 2.x 이상 권장

# 환경 변수 확인 (Windows)
echo %PATH%

# 환경 변수 확인 (Linux/macOS)
echo $PATH
```

### 일반적인 해결 방법

1. **스크립트가 실행되지 않으면:**
   - .NET 10 SDK 설치 확인
   - `.release-notes/scripts` 디렉터리에서 실행
   - `dotnet` 명령 사용 (shebang 대신)

2. **Base branch 오류 시:**
   - 스크립트가 자동으로 안내 메시지 표시
   - 첫 배포 시나리오 옵션 참조
   - 존재하는 브랜치나 커밋 해시 사용

3. **패키지 오류 시:**
   - NuGet 캐시 정리
   - 인터넷 연결 확인
   - 방화벽/프록시 설정 확인

4. **출력 파일 문제 시:**
   - `.analysis-output` 폴더 수동 삭제
   - 파일 권한 확인
   - 디스크 공간 확인
