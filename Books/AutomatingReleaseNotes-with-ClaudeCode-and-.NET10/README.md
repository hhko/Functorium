# Claude Code와 .NET 10으로 만드는 릴리스 노트 자동화

> AI 기반 CLI 도구와 C# 스크립트로 전문적인 릴리스 노트를 자동 생성하는 방법을 배웁니다.

---

## 이 책에 대하여

이 책은 **Claude Code의 사용자 정의 Command**와 **.NET 10 file-based app**을 활용하여 릴리스 노트 생성을 완전히 자동화하는 방법을 다룹니다. 실제 오픈소스 프로젝트인 **Functorium**의 릴리스 노트 자동화 시스템을 분석하며, 초급 C# 개발자도 쉽게 따라할 수 있도록 단계별로 설명합니다.

### 대상 독자

- C# 기초 문법을 알고 있는 **초급 개발자**
- CLI 도구 개발에 관심 있는 개발자
- 릴리스 노트 작성을 자동화하고 싶은 프로젝트 관리자
- Claude Code를 활용한 개발 워크플로우에 관심 있는 분

### 이 책에서 배우는 것

- Claude Code 사용자 정의 Command 작성법
- .NET 10 file-based app 개발 방법
- System.CommandLine을 활용한 CLI 인자 처리
- Spectre.Console을 활용한 풍부한 콘솔 UI
- Git 기반 변경사항 분석 자동화
- 5단계 워크플로우 기반 문서 자동화

---

## 목차

### [1장: 소개](01-introduction/)

릴리스 노트의 중요성과 자동화 시스템의 개요를 살펴봅니다.

- [1.1 릴리스 노트가 필요한 이유](01-introduction/01-why-release-notes.md)
- [1.2 자동화 시스템 개요](01-introduction/02-automation-overview.md)
- [1.3 프로젝트 구조 소개](01-introduction/03-project-structure.md)

### [2장: 사전 준비](02-prerequisites/)

개발 환경을 설정하고 필요한 도구를 설치합니다.

- [2.1 .NET 10 설치 및 환경 설정](02-prerequisites/01-dotnet10-setup.md)
- [2.2 Claude Code 소개](02-prerequisites/02-claude-code-intro.md)
- [2.3 Git 기초](02-prerequisites/03-git-basics.md)

### [3장: Claude 사용자 정의 Command](03-claude-commands/)

Claude Code에서 사용자 정의 Command를 만들고 활용하는 방법을 배웁니다.

- [3.1 사용자 정의 Command란?](03-claude-commands/01-what-is-command.md)
- [3.2 Command 문법 및 구조](03-claude-commands/02-command-syntax.md)
- [3.3 release-note.md 상세 분석](03-claude-commands/03-release-note-command.md)
- [3.4 commit.md 소개](03-claude-commands/04-commit-command.md)

### [4장: 5-Phase 워크플로우](04-five-phase-workflow/)

릴리스 노트 생성의 5단계 워크플로우를 상세히 분석합니다.

- [4.0 워크플로우 전체 개요](04-five-phase-workflow/00-overview.md)
- [4.1 Phase 1: 환경 검증](04-five-phase-workflow/01-phase1-setup.md)
- [4.2 Phase 2: 데이터 수집](04-five-phase-workflow/02-phase2-collection.md)
- [4.3 Phase 3: 커밋 분석](04-five-phase-workflow/03-phase3-analysis.md)
- [4.4 Phase 4: 문서 작성](04-five-phase-workflow/04-phase4-writing.md)
- [4.5 Phase 5: 검증](04-five-phase-workflow/05-phase5-validation.md)

### [5장: C# 스크립트](05-csharp-scripts/)

.NET 10 file-based app으로 작성된 C# 스크립트를 분석합니다.

- [5.1 .NET 10 file-based app 소개](05-csharp-scripts/01-file-based-apps.md)
- [5.2 System.CommandLine 패키지](05-csharp-scripts/02-system-commandline.md)
- [5.3 Spectre.Console 패키지](05-csharp-scripts/03-spectre-console.md)
- [5.4 AnalyzeAllComponents.cs 분석](05-csharp-scripts/04-analyze-all-components.md)
- [5.5 ExtractApiChanges.cs 분석](05-csharp-scripts/05-extract-api-changes.md)
- [5.6 ApiGenerator.cs 분석](05-csharp-scripts/06-api-generator.md)
- [5.7 SummarizeSlowestTests.cs 분석](05-csharp-scripts/07-summarize-tests.md)

### [6장: 템플릿 및 설정](06-templates-and-config/)

릴리스 노트 템플릿과 설정 파일을 분석합니다.

- [6.1 TEMPLATE.md 구조](06-templates-and-config/01-template-md.md)
- [6.2 component-priority.json 설정](06-templates-and-config/02-component-config.md)
- [6.3 출력 파일 형식](06-templates-and-config/03-output-formats.md)

### [7장: 실습 튜토리얼](07-hands-on-tutorial/)

실제로 릴리스 노트를 생성하고 나만의 스크립트를 작성합니다.

- [7.1 첫 번째 릴리스 노트 생성](07-hands-on-tutorial/01-first-release-note.md)
- [7.2 나만의 스크립트 작성](07-hands-on-tutorial/02-custom-script.md)
- [7.3 문제 해결 가이드](07-hands-on-tutorial/03-troubleshooting.md)

### [8장: Quick Reference](08-quick-reference/)

릴리스 노트 자동화 명령어와 워크플로우를 빠르게 참조합니다.

- [8.1 Quick Reference](08-quick-reference/01-quick-reference.md)

### [부록](appendix/)

- [A. 용어 사전](appendix/A-glossary.md)
- [B. API 레퍼런스](appendix/B-api-reference.md)
- [C. 참고 자료 및 링크](appendix/C-resources.md)

---

## 사용된 기술 스택

| 기술 | 버전 | 용도 |
|------|------|------|
| .NET | 10.0 | file-based app 실행 환경 |
| System.CommandLine | 2.0.1 | CLI 인자 처리 |
| Spectre.Console | 0.54.0 | 콘솔 UI (테이블, 패널, 스피너) |
| PublicApiGenerator | 11.5.4 | Public API 추출 |
| Claude Code | - | AI 기반 CLI 도구 |

---

## 예제 코드 저장소

이 책의 모든 예제 코드는 [Functorium](https://github.com/hhko/Functorium) 프로젝트에서 확인할 수 있습니다.

```bash
git clone https://github.com/hhko/Functorium.git
cd Functorium
```

주요 경로:
- `.claude/commands/` - Claude 사용자 정의 Command
- `.release-notes/scripts/` - C# 스크립트
- `.release-notes/scripts/docs/` - Phase별 상세 문서
