# Books 폴더 문서 작성 가이드

이 폴더의 마크다운 문서를 작성할 때 따라야 할 지침입니다.

---

## 1. 폴더 구조 규칙

책의 Part 폴더 내부 구조는 **파일 개수에 따라** 두 가지 규칙을 적용합니다.

### 규칙 1: MD 파일만 있는 경우

장(chapter)에 MD 파일 1개만 있을 때는 **폴더 없이 MD 파일로 직접 표현**합니다.

```txt
Part0-Introduction/
├── 01-what-is-source-generator.md
├── 02-why-source-generator.md
└── 03-project-overview.md
```

**파일명 규칙:**

| 항목 | 규칙 | 예시 |
|------|------|------|
| 대소문자 | 소문자 | `01-development-environment.md` |
| 단어 구분 | 하이픈(`-`) | `02-project-structure.md` |
| 번호 접두사 | `01-`, `02-`, ... | `03-debugging-setup.md` |

### 규칙 2: 2개 이상 파일이 있는 경우

장(chapter)에 MD 파일 외에 **코드, 이미지 등 2개 이상 파일**이 있을 때는 **폴더로 구성하고 README.md 사용**합니다.

```txt
Part5-Domain-Examples/
├── 01-Ecommerce-Domain/
│   ├── README.md
│   ├── EcommerceDomain/
│   │   └── ... (C# 프로젝트)
│   └── EcommerceDomain.Tests.Unit/
│       └── ... (테스트 프로젝트)
└── 02-Healthcare-Domain/
    ├── README.md
    └── HealthcareDomain/
        └── ...
```

**폴더명 규칙:**

| 항목 | 규칙 | 예시 |
|------|------|------|
| 대소문자 | PascalCase | `01-Ecommerce-Domain/` |
| 번호 접두사 | `01-`, `02-`, ... | `02-Healthcare-Domain/` |
| 장 내용 | `README.md`에 작성 | - |

### 규칙 적용 기준

| 상황 | 적용 규칙 | 결과 |
|------|----------|------|
| 개념 설명만 있는 장 | 규칙 1 | `Part1/01-concept.md` |
| 코드 예제가 포함된 장 | 규칙 2 | `Part1/01-Concept/README.md` + 코드 폴더 |
| 이미지가 포함된 장 | 규칙 2 | `Part1/01-Concept/README.md` + `images/` |

---

## 2. 목차 구조

책의 README.md 목차는 **Part 유형에 따라** 두 가지 형식을 사용합니다.

### 형식 1: 리스트 형식

서론, 결론, 부록처럼 **장 수가 적은 Part**(3개 이하)는 리스트 형식을 사용합니다.

````markdown
### Part 0: 서론

파트에 대한 한줄 설명.

- [0.1 첫 번째 장](Part0-Introduction/01-first-topic.md)
- [0.2 두 번째 장](Part0-Introduction/02-second-topic.md)
- [0.3 세 번째 장](Part0-Introduction/03-third-topic.md)

### [부록](Appendix/)

- [A. 부록 제목](Appendix/a-appendix-name.md)
- [B. 부록 제목](Appendix/b-appendix-name.md)
````

### 형식 2: 테이블 형식

본문 Part처럼 **장 수가 많은 Part**(4개 이상)는 테이블 형식을 사용합니다.

````markdown
### Part 1: 기초

파트에 대한 한줄 설명.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [개발 환경](Part1-Fundamentals/01-development-environment.md) | 개발 환경 설정 |
| 2 | [프로젝트 구조](Part1-Fundamentals/02-project-structure.md) | 프로젝트 구성 |
| 3 | [디버깅 설정](Part1-Fundamentals/03-debugging-setup.md) | 디버깅 환경 구축 |
````

### 형식 선택 기준

| Part 유형 | 장 수 | 권장 형식 |
|----------|:-----:|----------|
| Part 0 (서론) | 3개 이하 | 리스트 |
| Part N (본문) | 4개 이상 | 테이블 |
| Part N (결론) | 3개 이하 | 리스트 |
| 부록 | - | 리스트 |

### 목차 요소 설명

| 요소 | 마크다운 | 설명 |
|------|----------|------|
| Part 제목 | `### Part N: 이름` | 대분류 (h3) |
| Part 설명 | 일반 텍스트 | Part 제목 아래 한 줄 설명 |
| 리스트 장 | `- [N.M 제목](경로)` | 소규모 Part용 |
| 테이블 장 | `| N | [제목](경로) | 설명 |` | 대규모 Part용 |
| 부록 링크 | `### [부록](Appendix/)` | Appendix 폴더 링크 포함 |

---

## 3. 코드 블록 작성

### 언어 지정

모든 코드 블록에 언어를 명시합니다.

| 언어 | 지정자 |
|------|--------|
| C# | `csharp` |
| Bash/Shell | `bash` |
| PowerShell | `powershell` |
| YAML | `yaml` |
| JSON | `json` |
| Markdown | `markdown` |
| 일반 텍스트 | `txt` |
| Diff | `diff` |

### 중첩 코드 블록

마크다운 예시를 보여줄 때 코드 블록 안에 또 다른 코드 블록이 포함되면 GitHub에서 렌더링이 깨집니다.

**문제 상황:**

내부 코드 블록이 외부 블록을 닫아버려 렌더링이 깨집니다.

**해결 방법:**

외부 코드 블록에 백틱 4개(````)를 사용합니다.

`````markdown
````markdown
## 예시
```csharp
var x = 1;
```
````
`````

**규칙:** 내부에 백틱 3개 코드 블록이 있으면 외부는 반드시 백틱 4개 사용

---

## 4. 기타 작성 규칙

### ASCII 박스 정렬

한글과 영문이 섞인 ASCII 박스를 그릴 때 고정폭 폰트에서 정렬이 맞지 않을 수 있습니다.

**원인:**

- 한글 문자: 2칸 차지
- 영문/숫자/기호: 1칸 차지

**해결 방법:**

한글 문자 수만큼 공백을 줄여서 보정합니다.

```txt
영문만:    │ Hello World │  (11자 + 공백 2개 = 13칸)
한글 포함: │ 안녕 World  │  (안녕=4칸 + World=5칸 + 공백 2개 = 11칸 → 13칸)
```

---

## 5. 체크리스트

문서 작성 완료 전 확인:

- [ ] 폴더 구조 규칙 적용 (MD만 → 파일, 2개+ → 폴더/README.md)
- [ ] 목차 형식 확인 (서론/결론/부록 → 리스트, 본문 → 테이블)
- [ ] 모든 코드 블록에 언어 지정
- [ ] 중첩 코드 블록이 있으면 외부에 백틱 4개 사용
- [ ] ASCII 박스의 한글 정렬 확인
- [ ] GitHub에서 렌더링 미리보기 확인
