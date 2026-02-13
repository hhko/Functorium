# CLAUDE.md

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

## 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

## 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

## 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it, then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

## Functorium 프로젝트 가이드
### 솔루션 파일

이 프로젝트는 `.slnx` 확장자를 사용하며, 두 개의 솔루션 파일이 있습니다.

| 솔루션 파일 | 포함 프로젝트 | 용도 |
|-------------|---------------|------|
| `Functorium.slnx` | Src/, Tests/ | 핵심 라이브러리 개발 (기본) |
| `Functorium.All.slnx` | 전체 프로젝트 | Tutorials, Books 포함 전체 빌드 |

- 빌드: `dotnet build Functorium.slnx`
- 테스트: `dotnet test --solution Functorium.slnx`
- 전체 빌드: `dotnet build Functorium.All.slnx`
- 전체 테스트: `dotnet test --solution Functorium.All.slnx`

> 'dotnet test'에 대한 솔루션을 지정하려면 '--solution'을 통해 지정해야 합니다.

### 빌드 스크립트

| 스크립트 | 용도 |
|----------|------|
| `Build-Local.ps1` | 빌드, 테스트, 코드 커버리지, NuGet 패키지 생성 |
| `Build-Clean.ps1` | 빌드 아티팩트 정리 |
| `Build-CleanRunFileCache.ps1` | .NET 파일 기반 프로그램 캐시 정리 |
| `Build-VerifyAccept.ps1` | Verify.Xunit 스냅샷 테스트 승인 |

#### Build-Local.ps1

```powershell
# 기본 솔루션 빌드 (Functorium.slnx)
./Build-Local.ps1

# 전체 솔루션 빌드
./Build-Local.ps1 -s Functorium.All.slnx

# NuGet 패키지 생성 건너뛰기
./Build-Local.ps1 -SkipPack
```

#### Build-CleanRunFileCache.ps1

.NET 10 파일 기반 프로그램(`.cs` 직접 실행) 캐시 정리 스크립트입니다.
`System.CommandLine` 패키지 로딩 오류 발생 시 사용합니다.

```powershell
# SummarizeSlowestTests 캐시만 정리
./Build-CleanRunFileCache.ps1

# 모든 runfile 캐시 정리
./Build-CleanRunFileCache.ps1 -Pattern "All"

# 삭제 대상만 확인
./Build-CleanRunFileCache.ps1 -WhatIf
```

#### Build-VerifyAccept.ps1

Verify.Xunit 스냅샷 테스트 결과를 승인하는 스크립트입니다.
테스트 실행 후 pending 상태의 스냅샷을 일괄 승인할 때 사용합니다.

```powershell
# 모든 pending 스냅샷 승인
./Build-VerifyAccept.ps1
```

### 커밋 규칙

커밋 시 `.claude/commands/commit.md`의 규칙을 준수하십시오.

### 단위 테스트 규칙

단위 테스트 구현 시 `.claude/guides/unit-testing-guide.md`의 규칙을 준수하십시오.
