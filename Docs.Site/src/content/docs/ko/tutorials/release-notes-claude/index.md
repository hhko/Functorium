---
title: "릴리스 노트 자동화"
---

**릴리스마다 2-3시간씩 Git 로그를 뒤지며 수동으로 릴리스 노트를 작성하고 계신가요?** 빠뜨린 Breaking Change 때문에 사용자 이슈가 올라오고, 작성자마다 형식이 달라 일관성 없는 문서가 쌓이는 경험, 누구나 한 번쯤 해보셨을 것입니다.

이 튜토리얼은 그 문제를 해결합니다. **Claude Code의 사용자 정의 Command와** **.NET 10 file-based app을** 조합하여, `/release-note v1.2.0` 한 줄이면 전문적인 릴리스 노트가 자동으로 생성되는 시스템을 만들어 봅니다. 실제 오픈소스 프로젝트인 Functorium에서 운용 중인 자동화 시스템을 분석하며, 5단계 워크플로우를 통해 체계적으로 학습할 수 있습니다.

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| **초급** | C# 기초 문법을 알고 CLI 도구 개발에 입문하려는 개발자 | Part 0~1 |
| **중급** | 워크플로우 자동화와 스크립트 개발에 관심 있는 개발자 | Part 2~3 전체 |
| **고급** | Claude Code 커스터마이징과 고급 자동화에 관심 있는 개발자 | Part 4~5 + 부록 |

### 학습 목표

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

1. **Claude Code 사용자 정의 Command** 작성 및 활용
2. **.NET 10 file-based app으로** CLI 스크립트 개발
3. **System.CommandLine을** 활용한 전문적인 CLI 인자 처리
4. **Spectre.Console을** 활용한 풍부한 콘솔 UI 구현

---

### Part 0: 서론

릴리스 노트가 왜 필요한지, 자동화 시스템이 어떤 구조로 동작하는지 큰 그림을 먼저 그려봅니다.

- [0.1 릴리스 노트가 필요한 이유](Part0-Introduction/01-why-release-notes.md)
- [0.2 자동화 시스템 개요](Part0-Introduction/02-automation-overview.md)
- [0.3 프로젝트 구조 소개](Part0-Introduction/03-project-structure.md)

### Part 1: 사전 준비

실습 환경을 갖추기 위해 .NET 10 SDK, Claude Code, Git을 설치하고 설정합니다.

- [1.1 .NET 10 설치](Part1-Setup/01-dotnet10-setup.md)
- [1.2 Claude Code 소개](Part1-Setup/02-claude-code-intro.md)
- [1.3 Git 기초](Part1-Setup/03-git-basics.md)

### Part 2: Claude Commands

Claude Code에서 사용자 정의 Command를 만드는 방법을 배우고, 릴리스 노트 생성 Command의 내부 구조를 분석합니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [사용자 정의 Command란?](Part2-Claude-Commands/01-what-is-command.md) | Command 개념 이해 |
| 2 | [Command 문법 및 구조](Part2-Claude-Commands/02-command-syntax.md) | 문법 및 작성법 |
| 3 | [release-note.md 상세 분석](Part2-Claude-Commands/03-release-note-command.md) | 릴리스 노트 Command |
| 4 | [commit.md 소개](Part2-Claude-Commands/04-commit-command.md) | 커밋 Command |

### Part 3: 워크플로우

릴리스 노트 생성의 5단계 워크플로우를 상세히 분석합니다. 환경 검증부터 최종 검증까지, 각 단계가 어떤 입력을 받아 어떤 출력을 만들어내는지 살펴봅니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 0 | [워크플로우 개요](Part3-Workflow/00-overview.md) | 5-Phase 전체 개요 |
| 1 | [Phase 1: 환경 검증](Part3-Workflow/01-phase1-setup.md) | 디렉토리, 파일 확인 |
| 2 | [Phase 2: 데이터 수집](Part3-Workflow/02-phase2-collection.md) | Git 로그, 변경 내역 |
| 3 | [Phase 3: 커밋 분석](Part3-Workflow/03-phase3-analysis.md) | 커밋 분류, 그룹화 |
| 4 | [Phase 4: 문서 작성](Part3-Workflow/04-phase4-writing.md) | 템플릿 기반 생성 |
| 5 | [Phase 5: 검증](Part3-Workflow/05-phase5-validation.md) | 출력 파일 검증 |

### Part 4: 구현

.NET 10 file-based app으로 작성된 C# 스크립트와 템플릿의 내부를 들여다봅니다. System.CommandLine으로 CLI 인자를 처리하고, Spectre.Console로 풍부한 콘솔 UI를 만드는 방법까지 다룹니다.

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

지금까지 배운 내용을 토대로 직접 릴리스 노트를 생성하고, 나만의 자동화 스크립트를 작성해봅니다.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [첫 번째 릴리스 노트 생성](Part5-Hands-On/01-first-release-note.md) | 첫 실습 |
| 2 | [나만의 스크립트 작성](Part5-Hands-On/02-custom-script.md) | 커스텀 스크립트 |
| 3 | [문제 해결 가이드](Part5-Hands-On/03-troubleshooting.md) | 트러블슈팅 |
| 4 | [Quick Reference](Part5-Hands-On/04-quick-reference.md) | 빠른 참조 |

### [부록](Appendix/)

- [A. 용어 사전](Appendix/A-glossary.md)
- [B. API 레퍼런스](Appendix/B-api-reference.md)
- [C. 참고 자료 및 링크](Appendix/C-resources.md)

---

## 5-Phase 워크플로우

자동화 시스템의 핵심은 5단계 파이프라인입니다. 사용자가 `/release-note v1.2.0` 명령을 실행하면, 환경 검증에서 시작해 데이터 수집, 커밋 분석, 문서 작성, 최종 검증까지 순차적으로 진행됩니다. 각 단계는 이전 단계의 출력을 입력으로 받으며, 명확한 성공 기준이 정의되어 있어 어디서 문제가 생겼는지 바로 파악할 수 있습니다.

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

## 소스 코드

이 튜토리얼의 모든 예제 코드는 Functorium 프로젝트에서 확인할 수 있습니다:

```bash
git clone https://github.com/hhko/Functorium.git
cd Functorium
```

Claude 사용자 정의 Command는 `.claude/commands/`에, C# 스크립트는 `.release-notes/scripts/`에, Phase별 상세 문서는 `.release-notes/scripts/docs/`에 위치합니다. 프로젝트 전체 폴더 구조는 [0.3 프로젝트 구조 소개](Part0-Introduction/03-project-structure.md)에서 자세히 다룹니다.

---

이 튜토리얼은 Functorium 프로젝트의 실제 릴리스 노트 자동화 시스템 개발 경험을 바탕으로 작성되었습니다.
