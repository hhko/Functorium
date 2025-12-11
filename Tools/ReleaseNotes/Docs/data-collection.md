# 릴리스 노트를 위한 데이터 수집

이 가이드는 릴리스 노트 생성 전 필요한 전체 데이터 수집 프로세스를 다룹니다. 모든 스크립트는 `tools/ReleaseNotes` 디렉터리에서 실행해야 합니다.

## 목표: 포괄적인 데이터 기반 구축

릴리스 버전 간의 컴포넌트 변경사항과 API 수정사항을 분석하여 정확하고 포괄적인 릴리스 노트 생성에 필요한 모든 데이터를 수집합니다.

## 데이터 수집 단계

### 1단계: 컴포넌트 변경사항 분석

```bash
./analyze-components.sh <base_branch> <target_branch>
```

**예시:**
```bash
./analyze-components.sh release/1.0 main
```

#### 생성되는 결과물:

**개별 컴포넌트 분석 파일** (`analysis-output/*.md`)
- 각 Functorium 컴포넌트별 파일 생성 (예: `Functorium.md`, `Functorium.Testing.md`)
- 각 파일 포함 내용:
  - **전체 변경 통계**: 추가/수정/삭제된 파일
  - **완전한 커밋 히스토리**: 해당 컴포넌트의 릴리스 간 모든 커밋
  - **주요 기여자**: 가장 많은 변경을 한 사람
  - **분류된 커밋**: 기능, 버그 수정, 브레이킹 체인지

**분석 요약** (`analysis-output/analysis-summary.md`)
- 모든 컴포넌트 변경사항의 고수준 개요
- 전체 컴포넌트의 총 커밋 수
- 주요 패턴 및 테마 요약

#### 예상 출력 구조:
```
analysis-output/
├── Functorium.md                    # 핵심 기능
├── Functorium.Testing.md            # 테스트 유틸리티
├── ... (컴포넌트별 파일)
└── analysis-summary.md              # 전체 요약
```

### 2단계: API 변경사항 추출

```bash
./extract-api-changes.sh
```

#### 생성되는 결과물:

**Uber API 파일** (`analysis-output/api-changes-build-current/all-api-changes.txt`)
- 모든 API 참조의 **단일 진실 소스**
- 현재 빌드의 완전한 API 정의
- 정확한 매개변수 이름과 타입을 포함한 메서드 시그니처
- **코드 샘플 검증에 중요** - 이 파일에 없으면 문서화하지 않습니다

**API 변경 요약** (`analysis-output/api-changes-build-current/api-changes-summary.md`)
- 모든 컴포넌트에서 추가된 새 API
- 브레이킹 체인지 및 폐기 예정
- 메서드 시그니처 변경
- 새 확장 메서드 및 빌더 패턴

**상세 API Diff** (`analysis-output/api-changes-build-current/api-changes-diff.txt`)
- 줄 단위 API 차이점
- 버전 간 정확한 변경 내용 표시
- 브레이킹 체인지 식별에 유용

#### 예상 출력 구조:
```
analysis-output/api-changes-build-current/
├── all-api-changes.txt              # UBER 파일 - 주요 API 소스
├── api-changes-summary.md           # 사람이 읽을 수 있는 API 요약
├── api-changes-diff.txt             # 원시 API 차이점
├── api-files/                       # 개별 어셈블리 API 파일
│   ├── Functorium.cs
│   └── Functorium.Testing.cs
└── projects.txt                     # 처리된 프로젝트 목록
```

### 3단계: 데이터 수집 결과 검증

두 스크립트 실행 후 다음을 확인합니다:

#### 컴포넌트 분석 검증:
```bash
# 컴포넌트 파일 수 확인
ls -1 analysis-output/*.md | wc -l

# 주요 컴포넌트 존재 확인
ls analysis-output/Functorium*.md

# 분석 요약 확인
head -20 analysis-output/analysis-summary.md
```

#### API 변경사항 검증:
```bash
# Uber 파일 존재 및 내용 확인
wc -l analysis-output/api-changes-build-current/all-api-changes.txt

# 주요 API 확인 (예시)
grep -c "ErrorCodeFactory" analysis-output/api-changes-build-current/all-api-changes.txt

# API 요약 검토
head -50 analysis-output/api-changes-build-current/api-changes-summary.md
```

## 출력 이해하기

### 컴포넌트 분석 파일 구조

각 컴포넌트 파일 (`*.md`)은 다음 구조를 따릅니다:

```markdown
# 컴포넌트 이름 분석

## 변경 요약
- X개 파일 변경
- 릴리스 간 Y개 커밋
- 주요 기여자: [목록]

## 모든 커밋 (시간순)
[SHA와 메시지가 포함된 전체 커밋 목록]

## 분류된 변경사항
### 기능
### 버그 수정
### 브레이킹 체인지
```

### 주요 커밋 패턴:

- **"Add"** 커밋 → 새 기능 또는 API
- **"Rename"** 커밋 → 브레이킹 체인지 또는 API 업데이트
- **"Improve/Enhance"** 커밋 → 기존 기능 개선
- **"Support for"** 커밋 → 새 플랫폼/기술 통합
- **GitHub 참조** (`#12345`) → 추가 컨텍스트 확인

## 다음 단계

데이터 수집 완료 후:

1. **[commit-analysis.md](commit-analysis.md) 검토** - 기능 분석 방법 학습
2. **[api-documentation.md](api-documentation.md) 검토** - API 검증 프로세스 이해
3. **기능 추출 시작** - 수집된 데이터 사용
4. **릴리스 노트 생성** - [writing-guidelines.md](writing-guidelines.md) 따르기

## 중요 사항

- **문서 작성 중 스크립트 실행 금지** - 데이터 수집은 사전에 한 번만 수행
- **Uber 파일이 단일 진실 소스** - API 검증용
- **모든 문서화된 API는 반드시 존재해야 함** - Uber 파일에서 확인
- **커밋 분석이 기능 기반 제공** - 릴리스 노트용
- **GitHub 이슈 조회로 커밋 이해 향상** - 추가 컨텍스트 제공

## 데이터 수집 체크리스트

- [ ] 컴포넌트 분석 완료 (`./analyze-components.sh`)
- [ ] API 변경사항 추출 완료 (`./extract-api-changes.sh`)
- [ ] 컴포넌트 파일 생성됨 (`analysis-output/`)
- [ ] Uber API 파일 생성됨 (`all-api-changes.txt`)
- [ ] API 요약 생성됨 (`api-changes-summary.md`)
- [ ] 주요 컴포넌트 파일 확인됨
- [ ] 기능 분석 및 문서화 진행 준비 완료
