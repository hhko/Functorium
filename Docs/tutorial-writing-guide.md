---
title: "Tutorial 쓰기 가이드"
---

이 문서는 `Docs.Site/src/content/docs/tutorials/` 폴더의 튜토리얼 문서를 작성할 때 따라야 할 지침입니다.

---

## 1. 튜토리얼 전체 구조

각 튜토리얼은 독립된 루트 디렉토리에 다음 요소를 포함합니다.

**필수 파일:**

| 파일 | 설명 |
|------|------|
| `index.md` | 튜토리얼 랜딩 페이지 |
| `{tutorial-name}.slnx` | 솔루션 파일 |
| `Directory.Build.props` | .NET 빌드 설정 |
| `Directory.Build.targets` | 루트 상속 차단 |

**디렉토리 패턴:**

```txt
architecture-rules/
├── index.md
├── architecture-rules.slnx
├── Directory.Build.props
├── Directory.Build.targets
├── Part0-Introduction/
│   ├── 01-why-architecture-testing.md
│   ├── 02-archunitnet-and-functorium.md
│   └── 03-environment-setup.md
├── Part1-ClassValidator-Basics/
│   ├── 01-First-Architecture-Test/
│   │   ├── index.md
│   │   ├── FirstArchitectureTest/
│   │   │   └── FirstArchitectureTest.csproj
│   │   └── FirstArchitectureTest.Tests.Unit/
│   │       └── FirstArchitectureTest.Tests.Unit.csproj
│   ├── 02-Visibility-And-Modifiers/
│   ├── 03-Naming-Rules/
│   └── 04-Inheritance-And-Interface/
├── Part2-Method-And-Property-Validation/
├── Part3-Advanced-Validation/
├── Part4-Real-World-Patterns/
├── Part5-Conclusion/
│   ├── 01-best-practices.md
│   └── 02-next-steps.md
└── Appendix/
    ├── A-api-reference.md
    ├── B-archunitnet-cheatsheet.md
    └── C-faq.md
```

---

## 2. 네이밍 규칙

| 항목 | 규칙 | 예시 |
|------|------|------|
| 튜토리얼 루트 폴더 | kebab-case | `functional-valueobject` |
| Part 폴더 | PascalCase | `Part1-ValueObject-Concepts` |
| 장 폴더 (코드 포함) | `NN-PascalCase` | `01-First-Architecture-Test` |
| 장 파일 (MD만) | `NN-kebab-case.md` | `01-why-architecture-testing.md` |
| C# 프로젝트 폴더 | PascalCase | `FirstArchitectureTest` |
| 테스트 프로젝트 폴더 | `ProjectName.Tests.Unit` | `FirstArchitectureTest.Tests.Unit` |
| 부록 파일 | `X-kebab-case.md` | `A-api-reference.md`, `B-archunitnet-cheatsheet.md` |
| 장 문서 | `index.md` | ~~`README.md`~~ → `index.md` (Starlight 규칙) |

---

## 3. 폴더 구조 규칙

튜토리얼의 Part 폴더 내부 구조는 **파일 개수에 따라** 두 가지 규칙을 적용합니다.

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

장(chapter)에 MD 파일 외에 **코드, 이미지 등 2개 이상 파일**이 있을 때는 **폴더로 구성하고 `index.md` 사용**합니다.

```txt
Part1-ClassValidator-Basics/
├── 01-First-Architecture-Test/
│   ├── index.md
│   ├── FirstArchitectureTest/
│   │   ├── FirstArchitectureTest.csproj
│   │   ├── Program.cs
│   │   └── Domains/
│   │       └── Employee.cs
│   └── FirstArchitectureTest.Tests.Unit/
│       ├── FirstArchitectureTest.Tests.Unit.csproj
│       ├── xunit.runner.json
│       └── ArchitectureTests.cs
└── 02-Visibility-And-Modifiers/
    ├── index.md
    └── ...
```

**폴더명 규칙:**

| 항목 | 규칙 | 예시 |
|------|------|------|
| 대소문자 | PascalCase | `01-First-Architecture-Test/` |
| 번호 접두사 | `01-`, `02-`, ... | `02-Visibility-And-Modifiers/` |
| 장 내용 | `index.md`에 작성 | - |

**C# 프로젝트 폴더 구조:**

C# 프로젝트가 포함된 장은 반드시 다음 구조를 따릅니다:

```txt
01-Topic-Name/                          # 장 폴더 (번호-PascalCase)
├── index.md                            # 장 설명 문서
├── ProjectName/                        # 메인 프로젝트 폴더
│   ├── ProjectName.csproj              # 프로젝트 파일
│   ├── Program.cs                      # 진입점
│   ├── AssemblyReference.cs            # (선택) 어셈블리 참조
│   └── Domains/                        # 소스 파일들
│       └── ...
└── ProjectName.Tests.Unit/             # 테스트 프로젝트 폴더
    ├── ProjectName.Tests.Unit.csproj   # 테스트 프로젝트 파일
    ├── xunit.runner.json               # xUnit 설정
    └── ...Tests.cs                     # 테스트 파일들
```

**핵심 규칙:**

| 항목 | 올바른 예시 | 잘못된 예시 |
|------|-------------|-------------|
| 메인 프로젝트 | `01-Topic/ProjectName/ProjectName.csproj` | `01-Topic/ProjectName.csproj` |
| 테스트 프로젝트 | `01-Topic/ProjectName.Tests.Unit/...` | `01-Topic/Tests/...` |
| ProjectReference | `..\ProjectName\ProjectName.csproj` | `..\ProjectName.csproj` |

**csproj 파일 템플릿:**

메인 프로젝트:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="LanguageExt.Core" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
  </ItemGroup>
</Project>
```

테스트 프로젝트:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="LanguageExt.Core" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit.v3" />
    <!-- ... 기타 테스트 패키지 -->
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\ProjectName\ProjectName.csproj" />
  </ItemGroup>
</Project>
```

### 규칙 적용 기준

| 상황 | 적용 규칙 | 결과 |
|------|----------|------|
| 개념 설명만 있는 장 | 규칙 1 | `Part0/01-concept.md` |
| 코드 예제가 포함된 장 | 규칙 2 | `Part1/01-Concept/index.md` + 코드 폴더 |
| 이미지가 포함된 장 | 규칙 2 | `Part1/01-Concept/index.md` + `images/` |

---

## 4. YAML Frontmatter

모든 `.md` 파일에 YAML frontmatter가 필수입니다.

```yaml
---
title: "한글 제목"
---
```

**규칙:**

| 위치 | 예시 |
|------|------|
| 튜토리얼 루트 `index.md` | `title: "아키텍처 규칙 테스트"` |
| 장 문서 `index.md` | `title: "첫 번째 아키텍처 테스트"` |
| 개념 설명 `01-xxx.md` | `title: "왜 아키텍처 테스트인가?"` |
| 부록 `A-xxx.md` | `title: "API 레퍼런스"` |

> **참고**: 사이드바 순서와 라벨은 frontmatter가 아닌 `astro.config.mjs`에서 관리합니다.

---

## 5. 튜토리얼 루트 index.md 구조

실제 튜토리얼에서 추출한 공통 패턴입니다.

````markdown
---
title: "튜토리얼 한글 제목"
---

**한 줄 강조 설명 (bold)**

---

## 이 튜토리얼에 대하여

문제 상황을 제시하는 hook 문단. 독자가 "왜 이것이 필요한가"를 느낄 수 있도록 실무 고충으로 시작합니다.

> **"핵심 인사이트를 담은 인용구"**

### 대상 독자

| 수준 | 대상 | 권장 학습 범위 |
|------|------|----------------|
| **초급** | ... | Part 0~1 |
| **중급** | ... | Part 2~3 |
| **고급** | ... | Part 4~5 + 부록 |

### 학습 목표

이 튜토리얼을 완료하면 다음을 할 수 있습니다:

1. **목표 1 제목**
   - 세부 내용
2. **목표 2 제목**
   - 세부 내용

---

### Part 0: 서론

파트에 대한 한줄 설명.

- [0.1 장 제목](Part0-Introduction/01-xxx.md)
- [0.2 장 제목](Part0-Introduction/02-xxx.md)

### Part 1: 본문 제목

파트에 대한 한줄 설명.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [장 제목](Part1-xxx/01-Xxx/) | 핵심 내용 |
| 2 | [장 제목](Part1-xxx/02-Xxx/) | 핵심 내용 |

### Part 5: 결론

- [5.1 장 제목](Part5-Conclusion/01-xxx.md)
- [5.2 장 제목](Part5-Conclusion/02-xxx.md)

### [부록](Appendix/)

- [A. 부록 제목](Appendix/A-xxx.md)
- [B. 부록 제목](Appendix/B-xxx.md)

---

## 핵심 진화 과정

```
[Part 1] 제목
1장: ...  →  2장: ...  →  3장: ...

[Part 2] 제목
1장: ...  →  2장: ...  →  3장: ...
```

#### 핵심 진화 과정 다이어그램 형식

`[Part N] 제목` 헤더 아래에 장 흐름을 `→`로 연결합니다.

```
[Part 0] 도입
1장: ...  →  2장: ...  →  3장: ...

[Part 1] 제목
1장: ...  →  2장: ...  →  3장: ...  →  4장: ...
```

---

## 프로젝트 구조

```txt
tutorial-name/
├── index.md
├── tutorial-name.slnx
├── Directory.Build.props
├── Directory.Build.targets
├── Part0-Introduction/
│   └── ...
├── Part1-xxx/
│   └── ...
└── Appendix/
    └── ...
```

## 테스트 실행

```bash
# 전체 솔루션 테스트
dotnet test --solution tutorial-name.slnx
```

## 테스트 프로젝트 목록

| Part | 장 | 테스트 프로젝트 |
|:----:|:---:|----------------|
| 1 | 1 | `ProjectName.Tests.Unit` |
| 1 | 2 | `ProjectName.Tests.Unit` |
````

---

## 6. 장(Chapter) 문서 구조

실제 장 파일에서 추출한 공통 패턴입니다.

````markdown
---
title: "장 한글 제목"
---
## 개요

문제 상황을 제시하는 hook 문단. 실무 고충이나 구체적인 시나리오로 시작합니다.

> **"핵심 인사이트를 담은 인용구"**

## 학습 목표

### 핵심 학습 목표
1. **목표 1**
   - 세부 내용
2. **목표 2**
   - 세부 내용

### 실습을 통해 확인할 내용
- 확인 사항 1
- 확인 사항 2

## 프로젝트 구조

```
01-Chapter-Name/
├── ProjectName/
│   ├── ProjectName.csproj
│   └── Domains/
│       └── Example.cs
└── ProjectName.Tests.Unit/
    ├── ProjectName.Tests.Unit.csproj
    └── ExampleTests.cs
```

## 검증 대상 코드

### FileName.cs

```csharp
// 실제 C# 코드
```

코드에 대한 설명 문단.

## 테스트 코드 설명

### 테스트 주제

```csharp
// 테스트 코드
```

테스트 코드 해설.

## 한눈에 보는 정리

| 구성요소 | 역할 |
|----------|------|
| **ComponentA** | 설명 |
| **ComponentB** | 설명 |

## FAQ

### Q1: 질문?
**A**: 답변.

### Q2: 질문?
**A**: 답변.

---

다음 장에서는 ...을 배웁니다.
````

#### 장 끝 전환 패턴

각 장의 마지막에는 다음 장으로의 전환 패턴을 사용합니다. `## 다음 단계` 같은 별도 섹션 제목 없이, 수평선 + 전환문 + 화살표 링크만 사용합니다.

```
---

{다음 장 내용을 1~2문장으로 예고하는 전환문}

→ [N장: 제목](상대경로)
```

- 튜토리얼의 마지막 장(부록 마지막 파일)에는 전환 패턴을 넣지 않습니다.
- FAQ 항목에 "다음 장에서는 무엇을 배우나요?" 같은 중복 내용을 넣지 않습니다.

---

## 7. 목차 구조

튜토리얼의 index.md 목차는 **Part 유형에 따라** 두 가지 형식을 사용합니다.

### 형식 1: 리스트 형식

서론, 결론, 부록처럼 **장 수가 적은 Part**(3개 이하)는 리스트 형식을 사용합니다.

````markdown
### Part 0: 서론

파트에 대한 한줄 설명.

- [0.1 첫 번째 장](Part0-Introduction/01-first-topic.md)
- [0.2 두 번째 장](Part0-Introduction/02-second-topic.md)
- [0.3 세 번째 장](Part0-Introduction/03-third-topic.md)

### [부록](Appendix/)

- [A. 부록 제목](Appendix/A-appendix-name.md)
- [B. 부록 제목](Appendix/B-appendix-name.md)
````

### 형식 2: 테이블 형식

본문 Part처럼 **장 수가 많은 Part**(4개 이상)는 테이블 형식을 사용합니다.

````markdown
### Part 1: 기초

파트에 대한 한줄 설명.

| 장 | 주제 | 핵심 학습 내용 |
|:---:|------|----------------|
| 1 | [장 제목](Part1-Fundamentals/01-Topic-Name/) | 핵심 내용 |
| 2 | [장 제목](Part1-Fundamentals/02-Topic-Name/) | 핵심 내용 |
| 3 | [장 제목](Part1-Fundamentals/03-Topic-Name/) | 핵심 내용 |
| 4 | [장 제목](Part1-Fundamentals/04-Topic-Name/) | 핵심 내용 |
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
| 테이블 장 | `\| N \| [제목](경로) \| 설명 \|` | 대규모 Part용 |
| 부록 링크 | `### [부록](Appendix/)` | Appendix 폴더 링크 포함 |

---

## 8. .slnx 솔루션 파일

각 튜토리얼 루트에 `{tutorial-name}.slnx` 파일을 배치합니다.

**위치:** `tutorials/{tutorial-name}/{tutorial-name}.slnx`

**형식:**

```xml
<Solution>
  <Folder Name="/Part1-ClassValidator-Basics/" />
  <Folder Name="/Part1-ClassValidator-Basics/01-First-Architecture-Test/">
    <Project Path="Part1-ClassValidator-Basics/01-First-Architecture-Test/FirstArchitectureTest.Tests.Unit/FirstArchitectureTest.Tests.Unit.csproj" />
    <Project Path="Part1-ClassValidator-Basics/01-First-Architecture-Test/FirstArchitectureTest/FirstArchitectureTest.csproj" />
  </Folder>
  <Folder Name="/Part1-ClassValidator-Basics/02-Visibility-And-Modifiers/">
    <Project Path="Part1-ClassValidator-Basics/02-Visibility-And-Modifiers/VisibilityAndModifiers.Tests.Unit/VisibilityAndModifiers.Tests.Unit.csproj" />
    <Project Path="Part1-ClassValidator-Basics/02-Visibility-And-Modifiers/VisibilityAndModifiers/VisibilityAndModifiers.csproj" />
  </Folder>
  <!-- ... -->
</Solution>
```

**규칙:**

| 항목 | 설명 |
|------|------|
| 루트 요소 | `<Solution>` |
| Part 폴더 | `<Folder Name="/PartN-xxx/" />` (빈 폴더 선언) |
| 장 폴더 | `<Folder Name="/PartN-xxx/NN-Chapter/">` (하위에 Project 포함) |
| 프로젝트 경로 | 솔루션 파일 기준 **상대 경로** |
| 프로젝트 순서 | Tests.Unit 프로젝트를 먼저, 메인 프로젝트를 나중에 |

---

## 9. Directory.Build.props/targets

모든 튜토리얼은 루트에 동일한 빌드 설정 파일을 포함합니다.

### Directory.Build.props

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <!-- Microsoft Testing Platform -->
  <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <OutputType>Exe</OutputType>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>
</Project>
```

### Directory.Build.targets

```xml
<Project>
  <!-- 루트 Directory.Build.targets 상속 차단 -->
</Project>
```

`Directory.Build.targets`는 내용이 비어 있지만 **필수**입니다. 이 파일이 존재해야 리포지토리 루트의 `Directory.Build.targets`를 상속받지 않으며, 튜토리얼이 독립된 빌드 환경을 유지할 수 있습니다.

---

## 10. astro.config.mjs 사이드바 등록

새 튜토리얼을 추가하면 `Docs.Site/astro.config.mjs`의 tutorials 섹션에 등록해야 합니다.

**등록 패턴:**

```javascript
{
  label: '튜토리얼 한글 제목',
  collapsed: true,
  items: [
    // 1. 튜토리얼 루트 index.md
    { slug: 'tutorials/tutorial-name' },

    // 2. Part0 — autogenerate (장 수가 적고 코드 없는 서론)
    { label: '소개', autogenerate: { directory: 'tutorials/tutorial-name/Part0-Introduction' } },

    // 3. Part1~N — 수동 items (장별 slug 명시)
    {
      label: 'Part 한글 제목',
      items: [
        { slug: 'tutorials/tutorial-name/part1-xxx/01-chapter-name' },
        { slug: 'tutorials/tutorial-name/part1-xxx/02-chapter-name' },
      ],
    },

    // 4. 결론 — autogenerate
    { label: '결론', autogenerate: { directory: 'tutorials/tutorial-name/Part5-Conclusion' } },

    // 5. 부록 — autogenerate + collapsed
    { label: '부록', collapsed: true, autogenerate: { directory: 'tutorials/tutorial-name/Appendix' } },
  ],
},
```

**slug 변환 규칙:**

파일시스템 경로의 PascalCase는 slug에서 자동으로 kebab-case(소문자)로 변환됩니다.

| 파일시스템 경로 | slug |
|----------------|------|
| `Part1-ClassValidator-Basics/01-First-Architecture-Test/` | `part1-classvalidator-basics/01-first-architecture-test` |
| `Part0-Introduction/` | `tutorials/tutorial-name/Part0-Introduction` (autogenerate용 directory) |

**autogenerate vs 수동 items 선택 기준:**

| 조건 | 방식 | 예시 |
|------|------|------|
| 장 순서/구조가 단순 | `autogenerate` | Part0 서론, Part5 결론, 부록 |
| 장 순서를 정밀 제어 | 수동 `items` 배열 | Part1~4 본문 |

---

## 11. 빌드 및 검증

새 튜토리얼이나 장을 추가한 후 반드시 다음 빌드/검증을 수행합니다.

### C# 빌드 및 테스트

```bash
# 튜토리얼 전체 빌드
dotnet build {tutorial-name}.slnx

# 튜토리얼 전체 테스트
dotnet test --solution {tutorial-name}.slnx

# Build-Local.ps1 사용
./Build-Local.ps1 -s Docs.Site/src/content/docs/tutorials/{tutorial-name}/{tutorial-name}.slnx
```

### 문서 빌드 및 링크 검증

```bash
# Docs.Site/ 디렉토리에서 실행
npx astro build
```

`starlight-links-validator` 플러그인이 빌드 시 링크를 자동 검증합니다.

---

## 12. 내러티브 작성 원칙

사양서(spec) 문체가 아닌 **기술도서 문체**로 작성합니다. `guide-writing-guide.md`의 내러티브 원칙을 튜토리얼에 맞게 적용합니다.

### 1. 개요는 문제 상황(hook)으로 시작

독자가 "왜 이것이 필요한가"를 느낄 수 있도록 실무 고충이나 구체적인 시나리오로 시작합니다.

```markdown
# Before (사양서 문체)
이 장에서는 ClassValidator의 RequirePublic 메서드를 다룹니다.

# After (기술도서 문체)
`Employee` 클래스가 `public sealed`인지 매번 코드 리뷰에서 눈으로 확인하고 있나요?
클래스가 50개, 100개로 늘어나면 놓치는 것은 시간 문제입니다.
```

### 2. 코드 블록 앞에 맥락 문장

주요 코드 블록 앞에 "이 코드에서 주목할 점"을 안내합니다.

```markdown
다음 코드에서 주목할 점은 `ValidateAllClasses`가 각 클래스에 **ClassValidator를** 적용한다는 것입니다.
```

### 3. 장 간 전환 문장으로 연결

장의 끝에서 다음 장을 예고하는 1~2문장을 추가합니다.

```markdown
다음 장에서는 `RequireInternal`, `RequireAbstract` 등 다양한 가시성과 한정자 검증 방법을 배웁니다.
```

### 4. 테이블 도입 문장

`## 한눈에 보는 정리` 등 테이블을 포함하는 섹션에서는, 테이블 바로 위에 도입 문장을 작성합니다.

```markdown
## 한눈에 보는 정리

이 장에서 다룬 핵심 내용을 정리합니다.

| 항목 | 설명 |
|------|------|
| ... | ... |
```

---

## 13. 코드 블록 작성

### 언어 지정

모든 코드 블록에 언어를 명시합니다.

| 언어 | 지정자 |
|------|--------|
| C# | `csharp` |
| Bash/Shell | `bash` |
| PowerShell | `powershell` |
| YAML | `yaml` |
| JSON | `json` |
| XML | `xml` |
| Markdown | `markdown` |
| 일반 텍스트 | `txt` |
| Diff | `diff` |

### 중첩 코드 블록

마크다운 예시를 보여줄 때 코드 블록 안에 또 다른 코드 블록이 포함되면 렌더링이 깨집니다.

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

## 14. 체크리스트

문서 작성 완료 전 확인:

- [ ] 모든 `.md` 파일에 YAML frontmatter (`title`) 확인
- [ ] 장 문서 파일명이 `index.md` (not `README.md`)
- [ ] 폴더 구조 규칙 적용 (MD만 → 파일, 2개+ → 폴더/`index.md`)
- [ ] C# 프로젝트 구조 확인 (`01-Topic/ProjectName/ProjectName.csproj`)
- [ ] 테스트 프로젝트 ProjectReference 경로 확인 (`..\ProjectName\ProjectName.csproj`)
- [ ] `.slnx`에 새 프로젝트 등록
- [ ] `Directory.Build.props`/`Directory.Build.targets` 존재
- [ ] `astro.config.mjs` 사이드바 등록
- [ ] 목차 형식 확인 (서론/결론/부록 → 리스트, 본문 → 테이블)
- [ ] 모든 코드 블록에 언어 지정
- [ ] 중첩 코드 블록이 있으면 외부에 백틱 4개 사용
- [ ] `dotnet test --solution {tutorial-name}.slnx` 통과
- [ ] `npx astro build` 통과 (Docs.Site/)
