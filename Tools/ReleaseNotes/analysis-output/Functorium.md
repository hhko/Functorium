# Analysis for Src/Functorium

Generated: 2025-12-13 오후 2:03:45
Comparing: 6712decdc446cedfbe4a4355ba787cb2c50e4844 -> HEAD

## Change Summary

```
Src/Functorium/.api/Functorium.cs                  | 231 +++++++++++++++
 .../ErrorTypes/ErrorCodeExceptionalDestructurer.cs |  31 ++
 .../ErrorTypes/ErrorCodeExpectedDestructurer.cs    |  26 ++
 .../ErrorTypes/ErrorCodeExpectedTDestructurer.cs   |  44 +++
 .../ErrorTypes/ExceptionalDestructurer.cs          |  32 +++
 .../ErrorTypes/ExpectedDestructurer.cs             |  31 ++
 .../ErrorTypes/ManyErrorsDestructurer.cs           |  31 ++
 .../ErrorsDestructuringPolicy.cs                   |  53 ++++
 .../DestructuringPolicies/IErrorDestructurer.cs    | 114 ++++++++
 .../Abstractions/Errors/ErrorCodeExceptional.cs    |  80 ++++++
 .../Abstractions/Errors/ErrorCodeExpected.cs       | 298 +++++++++++++++++++
 .../Abstractions/Errors/ErrorCodeFactory.cs        | 176 ++++++++++++
 .../Abstractions/Errors/ErrorCodeFieldNames.cs     |  18 ++
 .../Registrations/OpenTelemetryRegistration.cs     |  62 ++++
 .../Abstractions/Utilities/DictionaryUtilities.cs  |  10 +
 .../Abstractions/Utilities/IEnumerableUtilities.cs |  31 ++
 .../Abstractions/Utilities/StringUtilities.cs      |  28 ++
 .../Builders/Configurators/LoggingConfigurator.cs  |  95 +++++++
 .../Builders/Configurators/MetricsConfigurator.cs  |  83 ++++++
 .../Builders/Configurators/TracingConfigurator.cs  |  85 ++++++
 .../Builders/OpenTelemetryBuilder.Protocols.cs     |  23 ++
 .../Builders/OpenTelemetryBuilder.Resources.cs     |  17 ++
 .../Builders/OpenTelemetryBuilder.cs               | 315 +++++++++++++++++++++
 .../Observabilities/IOpenTelemetryOptions.cs       |  10 +
 .../Logging/IStartupOptionsLogger.cs               |  12 +
 .../Observabilities/Logging/StartupLogger.cs       | 159 +++++++++++
 .../Observabilities/OpenTelemetryOptions.cs        | 297 +++++++++++++++++++
 .../Adapters/Options/OptionsConfigurator.cs        | 130 +++++++++
 Src/Functorium/Applications/Linq/FinTUtilites.cs   | 144 ++++++++++
 Src/Functorium/Functorium.csproj                   |  40 +++
 Src/Functorium/Using.cs                            |   4 +
 31 files changed, 2710 insertions(+)
```

## All Commits

```
7b46907 chore(api): Public API 파일 타임스탬프 업데이트
a87dbce feat(release-notes): Spectre.Console로 콘솔 출력 개선
dfef661 refactor: api 폴더를 .api로 변경
a13f1f2 chore(api): Public API 파일 타임스탬프 업데이트
4727bf9 feat(api): PublicApiGenerator로 생성한 Public API 파일 추가
a8ec763 fix(build): NuGet 패키지 아이콘 경로 수정
afb59b3 build(nuget): NuGet 패키지 배포 설정 추가
7d9f182 feat(observability): OpenTelemetry 의존성 등록 확장 메서드 추가
4edcf7f refactor(options): OptionsUtilities를 OptionsConfigurator로 교체
08a1af8 refactor(observability): Builder 코드를 Configurator 패턴으로 재구성
8646736 refactor(observability): OpenTelemetry 속성 및 메서드 네이밍 통일
c9894fc refactor(observability): Builders 코드 품질 개선 및 네이밍 통일
1790c73 feat(observability): OpenTelemetry 및 Serilog 통합 구성 추가
afd1a42 refactor(errors): Destructurer 필드명 일관성 개선
b889230 test(abstractions): Errors 타입 단위 테스트 추가
6538216 feat(lang-ext): LanguageExt 5.0.0-beta-58 업그레이드
cda0a33 feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가
164e495 build: 테스트 프로젝트 구조 추가
25bf5a7 build: 솔루션 초기 구성
```

## Top Contributors

- 19 hhko

## Categorized Commits

### Feature Commits

- a87dbce feat(release-notes): Spectre.Console로 콘솔 출력 개선
- 4727bf9 feat(api): PublicApiGenerator로 생성한 Public API 파일 추가
- 7d9f182 feat(observability): OpenTelemetry 의존성 등록 확장 메서드 추가
- 1790c73 feat(observability): OpenTelemetry 및 Serilog 통합 구성 추가
- 6538216 feat(lang-ext): LanguageExt 5.0.0-beta-58 업그레이드
- cda0a33 feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가

### Bug Fixes

- a8ec763 fix(build): NuGet 패키지 아이콘 경로 수정

### Breaking Changes

None found
