---
title: "문제 해결 가이드"
---

릴리스 노트 자동화를 실행하다 보면 예상치 못한 오류를 만날 수 있습니다. 대부분의 문제는 환경 설정, 스크립트 실행, Phase 진행 중 하나에서 발생하며, 원인을 이해하면 빠르게 해결할 수 있습니다.

이 절에서는 각 문제가 **왜** 발생하는지 근본 원인을 설명하고, 그에 맞는 해결 방법을 안내합니다.

## 환경 관련 문제

환경 문제는 Phase 1에서 가장 먼저 드러납니다. 스크립트가 실행되기 전에 필요한 도구나 경로가 갖춰져 있지 않을 때 발생합니다.

### .NET SDK 없음

```txt
오류: .NET 10 SDK가 필요합니다
'dotnet --version' 명령을 실행할 수 없습니다.
```

릴리스 노트 스크립트는 .NET 10의 File-based App 기능을 사용합니다. 이 기능은 .NET 10에서 처음 도입되었기 때문에 이전 버전의 SDK로는 `.cs` 파일을 직접 실행할 수 없습니다.

**해결:**
1. .NET 10 SDK 설치: https://dotnet.microsoft.com/download/dotnet/10.0
2. 환경 변수 PATH 확인
3. 터미널 재시작 후 확인:
   ```bash
   dotnet --version
   ```

### Git 저장소 아님

```txt
오류: Git 저장소가 아닙니다
```

릴리스 노트 생성은 Git 커밋 히스토리를 분석하는 것에서 시작합니다. `.git` 디렉터리가 없는 곳에서 명령을 실행하면 커밋을 읽을 수 없으므로 이 오류가 발생합니다.

**해결:**
```bash
# Git 저장소 루트로 이동
cd /path/to/your/project

# Git 상태 확인
git status
```

### 스크립트 디렉터리 없음

```txt
오류: 릴리스 노트 스크립트를 찾을 수 없습니다
'.release-notes/scripts' 디렉터리가 존재하지 않습니다.
```

Phase 2에서 실행할 C# 스크립트들이 `.release-notes/scripts/` 폴더에 있어야 합니다. 이 폴더가 없다면 저장소를 처음 클론할 때 빠졌거나, 브랜치가 다를 수 있습니다.

**해결:**
```bash
# 프로젝트 루트에서 확인
ls -la .release-notes/scripts/

# 디렉터리가 없으면 저장소에서 가져오기
git checkout origin/main -- .release-notes/
```

## 스크립트 실행 문제

환경은 정상이지만 스크립트 자체가 실행에 실패하는 경우입니다. NuGet 패키지 문제, 파일 잠금, 빌드 오류가 주요 원인입니다.

### NuGet 패키지 복원 실패

```txt
error: Unable to resolve package 'System.CommandLine@2.0.1'
```

File-based App은 실행 시점에 NuGet 패키지를 자동으로 복원합니다. 네트워크 문제나 캐시 손상으로 패키지 다운로드가 실패하면 이 오류가 나타납니다.

**해결:**
```bash
# NuGet 캐시 정리
dotnet nuget locals all --clear

# 재시도
dotnet AnalyzeAllComponents.cs --base HEAD~10 --target HEAD
```

### 파일 잠금 오류

```txt
The process cannot access the file because it is being used by another process.
```

이전 스크립트 실행이 비정상 종료되면서 `.analysis-output/` 폴더의 파일을 잡고 있는 dotnet 프로세스가 남아 있을 수 있습니다. Windows에서 특히 자주 발생합니다.

**해결:**
```bash
# Windows에서 dotnet 프로세스 종료
taskkill /F /IM dotnet.exe

# .analysis-output 폴더 삭제
rm -rf .release-notes/scripts/.analysis-output/

# 재시도
```

### 빌드 실패

```txt
error CS1002: ; expected
Build FAILED.
```

API 추출 스크립트(`ExtractApiChanges.cs`)는 프로젝트를 빌드해서 DLL을 분석합니다. 프로젝트 코드에 컴파일 오류가 있으면 DLL이 생성되지 않아 API 추출도 실패합니다.

**해결:**
```bash
# 프로젝트 빌드 먼저 확인
dotnet build -c Release

# 빌드 오류 수정 후 재시도
```

## Phase별 문제

5-Phase 워크플로우 진행 중 특정 Phase에서 발생하는 문제들입니다. 어떤 Phase에서 멈췄는지가 원인 파악의 첫 번째 단서가 됩니다.

### Phase 1: Base Branch 없음

```txt
Base branch origin/release/1.0 does not exist
```

후속 릴리스에서는 이전 릴리스 브랜치를 Base로 사용합니다. 첫 배포에서는 이 브랜치가 아직 존재하지 않으므로 이 오류가 나타날 수 있습니다. 명령어가 자동으로 초기 커밋을 Base로 설정하지만, 수동 실행 시에는 직접 지정해야 합니다.

**해결 (첫 배포):**
```bash
# 초기 커밋부터 분석
cd .release-notes/scripts
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

**해결 (다른 브랜치 사용):**
```bash
dotnet AnalyzeAllComponents.cs --base origin/main --target HEAD
```

### Phase 2: API 추출 실패

```txt
API 추출 실패: ExtractApiChanges.cs
DLL not found
```

`ExtractApiChanges.cs`는 빌드된 DLL에서 Public API를 추출합니다. Release 빌드가 아직 수행되지 않았거나 빌드 출력 경로가 다르면 DLL을 찾지 못합니다.

**해결:**
```bash
# 프로젝트 빌드
dotnet build -c Release

# 빌드 출력 확인
ls Src/Functorium/bin/Release/net10.0/

# API 추출 재시도
dotnet ExtractApiChanges.cs
```

### Phase 3: 분석 파일 없음

```txt
분석 파일을 찾을 수 없습니다
.analysis-output/*.md 파일이 없습니다
```

Phase 3은 Phase 2의 출력물(컴포넌트 분석 파일)을 입력으로 사용합니다. Phase 2가 실패했거나 출력 폴더가 비어 있으면 Phase 3이 진행되지 않습니다.

**해결:**
```bash
# Phase 2 재실행
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
dotnet ExtractApiChanges.cs

# 파일 확인
ls .analysis-output/
```

### Phase 4: Uber 파일 없음

```txt
Uber 파일을 찾을 수 없습니다
all-api-changes.txt가 없습니다
```

Uber 파일은 프로젝트의 모든 Public API를 담고 있으며, Phase 4에서 릴리스 노트의 API 섹션을 작성할 때와 Phase 5에서 정확성을 검증할 때 사용합니다. `ExtractApiChanges.cs`가 이 파일을 생성하므로, 해당 스크립트를 먼저 실행해야 합니다.

**해결:**
```bash
# ExtractApiChanges.cs 실행
cd .release-notes/scripts
dotnet ExtractApiChanges.cs

# 확인
cat .analysis-output/api-changes-build-current/all-api-changes.txt
```

### Phase 5: 검증 실패

```txt
Phase 5: 검증 실패
API 정확성 (2 오류)
```

릴리스 노트에 기술된 API 시그니처가 실제 Uber 파일의 내용과 다를 때 발생합니다. Claude가 문서를 작성하면서 API 이름이나 파라미터를 약간 다르게 기술한 경우입니다.

**해결:**
1. 오류 메시지에서 문제 API 확인
2. Uber 파일에서 올바른 시그니처 검색:
   ```bash
   grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
   ```
3. 릴리스 노트 수정
4. 검증 재실행

## Claude Code 관련 문제

워크플로우를 실행하는 Claude Code 자체에서 발생하는 문제입니다.

### 명령어 인식 안됨

```txt
Command not found: /release-note
```

Claude Code의 커스텀 명령어는 `.claude/commands/` 폴더에 정의됩니다. 이 폴더나 `release-note.md` 파일이 없으면 명령어를 인식하지 못합니다.

**해결:**
```bash
# .claude/commands/ 폴더 확인
ls .claude/commands/

# release-note.md 파일 존재 확인
cat .claude/commands/release-note.md
```

### 버전 파라미터 누락

```txt
오류: 버전 파라미터가 필요합니다
```

`/release-note` 명령어는 버전 문자열을 필수 인자로 받습니다.

**해결:**
```bash
# 올바른 사용법
> /release-note v1.0.0
```

### 컨텍스트 초과

대규모 프로젝트에서는 분석할 커밋과 파일이 많아서 Claude의 컨텍스트 윈도우를 초과할 수 있습니다. 응답이 중간에 멈추거나 불완전한 결과가 나타나는 증상으로 드러납니다.

**해결:**
- 대화를 나누어 진행
- Phase별로 따로 요청
- 새 대화 시작

## 출력 파일 문제

### 한글 깨짐

릴리스 노트에서 한글이 깨져 보인다면 파일 인코딩 문제입니다. 생성된 파일이 UTF-8이 아닌 다른 인코딩으로 저장되었을 수 있습니다.

**해결:**
- 파일을 UTF-8 인코딩으로 저장
- 편집기 인코딩 설정 확인

### Markdown 렌더링 오류

코드 블록이나 테이블이 제대로 표시되지 않는다면 Markdown 문법 오류입니다. 백틱 개수 불일치, 테이블 정렬 문자 누락 등이 흔한 원인입니다.

**해결:**
```bash
# Markdown lint 실행
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-v1.0.0.md

# 오류 수정
```

## 일반 트러블슈팅 체크리스트

위의 개별 문제로 해결되지 않을 때는 다음 체크리스트를 순서대로 진행해봅시다. 가장 기본적인 환경부터 확인하고, 캐시를 정리하고, 스크립트를 개별적으로 실행해보는 접근입니다. 대부분의 문제는 이 다섯 단계 안에서 원인이 드러납니다.

1. **환경 확인**
   ```bash
   dotnet --version    # .NET 10.x?
   git status          # Git 저장소?
   pwd                 # 올바른 디렉터리?
   ```

2. **캐시 정리**
   ```bash
   dotnet nuget locals all --clear
   rm -rf .release-notes/scripts/.analysis-output/
   ```

3. **프로젝트 빌드**
   ```bash
   dotnet build -c Release
   ```

4. **스크립트 개별 실행**
   ```bash
   cd .release-notes/scripts
   dotnet AnalyzeAllComponents.cs --help
   dotnet ExtractApiChanges.cs --help
   ```

5. **로그 확인**
   - 오류 메시지 전체 읽기
   - 스택 트레이스 확인
   - 생성된 파일 내용 확인

## 도움 받기

체크리스트로도 해결되지 않는 문제라면 다음 문서를 참조하거나 이슈를 등록해주세요.

- `.release-notes/scripts/docs/README.md` - 스크립트 문서
- `.claude/commands/release-note.md` - 명령어 정의
- GitHub Issues: https://github.com/your-repo/issues

## FAQ

### Q1: "파일 잠금 오류"가 Windows에서 특히 자주 발생하는 이유는 무엇인가요?
**A**: Windows는 파일을 열고 있는 프로세스가 있으면 **다른 프로세스가 해당 파일을 삭제하거나 덮어쓸 수 없는** 배타적 잠금 정책을 사용합니다. 이전 스크립트 실행이 비정상 종료되면 dotnet 프로세스가 `.analysis-output/` 파일을 잡고 있는 채로 남을 수 있어, `taskkill /F /IM dotnet.exe`로 프로세스를 종료해야 합니다.

### Q2: Phase 5 검증에서 API 정확성 오류가 나왔을 때, Uber 파일과 릴리스 노트 중 어느 쪽을 수정해야 하나요?
**A**: **릴리스 노트를 수정해야 합니다.** Uber 파일은 실제 빌드된 DLL에서 추출한 것이므로 진실의 원천(Single Source of Truth)입니다. 릴리스 노트에 기술된 API 시그니처가 Uber 파일과 다르다면, 릴리스 노트의 표기가 잘못된 것입니다. `grep`으로 Uber 파일에서 올바른 시그니처를 확인한 뒤 릴리스 노트를 수정하세요.

### Q3: `dotnet nuget locals all --clear`는 어떤 캐시를 정리하나요?
**A**: HTTP 캐시, 글로벌 패키지 폴더, 임시 폴더 등 **NuGet이 사용하는 모든 로컬 캐시를** 삭제합니다. File-based App은 실행 시 NuGet 패키지를 자동 복원하므로, 캐시가 손상되면 패키지 복원에 실패할 수 있습니다. 정리 후 다음 실행에서 패키지를 다시 다운로드합니다.

### Q4: 컨텍스트 초과 문제가 발생하면 어떻게 대응해야 하나요?
**A**: 대규모 프로젝트에서 커밋과 파일이 많으면 Claude의 컨텍스트 윈도우를 초과할 수 있습니다. **Phase별로 나누어 요청하는 것이** 가장 효과적입니다. 예를 들어 먼저 Phase 2 데이터 수집만 실행하고, 새 대화에서 수집된 데이터를 기반으로 Phase 3~5를 진행하면 컨텍스트 부담을 분산할 수 있습니다.

실습 튜토리얼의 본문은 여기까지입니다. 다음 절에서는 릴리스 노트 생성에 필요한 명령어, 워크플로우, 출력 파일을 한곳에 정리한 빠른 참조 가이드를 제공합니다.

- [빠른 참조](04-quick-reference.md)
