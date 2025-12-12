# Functorium 릴리스 노트 생성 문서

이 디렉터리는 전문적인 Functorium 릴리스 노트를 생성하기 위한 모듈화된 문서를 포함합니다.

## 문서 구조

| 문서 | 설명 |
|------|------|
| [data-collection.md](data-collection.md) | 데이터 수집 단계 및 출력 검증 |
| [commit-analysis.md](commit-analysis.md) | 커밋 분석 및 기능 추출 가이드 |
| [api-documentation.md](api-documentation.md) | API 검증, 코드 샘플, 정확성 가이드라인 |
| [writing-guidelines.md](writing-guidelines.md) | 문서 스타일, 구조, 템플릿 요구사항 |
| [validation-checklist.md](validation-checklist.md) | 성공 기준 및 검증 프로세스 |

## 스크립트 구조

| 스크립트 | 설명 |
|----------|------|
| `analyze-all-components.ps1` / `.sh` | 컴포넌트 변경사항 분석 |
| `analyze-folder.ps1` / `.sh` | 개별 폴더 상세 분석 |
| `extract-api-changes.ps1` / `.sh` | API 변경사항 추출 |

## 빠른 시작

### 1. 데이터 수집

```powershell
# PowerShell - 첫 배포 시
$FIRST_COMMIT = git rev-list --max-parents=0 HEAD
.\analyze-all-components.ps1 -BaseBranch $FIRST_COMMIT -TargetBranch origin/main
.\extract-api-changes.ps1

# PowerShell - 릴리스 간 비교
.\analyze-all-components.ps1 -BaseBranch origin/release/1.0 -TargetBranch origin/main
.\extract-api-changes.ps1
```

### 2. 출력 확인

```
analysis-output/
├── analysis-summary.md              # 컴포넌트 분석 요약
├── Functorium.md                    # Src/Functorium 분석
├── Functorium.Testing.md            # Src/Functorium.Testing 분석
├── Docs.md                          # Docs 분석
└── api-changes-build-current/
    ├── all-api-changes.txt          # Uber API 파일 (단일 진실 소스)
    ├── api-changes-summary.md       # API 요약
    ├── projects.txt                 # 처리된 프로젝트 목록
    └── api-files/                   # 개별 API 파일
        ├── Functorium.cs
        └── Functorium.Testing.cs
```

### 3. 릴리스 노트 작성

1. [data-collection.md](data-collection.md)에서 데이터 수집 프로세스 확인
2. [commit-analysis.md](commit-analysis.md)에서 변경사항 분석 방법 확인
3. [api-documentation.md](api-documentation.md)에서 정확한 코드 샘플 작성법 확인
4. [writing-guidelines.md](writing-guidelines.md)에서 적절한 포맷팅 적용
5. [validation-checklist.md](validation-checklist.md)로 검증

## 핵심 원칙

> **정확성 우선**: 모든 문서화된 API는 Uber 파일에 존재해야 합니다.
> - API를 임의로 만들어내지 않습니다
> - 모든 기능은 커밋/PR로 추적 가능해야 합니다
> - 코드 샘플은 `all-api-changes.txt`에서 검증합니다

## 설정

컴포넌트 분석 대상은 `config/component-priority.json`에서 설정합니다:

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs"
  ]
}
```
