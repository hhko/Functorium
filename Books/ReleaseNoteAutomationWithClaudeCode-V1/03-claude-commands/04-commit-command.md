# 3.4 commit.md 소개

> 이 절에서는 일관된 커밋 메시지 작성을 위한 `commit.md` Command를 소개합니다. 이 Command는 릴리스 노트 자동화의 기반이 되는 커밋 히스토리 품질을 유지합니다.

---

## 파일 위치

```txt
.claude/commands/commit.md
```

---

## 프론트매터

```yaml
---
title: COMMIT
description: Conventional Commits 규격에 따라 변경사항을 커밋합니다.
argument-hint: "[topic]을 전달하면 해당 topic 관련 파일만 선별하여 커밋합니다"
---
```

| 필드 | 값 | 설명 |
|------|-----|------|
| title | COMMIT | 명령어 이름 |
| description | Conventional Commits... | 커밋 규칙 적용 |
| argument-hint | `[topic]` | 선택적 토픽 필터 |

---

## 왜 commit Command가 필요한가?

### 릴리스 노트와의 연관성

릴리스 노트 자동화 시스템은 커밋 메시지를 파싱하여 기능을 분류합니다:

```txt
커밋 메시지 → 자동 분류 → 릴리스 노트

feat(api): 새로운 엔드포인트 추가
  ↓
"새로운 기능" 섹션에 자동 포함

fix(auth): 토큰 만료 오류 수정
  ↓
"버그 수정" 섹션에 자동 포함

feat!: API 응답 형식 변경
  ↓
"Breaking Changes" 섹션에 자동 포함
```

일관된 커밋 메시지 형식이 없으면 자동 분류가 불가능합니다.

---

## Conventional Commits 형식

commit.md는 Conventional Commits 규격을 따릅니다:

```txt
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### 예시

```txt
feat(calculator): 나눗셈 기능 구현

- Divide 메서드 추가
- 0으로 나누기 예외 처리 포함

Closes #42
```

---

## 커밋 타입

| 타입 | 설명 | 릴리스 노트 분류 |
|------|------|-----------------|
| `feat` | 새로운 기능 추가 | 새로운 기능 |
| `fix` | 버그 수정 | 버그 수정 |
| `docs` | 문서 변경 | (보통 생략) |
| `style` | 코드 포맷팅 | (생략) |
| `refactor` | 리팩터링 | (보통 생략) |
| `perf` | 성능 개선 | 개선사항 |
| `test` | 테스트 추가/수정 | (생략) |
| `build` | 빌드 시스템 변경 | (보통 생략) |
| `ci` | CI 설정 변경 | (생략) |
| `chore` | 기타 변경 | (생략) |

---

## Topic 파라미터

commit Command의 핵심 기능 중 하나는 **Topic 필터링**입니다.

### Topic 미지정

```bash
/commit
```

모든 변경사항을 논리적 단위로 분리하여 여러 커밋 생성:

```txt
변경 파일:
├── UserService.cs (기능)
├── UserServiceTests.cs (테스트)
├── README.md (문서)
└── .gitignore (설정)

결과:
├── 커밋 1: feat(user): 사용자 서비스 추가
├── 커밋 2: test(user): 사용자 서비스 테스트 추가
├── 커밋 3: docs: README 업데이트
└── 커밋 4: chore: .gitignore 업데이트
```

### Topic 지정

```bash
/commit 빌드
```

지정된 Topic과 관련된 파일만 선별하여 단일 커밋 생성:

```txt
변경 파일:
├── Build-Local.ps1 (빌드 관련 ✓)
├── Directory.Build.props (빌드 관련 ✓)
├── README.md (빌드 무관 ✗)
└── UserService.cs (빌드 무관 ✗)

결과:
└── 커밋: feat(build): 빌드 설정 개선
    (Build-Local.ps1, Directory.Build.props만 포함)
```

### Topic 선별 기준

```txt
파일 선별 기준:
├── 파일명에 topic 키워드 포함
├── 파일 내용이 topic과 직접 관련
├── 디렉토리 경로에 topic 키워드 포함
└── 변경 내용이 topic 주제와 연관
```

---

## 커밋 메시지 작성 규칙

### 제목 (첫 줄)

```txt
규칙:
├── 72자 이내
├── 명령형으로 작성 ("추가한다" X, "추가" O)
├── 마침표 사용 금지
└── 한글로 작성
```

### 본문 (선택)

```txt
규칙:
├── 제목과 빈 줄로 구분
├── "무엇을", "왜" 변경했는지 설명
└── 72자마다 줄바꿈 권장
```

### 푸터 (선택)

```txt
용도:
├── Breaking Change 정보
└── 관련 이슈 참조 (Closes #123)
```

---

## 커밋 메시지 예시

### 새 기능 추가

```txt
feat(calculator): 나눗셈 기능 구현

- Divide 메서드 추가
- 0으로 나누기 예외 처리 포함
```

### 버그 수정

```txt
fix(auth): 토큰 만료 시 자동 갱신 실패 수정

만료된 리프레시 토큰으로 갱신 시도 시
무한 루프에 빠지는 문제 해결

Closes #42
```

### Breaking Change

```txt
feat!: API 응답 형식 변경

BREAKING CHANGE: 응답 JSON 구조가 변경되었습니다.
```

### 리팩터링

```txt
refactor: 테스트 픽스처에 공통 설정 추출

각 테스트 클래스에서 중복된 초기화 코드를
BaseTestFixture로 이동
```

---

## 커밋 조건 및 금지사항

### 커밋 전 필수 조건

```txt
코드 변경 시:
├── 모든 테스트 통과
├── 모든 컴파일러 경고 해결
└── 하나의 논리적 작업 단위
```

### 금지사항

```txt
절대 포함하지 말 것:
├── Claude/AI 생성 관련 메시지
├── Co-Authored-By: Claude
├── 이모지
└── 테스트 실패/경고 있는 상태
```

---

## 커밋 절차

### 기본 절차 (Topic 미지정)

```bash
# 1. 변경사항 확인
git status

# 2. 변경 내용 검토
git diff

# 3. 최근 커밋 스타일 확인
git log --oneline -5

# 4. 논리적 단위로 분리하여 스테이징 및 커밋
git add <files>
git commit -m "type(scope): description"
```

### Topic 지정 시 절차

```bash
# 1. 변경사항 확인
git status
git diff

# 2. Topic 관련 파일 선별
# 예: /commit 빌드

# 3. 선별된 파일만 스테이징
git add Build-Local.ps1 Directory.Build.props

# 4. 단일 커밋 생성
git commit -m "feat(build): 빌드 설정 개선"

# 5. 검증 (topic 무관 파일은 unstaged 상태)
git status
```

---

## 완료 메시지 형식

```txt
커밋 완료

커밋 정보:
  - 타입: feat
  - 메시지: 나눗셈 기능 구현
  - 변경 파일: 3개
```

---

## release-note.md와의 관계

```txt
commit.md                          release-note.md
    │                                    │
    │  Conventional Commits 형식         │
    │  ─────────────────────────▶        │
    │                                    │
    │  커밋 히스토리                      │
    │  ─────────────────────────▶        │
    │                                    │
    ▼                                    ▼
일관된 커밋 메시지        ────▶    자동 분류 및 릴리스 노트 생성
```

commit.md로 생성된 일관된 커밋 히스토리가 release-note.md의 자동 분류 기반이 됩니다.

---

## 정리

| 항목 | 설명 |
|------|------|
| 목적 | Conventional Commits 규격 적용 |
| Topic 미지정 | 모든 변경사항을 논리적 단위로 분리 커밋 |
| Topic 지정 | 관련 파일만 선별하여 단일 커밋 |
| 릴리스 노트 연계 | 일관된 커밋 → 자동 기능 분류 |

---

## 다음 단계

- [4.0 워크플로우 전체 개요](../04-five-phase-workflow/00-overview.md)
