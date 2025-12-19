# 7.3 문제 해결 가이드

> 이 절에서는 릴리스 노트 자동화 시스템 사용 중 발생할 수 있는 문제와 해결 방법을 알아봅니다.

---

## 환경 관련 문제

### .NET SDK 없음

**증상:**
```txt
오류: .NET 10 SDK가 필요합니다
'dotnet --version' 명령을 실행할 수 없습니다.
```

**해결:**
1. .NET 10 SDK 설치: https://dotnet.microsoft.com/download/dotnet/10.0
2. 환경 변수 PATH 확인
3. 터미널 재시작 후 확인:
   ```bash
   dotnet --version
   ```

### Git 저장소 아님

**증상:**
```txt
오류: Git 저장소가 아닙니다
```

**해결:**
```bash
# Git 저장소 루트로 이동
cd /path/to/your/project

# Git 상태 확인
git status
```

### 스크립트 디렉터리 없음

**증상:**
```txt
오류: 릴리스 노트 스크립트를 찾을 수 없습니다
'.release-notes/scripts' 디렉터리가 존재하지 않습니다.
```

**해결:**
```bash
# 프로젝트 루트에서 확인
ls -la .release-notes/scripts/

# 디렉터리가 없으면 저장소에서 가져오기
git checkout origin/main -- .release-notes/
```

---

## 스크립트 실행 문제

### NuGet 패키지 복원 실패

**증상:**
```txt
error: Unable to resolve package 'System.CommandLine@2.0.1'
```

**해결:**
```bash
# NuGet 캐시 정리
dotnet nuget locals all --clear

# 재시도
dotnet AnalyzeAllComponents.cs --base HEAD~10 --target HEAD
```

### 파일 잠금 오류

**증상:**
```txt
The process cannot access the file because it is being used by another process.
```

**해결:**
```bash
# Windows에서 dotnet 프로세스 종료
taskkill /F /IM dotnet.exe

# .analysis-output 폴더 삭제
rm -rf .release-notes/scripts/.analysis-output/

# 재시도
```

### 빌드 실패

**증상:**
```txt
error CS1002: ; expected
Build FAILED.
```

**해결:**
```bash
# 프로젝트 빌드 먼저 확인
dotnet build -c Release

# 빌드 오류 수정 후 재시도
```

---

## Phase별 문제

### Phase 1: Base Branch 없음

**증상:**
```txt
Base branch origin/release/1.0 does not exist
```

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

**증상:**
```txt
API 추출 실패: ExtractApiChanges.cs
DLL not found
```

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

**증상:**
```txt
분석 파일을 찾을 수 없습니다
.analysis-output/*.md 파일이 없습니다
```

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

**증상:**
```txt
Uber 파일을 찾을 수 없습니다
all-api-changes.txt가 없습니다
```

**해결:**
```bash
# ExtractApiChanges.cs 실행
cd .release-notes/scripts
dotnet ExtractApiChanges.cs

# 확인
cat .analysis-output/api-changes-build-current/all-api-changes.txt
```

### Phase 5: 검증 실패

**증상:**
```txt
Phase 5: 검증 실패
API 정확성 (2 오류)
```

**해결:**
1. 오류 메시지에서 문제 API 확인
2. Uber 파일에서 올바른 시그니처 검색:
   ```bash
   grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
   ```
3. 릴리스 노트 수정
4. 검증 재실행

---

## Claude Code 관련 문제

### 명령어 인식 안됨

**증상:**
```txt
Command not found: /release-note
```

**해결:**
```bash
# .claude/commands/ 폴더 확인
ls .claude/commands/

# release-note.md 파일 존재 확인
cat .claude/commands/release-note.md
```

### 버전 파라미터 누락

**증상:**
```txt
오류: 버전 파라미터가 필요합니다
```

**해결:**
```bash
# 올바른 사용법
> /release-note v1.0.0
```

### 컨텍스트 초과

**증상:**
Claude가 응답 중간에 멈추거나 불완전한 결과

**해결:**
- 대화를 나누어 진행
- Phase별로 따로 요청
- 새 대화 시작

---

## 출력 파일 문제

### 한글 깨짐

**증상:**
릴리스 노트에서 한글이 깨져 보임

**해결:**
- 파일을 UTF-8 인코딩으로 저장
- 편집기 인코딩 설정 확인

### Markdown 렌더링 오류

**증상:**
코드 블록이나 테이블이 제대로 표시 안됨

**해결:**
```bash
# Markdown lint 실행
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-v1.0.0.md

# 오류 수정
```

---

## 일반 트러블슈팅 체크리스트

문제 발생 시 순서대로 확인:

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

---

## 도움 받기

### 문서 참조

- `.release-notes/scripts/docs/README.md` - 스크립트 문서
- `.claude/commands/release-note.md` - 명령어 정의

### 문의

- GitHub Issues: https://github.com/your-repo/issues
- 프로젝트 README.md

---

## 7장 완료

실습 튜토리얼을 완료했습니다!

### 배운 내용

- `/release-note` 명령어로 릴리스 노트 생성
- .NET 10 File-based App 직접 작성
- 문제 해결 방법

---

## 다음 단계

- [부록 A: 용어 사전](../appendix/A-glossary.md)
