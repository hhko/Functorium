# Functorium 프로젝트 가이드

## 솔루션 파일

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

## 빌드 스크립트

| 스크립트 | 용도 |
|----------|------|
| `Build-Local.ps1` | 빌드, 테스트, 코드 커버리지, NuGet 패키지 생성 |
| `Build-Clean.ps1` | 빌드 아티팩트 정리 |
| `Build-CleanRunFileCache.ps1` | .NET 파일 기반 프로그램 캐시 정리 |
| `Build-VerifyAccept.ps1` | Verify.Xunit 스냅샷 테스트 승인 |

### Build-Local.ps1

```powershell
# 기본 솔루션 빌드 (Functorium.slnx)
./Build-Local.ps1

# 전체 솔루션 빌드
./Build-Local.ps1 -s Functorium.All.slnx

# NuGet 패키지 생성 건너뛰기
./Build-Local.ps1 -SkipPack
```

### Build-CleanRunFileCache.ps1

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

### Build-VerifyAccept.ps1

Verify.Xunit 스냅샷 테스트 결과를 승인하는 스크립트입니다.
테스트 실행 후 pending 상태의 스냅샷을 일괄 승인할 때 사용합니다.

```powershell
# 모든 pending 스냅샷 승인
./Build-VerifyAccept.ps1
```

## 커밋 규칙

커밋 시 `.claude/commands/commit.md`의 규칙을 준수하십시오.

## 단위 테스트 규칙

단위 테스트 구현 시 `.claude/guides/unit-testing-guide.md`의 규칙을 준수하십시오.
