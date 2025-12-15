# Analysis for Src/Functorium.Testing

Generated: 2025-12-13 오후 2:03:48
Comparing: 6712decdc446cedfbe4a4355ba787cb2c50e4844 -> HEAD

## Change Summary

```
Src/Functorium.Testing/.api/Functorium.Testing.cs  | 145 ++++++
 .../ArchitectureValidationEntryPoint.cs            |  60 +++
 .../ArchitectureRules/ClassValidator.cs            | 555 +++++++++++++++++++++
 .../ArchitectureRules/MethodValidator.cs           | 100 ++++
 .../ArchitectureRules/ValidationResult.cs          |  44 ++
 .../ArchitectureRules/ValidationResultSummary.cs   |  89 ++++
 .../Arrangements/Hosting/HostTestFixture.cs        |  83 +++
 .../Arrangements/Logging/StructuredTestLogger.cs   |  98 ++++
 .../Arrangements/Logging/TestSink.cs               |  20 +
 .../ScheduledJobs/JobCompletionListener.cs         |  77 +++
 .../ScheduledJobs/JobExecutionResult.cs            |  16 +
 .../ScheduledJobs/QuartzTestFixture.cs             | 150 ++++++
 Src/Functorium.Testing/AssemblyReference.cs        |   8 +
 .../Logging/LogEventPropertyExtractor.cs           |  79 +++
 .../Logging/LogEventPropertyValueConverter.cs      |  23 +
 .../Logging/SerilogTestPropertyValueFactory.cs     | 101 ++++
 Src/Functorium.Testing/Functorium.Testing.csproj   |  29 ++
 Src/Functorium.Testing/Using.cs                    |   2 +
 18 files changed, 1679 insertions(+)
```

## All Commits

```
7b46907 chore(api): Public API 파일 타임스탬프 업데이트
a87dbce feat(release-notes): Spectre.Console로 콘솔 출력 개선
dfef661 refactor: api 폴더를 .api로 변경
a13f1f2 chore(api): Public API 파일 타임스탬프 업데이트
4727bf9 feat(api): PublicApiGenerator로 생성한 Public API 파일 추가
a8ec763 fix(build): NuGet 패키지 아이콘 경로 수정
9094097 refactor(testing): ControllerTestFixture를 HostTestFixture로 이름 변경
afb59b3 build(nuget): NuGet 패키지 배포 설정 추가
922c7b3 refactor(testing): 로깅 테스트 유틸리티 재구성
2b62bf5 docs(functorium): 문서 파일 리팩터링
fef699c build: .NET 10 및 테스트 패키지 업데이트
0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가
164e495 build: 테스트 프로젝트 구조 추가
```

## Top Contributors

- 13 hhko

## Categorized Commits

### Feature Commits

- a87dbce feat(release-notes): Spectre.Console로 콘솔 출력 개선
- 4727bf9 feat(api): PublicApiGenerator로 생성한 Public API 파일 추가
- 0282d23 feat(testing): 테스트 헬퍼 라이브러리 소스 구조 추가

### Bug Fixes

- a8ec763 fix(build): NuGet 패키지 아이콘 경로 수정

### Breaking Changes

None found
