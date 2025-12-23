# 부록 C: 참고 자료

> 이 부록에서는 추가 학습을 위한 참고 자료와 링크를 정리합니다.

---

## 공식 문서

### .NET

| 자료 | URL | 설명 |
|------|-----|------|
| .NET 공식 문서 | https://learn.microsoft.com/dotnet/ | Microsoft 공식 .NET 문서 |
| .NET 10 새 기능 | https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10 | .NET 10 릴리스 노트 |
| C# 언어 가이드 | https://learn.microsoft.com/dotnet/csharp/ | C# 문법 및 기능 |

### System.CommandLine

| 자료 | URL | 설명 |
|------|-----|------|
| 공식 문서 | https://learn.microsoft.com/dotnet/standard/commandline/ | CLI 파싱 가이드 |
| GitHub 저장소 | https://github.com/dotnet/command-line-api | 소스 코드 및 이슈 |
| 샘플 코드 | https://github.com/dotnet/command-line-api/tree/main/samples | 예제 모음 |

### Spectre.Console

| 자료 | URL | 설명 |
|------|-----|------|
| 공식 사이트 | https://spectreconsole.net/ | 문서 및 데모 |
| GitHub 저장소 | https://github.com/spectreconsole/spectre.console | 소스 코드 |
| API 레퍼런스 | https://spectreconsole.net/api/ | 전체 API 문서 |

### Claude Code

| 자료 | URL | 설명 |
|------|-----|------|
| Claude 공식 사이트 | https://www.anthropic.com/claude | Anthropic Claude |
| Claude Code 문서 | https://docs.anthropic.com/ | 공식 문서 |

---

## Git 관련

### Conventional Commits

| 자료 | URL | 설명 |
|------|-----|------|
| 공식 스펙 | https://www.conventionalcommits.org/ | 표준 명세 |
| 한국어 번역 | https://www.conventionalcommits.org/ko/v1.0.0/ | 한국어 문서 |

### Git 학습

| 자료 | URL | 설명 |
|------|-----|------|
| Pro Git 책 | https://git-scm.com/book/ko/v2 | 무료 온라인 책 (한국어) |
| Git 공식 문서 | https://git-scm.com/doc | 레퍼런스 매뉴얼 |
| Learn Git Branching | https://learngitbranching.js.org/?locale=ko | 대화형 학습 |

---

## NuGet 패키지

### 이 도서에서 사용한 패키지

| 패키지 | 버전 | NuGet 링크 |
|--------|------|-----------|
| System.CommandLine | 2.0.1 | https://www.nuget.org/packages/System.CommandLine |
| Spectre.Console | 0.54.0 | https://www.nuget.org/packages/Spectre.Console |
| PublicApiGenerator | 11.1.0 | https://www.nuget.org/packages/PublicApiGenerator |

### 유용한 추가 패키지

| 패키지 | 용도 | NuGet 링크 |
|--------|------|-----------|
| Humanizer | 문자열 변환 | https://www.nuget.org/packages/Humanizer |
| FluentValidation | 유효성 검사 | https://www.nuget.org/packages/FluentValidation |
| Polly | 재시도 정책 | https://www.nuget.org/packages/Polly |
| Serilog | 로깅 | https://www.nuget.org/packages/Serilog |

---

## 관련 도구

### 개발 환경

| 도구 | URL | 설명 |
|------|-----|------|
| Visual Studio Code | https://code.visualstudio.com/ | 경량 코드 편집기 |
| Visual Studio | https://visualstudio.microsoft.com/ko/ | 통합 개발 환경 |
| JetBrains Rider | https://www.jetbrains.com/rider/ | 크로스 플랫폼 IDE |

### VS Code 확장

| 확장 | 용도 |
|------|------|
| C# Dev Kit | C# 개발 지원 |
| .NET Install Tool | .NET SDK 관리 |
| GitLens | Git 기능 확장 |
| Markdown All in One | Markdown 편집 |

---

## 학습 자료

### C# 입문

| 자료 | URL | 설명 |
|------|-----|------|
| C# 자습서 | https://learn.microsoft.com/dotnet/csharp/tour-of-csharp/ | Microsoft 공식 |
| C# 기초 | https://learn.microsoft.com/training/paths/csharp-first-steps/ | 무료 학습 경로 |

### CLI 도구 개발

| 자료 | URL | 설명 |
|------|-----|------|
| CLI 앱 만들기 | https://learn.microsoft.com/dotnet/standard/commandline/get-started-tutorial | 튜토리얼 |

### 릴리스 노트 작성

| 자료 | URL | 설명 |
|------|-----|------|
| Keep a Changelog | https://keepachangelog.com/ko/1.1.0/ | 변경 로그 작성 가이드 |
| Semantic Versioning | https://semver.org/lang/ko/ | 시맨틱 버저닝 명세 |

---

## 커뮤니티

### .NET 커뮤니티

| 커뮤니티 | URL | 설명 |
|----------|-----|------|
| .NET Foundation | https://dotnetfoundation.org/ | .NET 재단 |
| Stack Overflow | https://stackoverflow.com/questions/tagged/.net | Q&A |
| Reddit r/dotnet | https://www.reddit.com/r/dotnet/ | 토론 |

### 한국 커뮤니티

| 커뮤니티 | URL | 설명 |
|----------|-----|------|
| KODNOT | https://forum.dotnetdev.kr/ | 한국 .NET 개발자 포럼 |

---

## 이 프로젝트 관련

### Functorium

| 자료 | 설명 |
|------|------|
| `README.md` | 프로젝트 소개 |
| `.claude/commands/` | Claude 사용자 정의 Command |
| `.release-notes/scripts/` | C# 분석 스크립트 |
| `.release-notes/TEMPLATE.md` | 릴리스 노트 템플릿 |

### 폴더 구조

```txt
Functorium/
├── .claude/
│   └── commands/
│       ├── release-note.md      # 메인 Command
│       └── commit.md            # 보조 Command
├── .release-notes/
│   ├── TEMPLATE.md              # 템플릿
│   └── scripts/
│       ├── AnalyzeAllComponents.cs
│       ├── ExtractApiChanges.cs
│       ├── ApiGenerator.cs
│       ├── config/
│       │   └── component-priority.json
│       └── docs/
│           ├── phase1-setup.md
│           ├── phase2-collection.md
│           ├── phase3-analysis.md
│           ├── phase4-writing.md
│           └── phase5-validation.md
└── Src/
    ├── Functorium/
    └── Functorium.Testing/
```

---

## 추가 읽을거리

### 소프트웨어 엔지니어링

| 자료 | 저자 | 설명 |
|------|------|------|
| Clean Code | Robert C. Martin | 깨끗한 코드 작성법 |
| The Pragmatic Programmer | David Thomas, Andrew Hunt | 실용주의 프로그래머 |

### 함수형 프로그래밍

| 자료 | URL | 설명 |
|------|-----|------|
| LanguageExt | https://github.com/louthy/language-ext | C# 함수형 라이브러리 |

---

## 버전 정보

이 도서는 다음 버전을 기준으로 작성되었습니다:

| 소프트웨어 | 버전 |
|------------|------|
| .NET SDK | 10.0.100 |
| System.CommandLine | 2.0.1 |
| Spectre.Console | 0.54.0 |
| PublicApiGenerator | 11.1.0 |

---

## 피드백

이 도서에 대한 피드백이나 오류 신고는 다음을 통해 해주세요:

- GitHub Issues
- Pull Request

---

## 도서 완료

축하합니다! "릴리스 노트 자동화 시스템" 도서를 모두 읽으셨습니다.

### 학습 내용 요약

1. **1장**: 릴리스 노트 자동화의 필요성
2. **2장**: .NET 10, Claude Code, Git 환경 설정
3. **3장**: Claude 사용자 정의 Command 작성
4. **4장**: 5-Phase 워크플로우 이해
5. **5장**: C# file-based app 개발
6. **6장**: 템플릿 및 설정 파일 구성
7. **7장**: 실습을 통한 적용

### 다음 단계 제안

1. 자신의 프로젝트에 릴리스 노트 자동화 적용
2. 커스텀 분석 스크립트 작성
3. 팀에 시스템 도입 및 교육

---

**감사합니다!**
