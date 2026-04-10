---
title: "release-note"
description: "릴리스 노트 자동 생성 플러그인"
---

release-note는 Functorium 프로젝트의 릴리스 노트 생성을 자동화하는 Claude Code 플러그인입니다. C# 스크립트 기반 데이터 수집, Conventional Commits 분석, Breaking Changes 감지, 릴리스 노트 작성, 검증까지 5단계 워크플로를 실행합니다.

## 설치

```bash
# 단독 로드
claude --plugin-dir ./.claude/plugins/release-note

# functorium-develop 플러그인과 동시 로드
claude --plugin-dir ./.claude/plugins/release-note --plugin-dir ./.claude/plugins/functorium-develop
```

> `--plugin-dir`는 세션 단위로 플러그인을 로드합니다. `/skills`에서 `release-note:generate` 형식으로 표시됩니다.

## 5-Phase 워크플로

```
환경 검증 → 데이터 수집 → 커밋 분석 → 릴리스 노트 작성 → 검증
```

| Phase | 이름 | 목표 | 주요 산출물 |
|:-----:|------|------|------------|
| 1 | 환경 검증 | 전제조건 확인, Base Branch 결정 | Git/SDK 상태, Base/Target 결정 |
| 2 | 데이터 수집 | C# 스크립트로 컴포넌트/API 변경 분석 | `*.md` 분석 파일, `all-api-changes.txt`, `api-changes-diff.txt` |
| 3 | 커밋 분석 | 수집된 데이터 분석, 기능 추출 | `phase3-commit-analysis.md`, `phase3-feature-groups.md` |
| 4 | 릴리스 노트 작성 | TEMPLATE.md 기반 릴리스 노트 생성 | `RELEASE-{VERSION}.md` |
| 5 | 검증 | 품질 및 정확성 검증 | `phase5-validation-report.md`, `phase5-api-validation.md` |

### 핵심 원칙

| 원칙 | 설명 |
|------|------|
| 정확성 우선 | Uber 파일(`all-api-changes.txt`)에 없는 API는 절대 문서화하지 않음 |
| 가치 전달 필수 | 모든 주요 기능에 "Why this matters (왜 중요한가):" 섹션 포함 |
| Breaking Changes 자동 감지 | Git Diff 분석이 커밋 메시지 패턴보다 우선 |
| 추적성 | 모든 기능을 커밋 SHA로 추적 |

## generate 스킬

릴리스 노트를 자동으로 생성하는 유일한 스킬입니다. 버전을 파라미터로 받아 5-Phase 워크플로 전체를 실행합니다.

**트리거 예시:**

```text
/generate v1.2.0
릴리스 노트 생성해줘
release note 만들어줘
릴리스 노트 작성
새 버전 릴리스
```

**버전 형식:**

| 형식 | 예시 | 설명 |
|------|------|------|
| 정규 릴리스 | `v1.2.0` | 일반 배포 |
| 첫 배포 | `v1.0.0` | 초기 커밋부터 분석 |
| 프리릴리스 | `v1.0.0-beta.1` | 사전 배포 |

## 에이전트

release-note 플러그인은 **release-engineer** 에이전트 1개를 제공합니다. C# 스크립트 실행, 커밋 분석, Breaking Changes 감지, 릴리스 노트 작성 및 검증을 전담합니다.

상세 내용은 [전문 에이전트](./agents/) 페이지를 참고하십시오.

## 플러그인 구조

```
.claude/plugins/release-note/
├── .claude-plugin/
│   └── plugin.json              # 플러그인 메타데이터 (v1.0.0)
├── skills/
│   └── generate/
│       └── SKILL.md             # generate 스킬 정의 (5-Phase 워크플로)
└── agents/
    └── release-engineer.md      # release-engineer 에이전트 정의
```

## .release-notes/ 디렉터리 구조

릴리스 노트 생성에 필요한 스크립트, 템플릿, 검증 도구가 프로젝트 루트의 `.release-notes/` 디렉터리에 위치합니다.

```
.release-notes/
├── TEMPLATE.md                  # 릴리스 노트 복사용 템플릿
├── RELEASE-{VERSION}.md         # 생성된 릴리스 노트
├── validate-release-notes.ps1   # GitHub Release 크기 제한(125,000자) 검증
├── README.md                    # 프로세스 개요
└── scripts/
    ├── AnalyzeAllComponents.cs  # 컴포넌트 분석 C# 스크립트
    ├── AnalyzeFolder.cs         # 폴더 분석 C# 스크립트
    ├── ApiGenerator.cs          # API 변경 생성 C# 스크립트
    ├── ExtractApiChanges.cs     # API 변경 추출 C# 스크립트
    ├── Directory.Build.props    # 빌드 설정
    ├── Directory.Packages.props # 패키지 설정
    ├── config/
    │   └── component-priority.json  # 컴포넌트 우선순위 설정
    └── docs/
        ├── README.md            # 5-Phase 워크플로 전체 개요
        ├── phase1-setup.md      # Phase 1 상세
        ├── phase2-collection.md # Phase 2 상세
        ├── phase3-analysis.md   # Phase 3 상세
        ├── phase4-writing.md    # Phase 4 상세
        └── phase5-validation.md # Phase 5 상세
```

## 트러블슈팅

| 문제 | 해결 방법 |
|------|----------|
| Base Branch 없음 | 첫 배포로 자동 감지, 초기 커밋부터 분석 |
| .NET SDK 버전 오류 | .NET 10.x 설치 필요 |
| 파일 잠금 문제 | `taskkill /F /IM dotnet.exe` (Windows) |
| API 검증 실패 | Uber 파일에서 올바른 API 이름 확인 |
| runfile 캐시 오류 | `./Build-CleanRunFileCache.ps1` 실행 |

### 전체 초기화 (Windows)

```powershell
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .release-notes\scripts\.analysis-output -ErrorAction SilentlyContinue
dotnet nuget locals all --clear
```
