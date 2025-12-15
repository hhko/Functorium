# Phase 3: 커밋 분석 결과

Generated: 2025-12-16

## Breaking Changes

없음

## Feature Commits (높은 우선순위)

### Functorium
- [cda0a33] feat(functorium): 핵심 라이브러리 패키지 참조 및 소스 구조 추가
- [1790c73] feat(observability): OpenTelemetry 및 Serilog 통합 구성 추가
- [7d9f182] feat(observability): OpenTelemetry 의존성 등록 확장 메서드 추가
- [6538216] feat(lang-ext): LanguageExt 5.0.0-beta-58 업그레이드

### Functorium.Testing
- [0282d23] feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가

## Feature Commits (중간 우선순위)

- [4727bf9] feat(api): PublicApiGenerator로 생성한 Public API 파일 추가
- [a87dbce] feat(release-notes): Spectre.Console로 콘솔 출력 개선

## Bug Fixes

- [a8ec763] fix(build): NuGet 패키지 아이콘 경로 수정

## Refactoring Commits

- [08a1af8] refactor(observability): Builder 코드를 Configurator 패턴으로 재구성
- [4edcf7f] refactor(options): OptionsUtilities를 OptionsConfigurator로 교체
- [8646736] refactor(observability): OpenTelemetry 속성 및 메서드 네이밍 통일
- [c9894fc] refactor(observability): Builders 코드 품질 개선 및 네이밍 통일
- [afd1a42] refactor(errors): Destructurer 필드명 일관성 개선
- [9094097] refactor(testing): ControllerTestFixture를 HostTestFixture로 이름 변경
- [922c7b3] refactor(testing): 로깅 테스트 유틸리티 재구성
- [dfef661] refactor: api 폴더를 .api로 변경

## Build/Chore Commits

- [afb59b3] build(nuget): NuGet 패키지 배포 설정 추가
- [164e495] build: 테스트 프로젝트 구조 추가
- [25bf5a7] build: 솔루션 초기 구성
- [fef699c] build: .NET 10 및 테스트 패키지 업데이트
- [7b46907] chore(api): Public API 파일 타임스탬프 업데이트
- [a13f1f2] chore(api): Public API 파일 타임스탬프 업데이트

## Test Commits

- [b889230] test(abstractions): Errors 타입 단위 테스트 추가
