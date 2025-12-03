- [x] claude commit 명령어
- [x] 아키텍처 문서
- [x] git command guide 문서: `Git-Commands.md`
- [x] git commit guide 문서: `Git-Commit.md`
- [x] guide 문서 작성을 위한 guide 문서: `Guide-Writing.md`
- [x] 솔루션 구성
  ```
  dotnet new sln -n Fuctorium
  dotnet sln migrate

  dotnet new editorconfig
  dotnet new nuget.config
  dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
  dotnet new buildprops
  dotnet new packagesprops
  dotnet new gitignore

  dotnet new classlib -o .\Src\Functorium
  dotnet sln add (ls -r **\*.csproj)
  ```
  - slnx
  - global.json
  - nuget.config
  - Directory.Build.props
  - Directory.Packages.props
  - .editorconfig
- [x] 코드 품질((코드 스타일, 코드 분석 규칙)) 빌드 통합: .editorconfig을 이용한 "코드 품질" 컴파일 과정 통합
  - .editorconfig: csharp_style_namespace_declarations, dotnet_diagnostic.IDE0161.severity
  - Directory.Build.props: EnforceCodeStyleInBuild
- [x] 코드 품질((코드 스타일, 코드 분석 규칙)) 빌드 통합 문서: `Code-Quality.md`
- [x] DOTNET SDK 빌드 명시 문서: `Build-SdkVersion-GlobalJson.md
- [x] 솔루션 구성: global.json SDK 버전 허용 범위 지정
- [X] 솔루션 구성: nuget.config 파일 생성
- [x] 커밋 이력 ps1
- [ ] powershell 학습 문서
- [ ] powershell 가이드 문서
- [ ] powershell 가이드 문서 기준 개선
- [ ] 로컬 빌드
- [ ] 로컬 빌드 문서(dotnet 명령어)
- [ ] 버전 체계 문서
- [ ] 솔루션 구성: Directory.Build.props 버전 체계
- [ ] 솔루션 구성: Directory.Build.props 형상관리와 버전 연동
- [ ] 솔루션 구성: .editorconfig 폴더 단위 개별 지정
- [ ] 솔루션 구성: Directory.Packages.props 하위 폴더 새로 시작, 버전 재정의
- [ ] GitHub actions build
- [ ] GitHub actions build 문서
- [ ] GitHub actions publish
- [ ] GitHub actions publish 문서

