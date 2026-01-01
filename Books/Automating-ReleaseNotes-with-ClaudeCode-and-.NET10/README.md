# Claude Code와 .NET 10으로 만드는 릴리스 노트 자동화

**AI 기반 CLI 도구와 C# 스크립트로 전문적인 릴리스 노트를 자동 생성하는 방법을 배웁니다**

---

## 이 책에 대하여

이 책은 **Claude Code의 사용자 정의 Command**와 **.NET 10 file-based app**을 활용하여 릴리스 노트 생성을 완전히 자동화하는 방법을 다룹니다. 실제 오픈소스 프로젝트인 **Functorium**의 릴리스 노트 자동화 시스템을 분석하며, **5단계 워크플로우**를 통해 체계적으로 학습할 수 있습니다.

> **수동 릴리스 노트 작성에서 완전 자동화 시스템까지, AI와 C# 스크립트의 조합으로 구현합니다.**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| 🟢 **초급** | C# 기초 문법을 알고 CLI 도구 개발에 입문하려는 개발자 | Part 0~1 |
| 🟡 **중급** | 워크플로우 자동화와 스크립트 개발에 관심 있는 개발자 | Part 2~3 전체 |
| 🔴 **고급** | Claude Code 커스터마이징과 고급 자동화에 관심 있는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 책을 완료하면 다음을 할 수 있습니다:

1. **Claude Code 사용자 정의 Command** 작성 및 활용
2. **.NET 10 file-based app**으로 CLI 스크립트 개발
3. **System.CommandLine**을 활용한 전문적인 CLI 인자 처리
4. **Spectre.Console**을 활용한 풍부한 콘솔 UI 구현

---

## 목차

### Part 0: 서론

릴리스 노트의 중요성과 자동화 시스템의 개요를 살펴봅니다.

- [0.1 릴리스 노트가 필요한 이유](Part0-Introduction/01-why-release-notes.md)
- [0.2 자동화 시스템 개요](Part0-Introduction/02-automation-overview.md)
- [0.3 프로젝트 구조 소개](Part0-Introduction/03-project-structure.md)

### Part 1: 사전 준비

개발 환경을 설정하고 필요한 도구를 설치합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [.NET 10 설치](Part1-Setup/01-dotnet10-setup.md) | .NET 10 설치 및 환경 설정 |
| 2 | [Claude Code 소개](Part1-Setup/02-claude-code-intro.md) | Claude Code CLI 도구 이해 |
| 3 | [Git 기초](Part1-Setup/03-git-basics.md) | Git 기본 명령어 |

### Part 2: Claude Commands

Claude Code에서 사용자 정의 Command를 만들고 활용하는 방법을 배웁니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [사용자 정의 Command란?](Part2-Claude-Commands/01-what-is-command.md) | Command 개념 이해 |
| 2 | [Command 문법 및 구조](Part2-Claude-Commands/02-command-syntax.md) | 문법 및 작성법 |
| 3 | [release-note.md 상세 분석](Part2-Claude-Commands/03-release-note-command.md) | 릴리스 노트 Command |
| 4 | [commit.md 소개](Part2-Claude-Commands/04-commit-command.md) | 커밋 Command |

### Part 3: 워크플로우

릴리스 노트 생성의 5단계 워크플로우를 상세히 분석합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 0 | [워크플로우 개요](Part3-Workflow/00-overview.md) | 5-Phase 전체 개요 |
| 1 | [Phase 1: 환경 검증](Part3-Workflow/01-phase1-setup.md) | 디렉토리, 파일 확인 |
| 2 | [Phase 2: 데이터 수집](Part3-Workflow/02-phase2-collection.md) | Git 로그, 변경 내역 |
| 3 | [Phase 3: 커밋 분석](Part3-Workflow/03-phase3-analysis.md) | 커밋 분류, 그룹화 |
| 4 | [Phase 4: 문서 작성](Part3-Workflow/04-phase4-writing.md) | 템플릿 기반 생성 |
| 5 | [Phase 5: 검증](Part3-Workflow/05-phase5-validation.md) | 출력 파일 검증 |

### Part 4: 구현

.NET 10 file-based app으로 작성된 C# 스크립트와 템플릿을 분석합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [.NET 10 file-based app](Part4-Implementation/01-file-based-apps.md) | file-based app 소개 |
| 2 | [System.CommandLine](Part4-Implementation/02-system-commandline.md) | CLI 인자 처리 |
| 3 | [Spectre.Console](Part4-Implementation/03-spectre-console.md) | 콘솔 UI 구현 |
| 4 | [AnalyzeAllComponents.cs](Part4-Implementation/04-analyzeallcomponents.md) | 컴포넌트 분석 스크립트 |
| 5 | [ExtractApiChanges.cs](Part4-Implementation/05-extractapichanges.md) | API 변경 추출 |
| 6 | [ApiGenerator.cs](Part4-Implementation/06-apigenerator.md) | API 생성기 |
| 7 | [TEMPLATE.md 구조](Part4-Implementation/07-template-structure.md) | 템플릿 구조 |
| 8 | [component-priority.json](Part4-Implementation/08-component-config.md) | 컴포넌트 설정 |
| 9 | [출력 파일 형식](Part4-Implementation/09-output-formats.md) | 출력 형식 |

### Part 5: 실습

실제로 릴리스 노트를 생성하고 나만의 스크립트를 작성합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [첫 번째 릴리스 노트 생성](Part5-Hands-On/01-first-release-note.md) | 첫 실습 |
| 2 | [나만의 스크립트 작성](Part5-Hands-On/02-custom-script.md) | 커스텀 스크립트 |
| 3 | [문제 해결 가이드](Part5-Hands-On/03-troubleshooting.md) | 트러블슈팅 |
| 4 | [Quick Reference](Part5-Hands-On/04-quick-reference.md) | 빠른 참조 |

### [부록](appendix/)

- [A. 용어 사전](appendix/A-glossary.md)
- [B. API 레퍼런스](appendix/B-api-reference.md)
- [C. 참고 자료 및 링크](appendix/C-resources.md)

---

## 5-Phase 워크플로우

```
Phase 1: 환경 검증    →  Phase 2: 데이터 수집  →  Phase 3: 커밋 분석
     ↓
Phase 4: 문서 작성    →  Phase 5: 검증
```

| Phase | 단계 | 주요 작업 |
|:-----:|------|----------|
| 1 | 환경 검증 | 디렉토리 구조, 필수 파일 확인 |
| 2 | 데이터 수집 | Git 커밋 로그, 파일 변경 내역 수집 |
| 3 | 커밋 분석 | 커밋 분류, 컴포넌트별 그룹화 |
| 4 | 문서 작성 | 템플릿 기반 릴리스 노트 생성 |
| 5 | 검증 | 출력 파일 검증, 형식 확인 |

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

## 필수 준비물

- .NET 10.0 SDK (Preview 또는 정식 버전)
- Claude Code CLI
- Git
- Visual Studio 2022 또는 VS Code + C# 확장

---

## 프로젝트 구조

```
Automating-ReleaseNotes-with-ClaudeCode-and-.NET10/
├── Part0-Introduction/         # Part 0: 서론
│   ├── 01-why-release-notes.md
│   ├── 02-automation-overview.md
│   └── 03-project-structure.md
├── Part1-Setup/                # Part 1: 사전 준비
│   ├── 01-dotnet10-setup.md
│   ├── 02-claude-code-intro.md
│   └── 03-git-basics.md
├── Part2-Claude-Commands/      # Part 2: Claude Commands
│   ├── 01-what-is-command.md
│   ├── 02-command-syntax.md
│   ├── 03-release-note-command.md
│   └── 04-commit-command.md
├── Part3-Workflow/             # Part 3: 5-Phase 워크플로우
│   ├── 00-overview.md
│   ├── 01-phase1-setup.md
│   ├── 02-phase2-collection.md
│   ├── 03-phase3-analysis.md
│   ├── 04-phase4-writing.md
│   └── 05-phase5-validation.md
├── Part4-Implementation/       # Part 4: 구현
│   ├── 01-file-based-apps.md
│   ├── 02-system-commandline.md
│   ├── 03-spectre-console.md
│   ├── 04-analyzeallcomponents.md
│   ├── 05-extractapichanges.md
│   ├── 06-apigenerator.md
│   ├── 07-template-structure.md
│   ├── 08-component-config.md
│   └── 09-output-formats.md
├── Part5-Hands-On/             # Part 5: 실습
│   ├── 01-first-release-note.md
│   ├── 02-custom-script.md
│   ├── 03-troubleshooting.md
│   └── 04-quick-reference.md
├── appendix/                   # 부록
│   ├── A-glossary.md
│   ├── B-api-reference.md
│   └── C-resources.md
└── README.md                   # 이 문서
```

---

## 소스 코드

이 책의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

```bash
git clone https://github.com/hhko/Functorium.git
cd Functorium
```

- Claude 사용자 정의 Command: `.claude/commands/`
- C# 스크립트: `.release-notes/scripts/`
- Phase별 상세 문서: `.release-notes/scripts/docs/`

---

이 책은 Functorium 프로젝트의 실제 릴리스 노트 자동화 시스템 개발 경험을 바탕으로 작성되었습니다.
