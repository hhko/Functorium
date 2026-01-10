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

## 커밋 규칙

커밋 시 `.claude/commands/commit.md`의 규칙을 준수하십시오.

## 단위 테스트 규칙

단위 테스트 구현 시 `.claude/guides/unit-testing-guide.md`의 규칙을 준수하십시오.
