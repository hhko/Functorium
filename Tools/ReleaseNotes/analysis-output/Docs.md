# Analysis for Docs

Generated: 2025-12-13 오후 2:03:51
Comparing: 6712decdc446cedfbe4a4355ba787cb2c50e4844 -> HEAD

## Change Summary

```
Docs/ArchitectureIs/ApplicationArchitecture.png    |  Bin 0 -> 166979 bytes
 Docs/ArchitectureIs/ArchitectureIs.png             |  Bin 0 -> 261235 bytes
 .../External_n_InternalArchitecture.png            |  Bin 0 -> 115205 bytes
 Docs/ArchitectureIs/InternalArchitecture.png       |  Bin 0 -> 221232 bytes
 .../InternalArchitecture_Observability.png         |  Bin 0 -> 205586 bytes
 .../InternalArchitecture_Testing.png               |  Bin 0 -> 120083 bytes
 Docs/ArchitectureIs/Layers.png                     |  Bin 0 -> 223201 bytes
 .../ArchitectureIs/Layers_Dependency_Direction.png |  Bin 0 -> 339793 bytes
 Docs/ArchitectureIs/Layers_RequestNResponse.png    |  Bin 0 -> 309750 bytes
 Docs/ArchitectureIs/README.md                      |  150 ++
 Docs/ArchitectureIs/README.pptx                    |  Bin 0 -> 1178260 bytes
 Docs/ArchitectureIs/SourceCodeDependencies.png     |  Bin 0 -> 208395 bytes
 Docs/ArchitectureIs/SourceCodeDependencies_Mix.png |  Bin 0 -> 446459 bytes
 ...SourceCodeDependencies_Vs_CleanArchitecture.png |  Bin 0 -> 261796 bytes
 ...ceCodeDependencies_Vs_HexagonalArchitecture.png |  Bin 0 -> 101458 bytes
 Docs/Functorium/Guide-01-Unit-Testing.md           |  458 +++++++
 Docs/Functorium/Guide-02-Integration-Testing.md    |  671 +++++++++
 Docs/Functorium/Infra-01-VSCode.md                 |  857 ++++++++++++
 Docs/Functorium/Infra-02-VSCode-Scripts.md         |  557 ++++++++
 Docs/Functorium/Infra-03-Git-Hooks.md              |  527 ++++++++
 Docs/Functorium/LogEventPropertyExtractor.md       |  364 +++++
 Docs/Functorium/Src-01-Error.md                    |  598 ++++++++
 Docs/Functorium/Src-02-Options.md                  |  711 ++++++++++
 Docs/Functorium/Src-03-OpenTelemetry.md            |  448 ++++++
 Docs/Guides/CI-GitHub-Actions.md                   |  807 +++++++++++
 Docs/Guides/CI-MinVer.md                           | 1429 ++++++++++++++++++++
 Docs/Guides/CI-NuGet-Package.md                    |  753 +++++++++++
 Docs/Guides/Code-Quality.md                        |  775 +++++++++++
 Docs/Guides/DotNet-CLI.md                          |    8 +
 Docs/Guides/Git.md                                 |  750 ++++++++++
 Docs/Guides/SdkVersion.md                          |  718 ++++++++++
 Docs/Guides/xUnit.md                               |  884 ++++++++++++
 Docs/Manuals/Build-CommitSummary.md                |  529 ++++++++
 Docs/Manuals/Build-Local.md                        |  483 +++++++
 Docs/Manuals/Command-Commit.md                     |  658 +++++++++
 Docs/Manuals/Command-SuggestNextVersion.md         |  406 ++++++
 Docs/Writing-Guide.md                              |  894 ++++++++++++
 Docs/Writing-ps1.md                                |  790 +++++++++++
 38 files changed, 15225 insertions(+)
```

## All Commits

```
016c2f3 build: MinVer 패키지 기능 제거
480cc75 docs(minver): Height의 실질적 활용 섹션 추가
f18b0ff docs(guides): Git 가이드에 커밋 합치기(squash) 방법 추가
ef01405 docs(functorium): 가이드 문서 위치 재정리 및 내용 업데이트
20bbb5b docs(functorium): 문서 파일 네이밍 체계 정리
d2cf445 docs(guides): VSCode 프로젝트 설정 스크립트 가이드 추가
d2296c6 docs(guides): Git Hooks 가이드 문서 추가
f2e8ee1 docs(functorium): 문서 구조 재정리 및 VSCode 가이드 추가
8d50e7f docs(guides): CI 관련 가이드 문서 파일명 통일
3a4447d docs(nuget): NuGet 패키지 생성 가이드 문서 추가
2b62bf5 docs(functorium): 문서 파일 리팩터링
fd1cd13 docs(functorium): OpenTelemetry Options 설정 가이드 문서 추가
3ba03e4 docs(functorium): Options 설정 가이드 문서 추가
6708202 docs: PowerShell 스크립트 작성 가이드 추가
3ae89b9 docs: ErrorTypes 및 테스트 가이드 문서 추가
b37a990 build(script): Build-Local.ps1 출력 개선
1cef75e docs: 매뉴얼 문서 구조 정리
dbd8098 docs: Build-Local.ps1 매뉴얼 문서 추가
428d3ec docs: MinVer FAQ에 Hotfix 릴리스 관리 방법 추가
db4962e docs: MinVer FAQ에 AssemblyVersion/FileVersion 차이 설명 추가
22359bd docs: MinVer 가이드 버전 구조 용어 정의 추가
3b2cf06 docs: MinVer 가이드 pre-release 단계 정리
b8cba28 docs: DotNet CLI 빌드 명령어 가이드 추가
feb3d46 docs: Code-Quality 문서에 코드 스타일 규칙 vs 진단 규칙 설명 추가
1c5432c docs: 커밋 요약 및 Claude 명령어 매뉴얼 추가
b81c247 ci: release 워크플로우를 publish로 변경
848ab9a docs: 가이드 문서 통합 및 재구성
6752f8d docs: 커밋 요약 스크립트 가이드 추가
49f411f docs: GitHub Actions 가이드 업데이트
31144dd docs: Git 명령어 가이드 업데이트
9bfa4e8 docs: 빌드 및 CI 관련 가이드 문서 추가
76f8efb docs: Git 명령어 가이드에 태그 섹션 추가
ac9d742 docs: Git 명령어 가이드 업데이트
b157c39 docs: 가이드 문서 추가
8bb591e docs: 아키텍처 문서 추가
dcd5c47 docs: Git 명령어 가이드 추가
```

## Top Contributors

- 36 hhko

## Categorized Commits

### Feature Commits

None found

### Bug Fixes

- 428d3ec docs: MinVer FAQ에 Hotfix 릴리스 관리 방법 추가

### Breaking Changes

None found
