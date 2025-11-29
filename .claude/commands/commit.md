---
title: COMMIT
description: Git 커밋 규칙에 따라 변경사항을 커밋합니다.
---

# Git 커밋 규칙

변경사항을 명확하고 일관된 규칙에 따라 커밋하여 프로젝트의 히스토리를 체계적으로 관리합니다.

## 적용 범위

이 커밋 규칙은 **코드 변경**에만 적용됩니다.

**CHANGE_TYPE을 사용하는 경우 (코드 변경):**
- 프로덕션 코드 추가/수정
- 테스트 코드 추가/수정
- 리팩터링
- 빌드 스크립트, 패키지 의존성 등 코드 동작에 영향을 주는 변경

**CHANGE_TYPE을 사용하지 않는 경우 (코드 외 변경):**
- 문서 수정 (README, .md 파일 등)
- 코드 포맷팅만 변경 (로직 변경 없음)
- .gitignore, .editorconfig 등 설정 파일만 변경

## 형상관리 대상 파일

다음 파일들은 형상관리 대상이며, 커밋 시 적절한 분류를 적용합니다:

| 경로 | 설명 | 커밋 분류 |
|------|------|-----------|
| `.claude/` | Claude Code 명령어, 가이드, 설정 파일 | `chore:` (CHANGE_TYPE 없음) |
| `.scripts/` | 빌드 스크립트 (Build.ps1 등) | `chore:` (CHANGE_TYPE 없음) |
| `.plans/` | 계획 문서 디렉토리 | `docs:` (CHANGE_TYPE 없음) |
| `PLAN.md` | 현재 작업 계획 문서 | `docs:` (CHANGE_TYPE 없음) |

**참고:** 위 파일들은 코드 동작에 직접적인 영향을 주지 않으므로 CHANGE_TYPE을 사용하지 않습니다.

## 커밋 조건

다음 조건을 **모두** 충족할 때만 커밋하십시오:
- 모든 테스트가 통과했을 때 (코드 변경 시)
- 모든 컴파일러/린터 경고가 해결되었을 때 (코드 변경 시)
- 변경이 하나의 논리적 작업 단위를 나타낼 때

## 커밋 메시지 형식

**코드 변경 (CHANGE_TYPE 포함):**
```
[CHANGE_TYPE] prefix: 한글 요약

- 한글 상세 설명
- 변경 내용
- 관련 테스트
```

**코드 외 변경 (CHANGE_TYPE 없음):**
```
prefix: 한글 요약

- 한글 상세 설명
- 변경 내용
```

### 변경 유형 (CHANGE_TYPE)
- `[STRUCTURAL]` - 구조적(structural) 변경 (코드 동작 변경 없음)
- `[BEHAVIORAL]` - 기능적(behavioral) 변경 (코드 동작 변경 있음)

**중요: 오직 위 2가지 타입만 사용. 코드 외 변경은 CHANGE_TYPE을 사용하지 않음**

## Tidy First 원칙 (Kent Beck)

구조적 변경과 기능적 변경을 명확히 분리하는 것은 Kent Beck의 "Tidy First" 원칙에 기반합니다.

### 핵심 정의

| 변경 유형 | 정의 | 테스트 결과 |
|-----------|------|-------------|
| **구조적 변경 (STRUCTURAL)** | 동작을 바꾸지 않으면서 코드를 재배치하는 변경 | 변경 전후 동일 |
| **기능적 변경 (BEHAVIORAL)** | 실제 기능을 추가하거나 수정하는 변경 | 변경 후 달라짐 |

### 구조적 변경의 특징
- 코드의 **형태(shape)**만 변경
- 외부에서 관찰 가능한 동작은 **동일**
- 리팩터링 패턴 적용: 이름 변경, 메서드 추출, 코드 이동, 중복 제거 등
- 변경 전후로 **모든 테스트가 동일하게 통과**해야 함

### 기능적 변경의 특징
- 코드의 **동작(behavior)**을 변경
- 새로운 기능 추가, 버그 수정, 예외 처리 변경 등
- 테스트 추가/수정이 동반됨
- 변경 후 **테스트 결과가 달라짐** (새 테스트 통과, 기존 테스트 결과 변경 등)

### Tidy First 규칙
1. **절대 섞지 마십시오**: 구조적 변경과 기능적 변경을 같은 커밋에 포함하지 마십시오
2. **구조적 변경 먼저**: 두 가지가 모두 필요할 때는 항상 구조적 변경을 먼저 커밋하십시오
3. **테스트로 검증**: 구조적 변경이 동작을 변경하지 않았음을 테스트 실행으로 확인하십시오

## 변경 유형 판단 가이드

다음 결정 트리를 사용하십시오:

1. 테스트 결과가 달라지는가?
   - 예 → `[BEHAVIORAL]`
   - 아니오 → 2번으로 이동

2. 프로덕션 코드의 동작이 변경되는가?
   - 예 → `[BEHAVIORAL]`
   - 아니오 → `[STRUCTURAL]`

### 일반적인 상황별 분류

**`[STRUCTURAL]` 사용 케이스 (코드 동작 변경 없음):**
- 변수/메서드 이름 변경
- 코드 추출 (Extract Method/Variable)
- 코드 이동 (Move Method/Class)
- 중복 코드 제거
- 패키지/의존성 추가 (아직 사용하지 않는 경우)
- 빌드 스크립트 변경 (동작 변경 없음)

**`[BEHAVIORAL]` 사용 케이스 (코드 동작 변경 있음):**
- 새로운 기능 추가
- 버그 수정
- 예외 처리 추가/변경
- 알고리즘 변경
- 테스트 추가/수정
- 패키지 추가 후 바로 사용

**CHANGE_TYPE 없음 (코드 외 변경):**
- 문서 수정 (README, .md 파일 등)
- 코드 포맷팅만 변경 (공백, 들여쓰기 정리 등)
- .gitignore, .editorconfig 등 설정 파일만 변경

## 접두사 (prefix)와 변경 유형 매핑

접두사는 변경 유형을 자동으로 결정합니다:

**항상 `[BEHAVIORAL]` (코드 동작 변경 있음):**
- `feat:` - 새로운 기능 추가
- `fix:` - 버그 수정
- `test:` - 테스트 코드 추가/수정

**항상 `[STRUCTURAL]` (코드 동작 변경 없음):**
- `refactor:` - 리팩터링 (동작 변경 없이 코드 구조 개선)
- `chore:` - 빌드, 패키지 의존성 등 (코드에 영향을 주는 경우)

**항상 CHANGE_TYPE 없음 (코드 외 변경):**
- `docs:` - 문서 수정 (.md 파일 등)
- `style:` - 코드 포맷팅만 변경 (공백, 들여쓰기 등)
- `chore:` - 설정 파일만 변경 (.gitignore, .editorconfig 등)

**규칙: 접두사를 보고 변경 유형을 즉시 판단할 수 있어야 합니다.**

## 커밋 메시지 예시

### 구조적 변경 (STRUCTURAL) - 리팩터링
```
[STRUCTURAL] refactor: 테스트 픽스처에 계산기 인스턴스 생성 추출

- 각 테스트에서 중복된 Calculator 인스턴스 생성 제거
- readonly _calculator 필드를 가진 생성자 추가
- 동작 변경 없음, 모든 테스트 통과
```

### 구조적 변경 (STRUCTURAL) - 패키지 추가
```
[STRUCTURAL] chore: LanguageExt.Core 패키지 추가

- LanguageExt.Core 4.4.9 버전 설치
- 아직 사용하지 않음 (준비 단계)
- 동작 변경 없음, 모든 테스트 통과
```

### 코드 외 변경 - 문서 수정
```
docs: README에 빌드 명령어 설명 추가

- dotnet build 명령어 사용법 추가
- 테스트 실행 방법 설명 추가
```

### 코드 외 변경 - 포맷팅
```
style: 코드 들여쓰기 통일

- 탭을 공백 4칸으로 변경
- 로직 변경 없음
```

### 기능적 변경 (BEHAVIORAL) - 새 기능
```
[BEHAVIORAL] feat: 정수 나누기 함수 구현

- Calculator 클래스에 Divide 메서드 추가
- 피제수를 제수로 나눈 몫 반환
- 테스트: Divide_ReturnsQuotient_WhenDividingTwoPositiveNumbers 통과
```

### 기능적 변경 (BEHAVIORAL) - 버그 수정
```
[BEHAVIORAL] fix: 0으로 나누기 예외 처리

- 제수가 0일 때 유효성 검사 추가
- 제수가 0이면 DivideByZeroException 발생
- 테스트: Divide_ThrowsException_WhenDivisorIsZero 통과
```

### 기능적 변경 (BEHAVIORAL) - 테스트 추가
```
[BEHAVIORAL] test: 음수 나누기 테스트 케이스 추가

- 음수 피제수와 양수 제수 테스트 추가
- 음수 몫이 올바르게 반환되는지 검증
- 테스트: Divide_ReturnsNegativeQuotient_WhenDividendIsNegative 통과
```

## 잘못된 예시

```
[SETUP] chore: 패키지 추가                   (X) [SETUP]은 존재하지 않음
[BEHAVIORAL] refactor: 메서드 이름 변경      (X) refactor는 항상 STRUCTURAL
[STRUCTURAL] feat: 새 기능 추가              (X) feat는 항상 BEHAVIORAL
[STRUCTURAL] test: 테스트 추가               (X) test는 항상 BEHAVIORAL
[STRUCTURAL] docs: README 업데이트           (X) docs는 CHANGE_TYPE 없음
[BEHAVIORAL] style: 코드 포맷팅              (X) style은 CHANGE_TYPE 없음
```

## 올바른 예시 - 접두사와 타입 일치

**코드 변경 (CHANGE_TYPE 포함):**
```
[STRUCTURAL] chore: 패키지 추가              (O) 코드에 영향
[STRUCTURAL] refactor: 메서드 이름 변경      (O) 동작 변경 없음
[BEHAVIORAL] feat: 새 기능 추가              (O) 동작 변경 있음
[BEHAVIORAL] fix: 버그 수정                  (O) 동작 변경 있음
[BEHAVIORAL] test: 테스트 추가               (O) 동작 변경 있음
```

**코드 외 변경 (CHANGE_TYPE 없음):**
```
docs: README 업데이트                        (O) 문서만 수정
style: 코드 포맷팅                          (O) 포맷팅만 변경
chore: .gitignore 업데이트                  (O) 설정 파일만 변경
```

## 커밋 빈도

- 크고 드문 커밋보다 **작고 잦은 커밋**을 사용하십시오
- TDD 사이클마다 커밋하는 것을 권장합니다:
  1. GREEN 단계 완료 후 커밋 (기능적 변경: behavioral)
  2. REFACTOR 단계 완료 후 커밋 (구조적 변경: structural)

## 커밋 순서

구조적(structural) 변경과 기능적(behavioral) 변경이 모두 필요한 경우:
1. 먼저 구조적(structural) 변경을 커밋하십시오
2. 테스트를 실행하여 동작이 변경되지 않았음을 확인하십시오
3. 그 다음 기능적(behavioral) 변경을 커밋하십시오

## 커밋 메시지 금지 사항

**커밋 메시지는 순수하게 변경 내용만 설명해야 합니다.**

다음 내용은 절대 포함하지 마십시오:
- `Generated with [Claude Code]` 등 Claude/AI 생성 관련 메시지
- `Co-Authored-By: Claude` 등 공동 저자 표시
- Claude, AI, 자동 생성 관련 모든 언급
- 이모지 (명시적으로 요청하지 않은 경우)

## 커밋 금지 사항

**코드 변경 시:**
- 구조적(structural) 변경과 기능적(behavioral) 변경을 같은 커밋에 포함하지 마십시오
- 테스트가 실패한 상태로 커밋하지 마십시오
- 컴파일러/린터 경고가 있는 상태로 커밋하지 마십시오
- 코드 변경과 문서 변경을 같은 커밋에 포함하지 마십시오

## 커밋 절차

1. `git status`로 변경사항을 확인하십시오
2. `git diff`로 변경 내용을 검토하십시오
3. `git log`로 최근 커밋 메시지 스타일을 확인하십시오
4. 변경사항을 논리적 단위로 분리하여 커밋하십시오

## 완료 메시지

커밋이 성공적으로 완료되면 다음과 같이 표시합니다:

```
✅ 커밋 완료

커밋 정보:
  - 타입: [STRUCTURAL/BEHAVIORAL/없음]
  - 접두사: feat/fix/refactor/test/docs/style/chore
  - 메시지: {커밋 메시지 첫 줄}
  - 변경 파일: N개
```

## 중요 사항

- **TDD 사이클 준수**: GREEN 후 기능적 커밋, REFACTOR 후 구조적 커밋
- **원자적 커밋**: 하나의 커밋은 하나의 논리적 변경만 포함
- **테스트 통과 필수**: 코드 변경 시 모든 테스트가 통과해야 함
- **AI 관련 언급 금지**: Claude, AI 생성 관련 메시지 절대 포함 금지
