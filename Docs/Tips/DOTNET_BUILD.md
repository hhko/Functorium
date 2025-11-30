# .NET 빌드 명령어 가이드

## 패키지 복원

```bash
dotnet restore <solution-file>
```

NuGet 패키지 의존성을 복원합니다.

## 보안 취약점 검사

```bash
dotnet list <solution-file> package --vulnerable --include-transitive
```

| 옵션 | 설명 |
|------|------|
| `--vulnerable` | 알려진 보안 취약점이 있는 패키지 표시 |
| `--include-transitive` | 간접 의존성(transitive) 패키지도 검사 |

## 빌드

```bash
dotnet build <solution-file> --no-restore -c Release
```

| 옵션 | 설명 |
|------|------|
| `--no-restore` | 빌드 전 복원 단계 건너뛰기 (이미 복원된 경우) |
| `-c Release` | Release 구성으로 빌드 |
| `--nologo` | 시작 배너 숨기기 |
| `-v:q` | 출력 상세 수준: quiet (최소 출력) |
| `-p:VersionSuffix=<suffix>` | 버전 접미사 지정 (예: `dev-20251130-120000`) |

## 테스트 실행

```bash
dotnet test <solution-file> --no-build -c Release \
    --results-directory .TestResults \
    --collect:"XPlat Code Coverage" \
    --logger "trx;LogFileName=test-results.trx" \
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

| 옵션 | 설명 |
|------|------|
| `--no-build` | 테스트 전 빌드 단계 건너뛰기 |
| `-c Release` | Release 구성으로 테스트 |
| `--results-directory` | 테스트 결과 출력 디렉토리 |
| `--collect:"XPlat Code Coverage"` | 코드 커버리지 수집 활성화 |
| `--logger "trx;LogFileName=..."` | TRX 형식으로 테스트 결과 저장 |
| `-- DataCollectionRunSettings...` | 커버리지 출력 형식을 Cobertura로 지정 |

## 도구 설치

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool --version 5.5.0
```

| 옵션 | 설명 |
|------|------|
| `-g` | 전역 도구로 설치 |
| `--version` | 특정 버전 설치 |

### ReportGenerator 실행

```bash
reportgenerator \
    -reports:.TestResults/**/coverage.cobertura.xml \
    -targetdir:.TestResults/coverage/report \
    -reporttypes:"Html;Cobertura;TextSummary" \
    -assemblyfilters:"-*.Tests*"
```

| 옵션 | 설명 |
|------|------|
| `-reports` | 커버리지 파일 경로 (glob 패턴 지원) |
| `-targetdir` | 리포트 출력 디렉토리 |
| `-reporttypes` | 출력 형식 (Html, Cobertura, TextSummary 등) |
| `-assemblyfilters` | 어셈블리 필터 (`-`는 제외, `+`는 포함) |

## 출력 구조

```
.TestResults/
├── {GUID}/
│   └── coverage.cobertura.xml    # 원본 커버리지 데이터
├── test-results.trx              # 테스트 결과 (TRX 형식)
└── coverage/
    └── report/
        ├── index.html            # HTML 리포트
        ├── Cobertura.xml         # 병합된 커버리지
        └── Summary.txt           # 텍스트 요약
```

## 참고

- [dotnet restore](https://learn.microsoft.com/dotnet/core/tools/dotnet-restore)
- [dotnet build](https://learn.microsoft.com/dotnet/core/tools/dotnet-build)
- [dotnet test](https://learn.microsoft.com/dotnet/core/tools/dotnet-test)
- [dotnet list package](https://learn.microsoft.com/dotnet/core/tools/dotnet-list-package)
- [ReportGenerator](https://github.com/danielpalme/ReportGenerator)
