# CLAUDE.md

## Functorium 프로젝트 가이드
### 솔루션 파일

이 프로젝트는 `.slnx` 확장자를 사용합니다.

| 솔루션 파일 | 포함 프로젝트 | 용도 |
|-------------|---------------|------|
| `Functorium.slnx` | Src/, Tests/ | 핵심 라이브러리 개발 (기본) |
| `Docs.Site/.../tutorials/<name>/<PascalName>.slnx` | 튜토리얼별 개별 솔루션 (6개) | 문서 내 실습 코드 빌드 |
| `Docs.Site/.../samples/<name>/<PascalName>.slnx` | 샘플별 개별 솔루션 (3개) | 문서 내 실습 코드 빌드 |
| `Docs.Site/.../quickstart/Quickstart.slnx` | 퀵스타트 솔루션 (1개) | 문서 내 실습 코드 빌드 |

- 빌드: `dotnet build Functorium.slnx`
- 테스트: `dotnet test --solution Functorium.slnx`
- 튜토리얼 빌드: `dotnet build Docs.Site/src/content/docs/tutorials/<name>/<PascalName>.slnx`
- 튜토리얼 테스트: `dotnet test --solution Docs.Site/src/content/docs/tutorials/<name>/<PascalName>.slnx`

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

# 특정 튜토리얼 빌드
./Build-Local.ps1 -s Docs.Site/src/content/docs/tutorials/functional-valueobject/FunctionalValueObject.slnx

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

### Git Hooks

이 프로젝트는 `.githooks/` 디렉토리에 커밋 메시지 정리 hook을 관리합니다.

- **커밋 전 필수 확인**: `git config core.hooksPath`가 `.githooks`를 가리키는지 확인하십시오.
- 설정되지 않았거나 다른 경로를 가리키면: `git config core.hooksPath .githooks`로 설정하십시오.
- `.githooks/commit-msg`: Claude/AI 관련 텍스트를 커밋 메시지에서 자동 제거합니다.

### 커밋 규칙

커밋 시 `.claude/commands/commit.md`의 규칙을 준수하십시오.

### 단위 테스트 규칙

단위 테스트 구현 시 `Docs.Site/src/content/docs/guides/testing/15a-unit-testing.md`의 규칙을 준수하십시오.

### Markdown 볼드 작성 규칙

`**텍스트(...)**` 뒤에 한글 조사가 바로 오면 CommonMark의 right-flanking delimiter 규칙에 의해 GitHub에서 볼드가 렌더링되지 않습니다. `)` 앞의 내용이 영어든 숫자든 상관없이 동일합니다. 한글 조사를 볼드 안에 포함시키십시오.

```markdown
# Bad  - GitHub에서 볼드 렌더링 실패
**공변성(Covariance)**은
**불변식 가드(1, 2)**와

# Good - 한글 조사를 볼드 안으로
**공변성(Covariance)은**
**불변식 가드(1, 2)와**
```
