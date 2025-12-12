# NuGet 패키지 생성 가이드

이 문서는 Functorium 프로젝트의 NuGet 패키지 생성 및 배포 설정을 설명합니다.

## 목차
- [개요](#개요)
- [요약](#요약)
- [프로젝트 구조](#프로젝트-구조)
- [Directory.Build.props 설정](#directorybuildprops-설정)
- [프로젝트별 설정](#프로젝트별-설정)
- [패키지 생성](#패키지-생성)
- [패키지 검증](#패키지-검증)
- [트러블슈팅](#트러블슈팅)
- [FAQ](#faq)

<br/>

## 개요

### NuGet 패키지란?

NuGet은 .NET의 공식 패키지 관리자입니다. 프로젝트를 NuGet 패키지로 배포하면 다른 개발자가 쉽게 사용할 수 있습니다.

### 주요 특징

- **Git 태그 기반 버전 관리**: Git 태그를 사용한 버전 관리
- **SourceLink 지원**: 디버깅 시 원본 소스 코드 접근 가능
- **심볼 패키지**: .snupkg를 통한 디버깅 지원
- **중앙 집중 설정**: Directory.Build.props로 공통 설정 관리

### 생성되는 패키지

| 패키지 | 설명 |
|--------|------|
| `Functorium` | .NET용 함수형 도메인 프레임워크 |
| `Functorium.Testing` | Functorium 테스트 유틸리티 |

<br/>

## 요약

### 주요 명령

**패키지 생성:**
```bash
# 빌드 및 패키지 생성 (Build-Local.ps1 사용)
./Build-Local.ps1

# 패키지만 건너뛰기
./Build-Local.ps1 -SkipPack

# dotnet CLI 직접 사용
dotnet pack -c Release -o .nupkg
```

**패키지 확인:**
```bash
# 생성된 패키지 목록
ls .nupkg/

# 패키지 내용 확인
dotnet nuget inspect .nupkg/Functorium.1.0.0.nupkg
```

**로컬 테스트:**
```bash
# 로컬 NuGet 소스 추가
dotnet nuget add source .nupkg/ --name local

# 패키지 설치 테스트
dotnet add package Functorium --source local
```

### 주요 절차

**1. 새 패키지 프로젝트 추가:**
```bash
# 1. 프로젝트 생성
dotnet new classlib -n Functorium.NewPackage -o Src/Functorium.NewPackage

# 2. csproj에 패키지 설정 추가
# - PackageId, Description, PackageTags
# - Package Files (README.md, icon.png)

# 3. 빌드 및 패키지 생성 테스트
./Build-Local.ps1
```

**2. 릴리스:**
```bash
# 1. 변경사항 커밋
git add .
git commit -m "feat: new package release"

# 2. 버전 태그 생성
git tag -a v1.0.0 -m "Release 1.0.0"

# 3. 빌드 및 패키지 생성
./Build-Local.ps1

# 4. 태그 푸시
git push origin v1.0.0

# 5. NuGet.org에 배포 (CI/CD 또는 수동)
dotnet nuget push .nupkg/*.nupkg --api-key <API_KEY> --source https://api.nuget.org/v3/index.json
```

### 주요 개념

**1. Directory.Build.props**

모든 프로젝트에 적용되는 공통 NuGet 설정:
- 작성자, 회사, 저작권
- 라이선스, 프로젝트 URL, 저장소 정보
- 심볼 패키지, SourceLink 설정

**2. 프로젝트별 csproj**

각 프로젝트 고유의 설정:
- PackageId (패키지 이름)
- Description (설명)
- PackageTags (추가 태그)
- Package Files (README.md, icon.png)

**3. 버전 관리**

Git 태그를 통한 버전 관리:
- Git 태그 기반 (`v1.0.0`)
- Pre-release 지원 (`1.0.0-alpha.0`)

<br/>

## 프로젝트 구조

### 파일 구조

```
Functorium/
├── Directory.Build.props          # 공통 NuGet 설정
├── Directory.Packages.props       # 중앙 패키지 버전 관리
├── Assets/
│   └── icon.png                   # 패키지 아이콘 (128x128 PNG)
├── README.md                      # 패키지 README
├── .nupkg/                        # 생성된 패키지 출력 디렉토리
│   ├── Functorium.1.0.0.nupkg
│   ├── Functorium.1.0.0.snupkg
│   ├── Functorium.Testing.1.0.0.nupkg
│   └── Functorium.Testing.1.0.0.snupkg
└── Src/
    ├── Functorium/
    │   └── Functorium.csproj      # 메인 패키지
    └── Functorium.Testing/
        └── Functorium.Testing.csproj  # 테스팅 패키지
```

### 출력 파일

| 파일 | 설명 |
|------|------|
| `*.nupkg` | NuGet 패키지 (배포용) |
| `*.snupkg` | 심볼 패키지 (디버깅용) |

<br/>

## Directory.Build.props 설정

### 공통 NuGet 설정

모든 프로젝트에 적용되는 메타데이터 설정입니다.

```xml
<!-- NuGet Package Common Settings -->
<PropertyGroup>
  <Authors>고형호</Authors>
  <Company>Functorium</Company>
  <Copyright>Copyright (c) Functorium Contributors. All rights reserved.</Copyright>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/hhko/Functorium</PackageProjectUrl>
  <RepositoryUrl>https://github.com/hhko/Functorium.git</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageIcon>icon.png</PackageIcon>
  <PackageTags>functorium;functional;dotnet;csharp</PackageTags>
</PropertyGroup>
```

**설정 설명:**

| 속성 | 설명 | 값 |
|------|------|-----|
| `Authors` | 패키지 작성자 | 이름 또는 닉네임 |
| `Company` | 회사/조직명 | 프로젝트 이름 |
| `Copyright` | 저작권 표시 | Copyright 문구 |
| `PackageLicenseExpression` | SPDX 라이선스 식별자 | MIT, Apache-2.0 등 |
| `PackageProjectUrl` | 프로젝트 홈페이지 | GitHub URL |
| `RepositoryUrl` | Git 저장소 URL | .git URL |
| `RepositoryType` | 저장소 타입 | git |
| `PackageReadmeFile` | README 파일 | README.md |
| `PackageIcon` | 아이콘 파일 | icon.png |
| `PackageTags` | 검색 태그 | 세미콜론 구분 |

### 심볼 패키지 설정

디버깅 지원을 위한 심볼 패키지 생성 설정입니다.

```xml
<!-- Symbol Package for Debugging -->
<PropertyGroup>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

**심볼 패키지 (.snupkg):**
- PDB 파일 포함
- NuGet.org 심볼 서버에 자동 업로드
- Visual Studio에서 Step-into 디버깅 가능

### SourceLink 설정

GitHub 소스와 연결하여 디버깅 시 원본 코드 접근을 지원합니다.

```xml
<!-- Source Link for Debugging -->
<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
</PropertyGroup>

<!-- Source Link Package -->
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
</ItemGroup>
```

**SourceLink 기능:**
- 디버깅 중 F11 (Step Into)로 라이브러리 소스 진입
- GitHub에서 정확한 커밋의 소스 코드 자동 다운로드
- 소스 코드를 패키지에 포함하지 않아 용량 절약

### Deterministic Build 설정

CI 환경에서 재현 가능한 빌드를 위한 설정입니다.

```xml
<!-- Deterministic Build -->
<PropertyGroup>
  <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

**Deterministic Build:**
- 동일 소스 → 동일 바이너리 생성
- 빌드 타임스탬프/경로 정보 제거
- CI/CD에서만 활성화 (로컬 디버깅 편의)

<br/>

## 프로젝트별 설정

### Functorium.csproj

메인 패키지의 고유 설정입니다.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- NuGet Package Settings -->
  <PropertyGroup>
    <PackageId>Functorium</PackageId>
    <Description>Functorium - A functional domain framework for .NET</Description>
    <PackageTags>$(PackageTags);functional-programming;domain-driven-design;ddd</PackageTags>
  </PropertyGroup>

  <!-- Package Files -->
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Assets\icon.png" Pack="true" PackagePath="\" />
  </ItemGroup>

</Project>
```

### Functorium.Testing.csproj

테스팅 패키지의 고유 설정입니다.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- NuGet Package Settings -->
  <PropertyGroup>
    <PackageId>Functorium.Testing</PackageId>
    <Description>Functorium.Testing - Testing utilities for a functional domain framework for .NET</Description>
    <PackageTags>$(PackageTags);testing;functional-programming;domain-driven-design;ddd</PackageTags>
  </PropertyGroup>

  <!-- Package Files -->
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Assets\icon.png" Pack="true" PackagePath="\" />
  </ItemGroup>

  <!-- Not a test project (produces NuGet package) -->
  <PropertyGroup>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>

</Project>
```

**IsTestProject 설정:**
- `false`: NuGet 패키지로 배포 가능
- 기본값 `true`인 경우 패키지 생성 불가

### 새 패키지 프로젝트 추가

새 패키지 프로젝트를 추가할 때 필요한 설정입니다.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- NuGet Package Settings -->
  <PropertyGroup>
    <PackageId>Functorium.NewPackage</PackageId>
    <Description>Functorium.NewPackage - Description here</Description>
    <PackageTags>$(PackageTags);additional;tags</PackageTags>
  </PropertyGroup>

  <!-- Package Files -->
  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Assets\icon.png" Pack="true" PackagePath="\" />
  </ItemGroup>

</Project>
```

**필수 설정:**
1. `PackageId`: 고유한 패키지 이름
2. `Description`: 패키지 설명
3. `PackageTags`: 공통 태그 + 추가 태그
4. Package Files: README.md, icon.png 포함

<br/>

## 패키지 생성

### Build-Local.ps1 사용 (권장)

빌드, 테스트, 패키지 생성을 한 번에 수행합니다.

```bash
# 전체 빌드 및 패키지 생성
./Build-Local.ps1

# 패키지 생성 건너뛰기
./Build-Local.ps1 -SkipPack
```

**출력:**
```
[8/8] Creating NuGet packages...

Project                                  Status          Package
------------------------------------------------------------------------------------------
Functorium                               Packed          Functorium.1.0.0-alpha.0.82.nupkg
Functorium.Testing                       Packed          Functorium.Testing.1.0.0-alpha.0.82.nupkg

      Created 2 package(s), 2 symbol package(s)
      Output: C:\Workspace\Github\Functorium\.nupkg

[DONE] Build, test, and pack completed
       Duration: 01:15
       Coverage: C:\Workspace\Github\Functorium\.coverage/index.html
       Packages: C:\Workspace\Github\Functorium\.nupkg
```

### dotnet CLI 직접 사용

개별 프로젝트 패키지 생성:

```bash
# 솔루션 전체 패키지 생성
dotnet pack -c Release -o .nupkg

# 특정 프로젝트만
dotnet pack Src/Functorium/Functorium.csproj -c Release -o .nupkg

# 버전 지정
dotnet pack -c Release -o .nupkg -p:Version=1.0.0
```

### 버전별 패키지

Git 태그에 따라 다양한 형태의 패키지가 생성됩니다.

**Pre-release (개발 중):**
```
Functorium.1.0.0-alpha.0.nupkg
Functorium.1.0.0-alpha.0.snupkg
```

**Stable (릴리스):**
```
Functorium.1.0.0.nupkg
Functorium.1.0.0.snupkg
```

<br/>

## 패키지 검증

### 패키지 내용 확인

```bash
# NuGet 패키지 탐색기 (GUI)
# https://github.com/NuGetPackageExplorer/NuGetPackageExplorer

# CLI로 확인
unzip -l .nupkg/Functorium.1.0.0.nupkg

# dotnet CLI (미리보기)
dotnet nuget verify .nupkg/Functorium.1.0.0.nupkg
```

### 예상 패키지 구조

```
Functorium.1.0.0.nupkg
├── Functorium.nuspec
├── README.md
├── icon.png
├── lib/
│   └── net10.0/
│       ├── Functorium.dll
│       └── Functorium.xml (문서)
├── [Content_Types].xml
└── _rels/
    └── .rels
```

### 로컬 테스트

```bash
# 1. 테스트 프로젝트 생성
dotnet new console -n PackageTest
cd PackageTest

# 2. 로컬 소스 추가
dotnet nuget add source ../Functorium/.nupkg/ --name local

# 3. 패키지 설치
dotnet add package Functorium --version 1.0.0-alpha.0.82 --source local

# 4. 사용 테스트
# Program.cs에서 using Functorium; 추가 후 빌드
dotnet build
```

<br/>

## 트러블슈팅

### 패키지 생성 실패

**원인 1**: 빌드 오류
```bash
# 해결: 먼저 빌드 오류 수정
dotnet build -c Release
```

**원인 2**: 버전 형식 오류
```bash
# 해결: 캐시 정리
dotnet clean
dotnet build
```

### README.md가 패키지에 포함되지 않음

**원인**: csproj에 Pack 설정 누락

**해결:**
```xml
<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

### 아이콘이 표시되지 않음

**원인 1**: 아이콘 파일 누락

**해결:**
```bash
# Assets 폴더에 icon.png 확인
ls Assets/icon.png
```

**원인 2**: 아이콘 크기/형식 문제

**해결:**
- 크기: 128x128 픽셀 권장 (최대 256x256)
- 형식: PNG
- 파일 크기: 1MB 이하

### SourceLink가 작동하지 않음

**원인 1**: Microsoft.SourceLink.GitHub 패키지 누락

**해결:**
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
</ItemGroup>
```

**원인 2**: Git 저장소가 아님

**해결:**
```bash
git init
git remote add origin https://github.com/hhko/Functorium.git
```

### 패키지 버전이 0.0.0으로 표시됨

**원인**: 버전 설정 오류

**해결:**
```bash
# 프로젝트의 Version 속성 확인
dotnet build -v detailed | grep Version
```

<br/>

## FAQ

### Q1. PackageId는 어떻게 정하나요?

**A:** 패키지 이름 규칙:
- 고유하고 명확한 이름
- 네임스페이스와 일치 권장
- 소문자와 점(.) 사용

```xml
<!-- 좋은 예시 -->
<PackageId>Functorium</PackageId>
<PackageId>Functorium.Testing</PackageId>
<PackageId>Functorium.Extensions.Http</PackageId>

<!-- 나쁜 예시 -->
<PackageId>my-package</PackageId>        <!-- 하이픈 지양 -->
<PackageId>MyAwesomeLib123</PackageId>   <!-- 불명확 -->
```

### Q2. 패키지 라이선스는 어떻게 설정하나요?

**A:** SPDX 라이선스 식별자 사용:

```xml
<!-- SPDX 식별자 사용 (권장) -->
<PackageLicenseExpression>MIT</PackageLicenseExpression>

<!-- 또는 라이선스 파일 포함 -->
<PackageLicenseFile>LICENSE</PackageLicenseFile>
```

**주요 라이선스:**
| SPDX 식별자 | 라이선스 |
|-------------|----------|
| `MIT` | MIT License |
| `Apache-2.0` | Apache License 2.0 |
| `GPL-3.0-only` | GNU GPL v3 |
| `BSD-3-Clause` | BSD 3-Clause |

### Q3. Pre-release 패키지와 Stable 패키지의 차이는?

**A:** 버전 형식으로 구분:

| 타입 | 버전 형식 | 용도 |
|------|-----------|------|
| Pre-release | `1.0.0-alpha.0.5` | 개발 중, 테스트용 |
| Stable | `1.0.0` | 프로덕션 배포용 |

```bash
# Pre-release 설치
dotnet add package Functorium --prerelease

# Stable만 설치 (기본값)
dotnet add package Functorium
```

### Q4. 심볼 패키지(.snupkg)란 무엇인가요?

**A:** 디버깅용 심볼(PDB) 파일을 담은 패키지입니다.

**장점:**
- Visual Studio에서 Step-into 디버깅 가능
- 스택 트레이스에 라인 번호 표시
- NuGet.org 심볼 서버 자동 연동

```xml
<PropertyGroup>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

### Q5. SourceLink는 무엇이고 왜 필요한가요?

**A:** 디버깅 시 GitHub에서 원본 소스 코드를 자동으로 가져오는 기능입니다.

**없을 때:**
- 디버깅 중 라이브러리 코드 진입 불가
- "소스를 찾을 수 없습니다" 메시지

**있을 때:**
- F11로 라이브러리 소스 진입 가능
- 정확한 커밋의 소스 코드 표시

### Q6. 심볼 패키지(.snupkg)와 SourceLink 둘 다 필요한 이유는?

**A:** 두 기능은 디버깅의 서로 다른 측면을 담당합니다.

| 기능 | 역할 | 제공 정보 |
|------|------|----------|
| **심볼 패키지 (.snupkg)** | "어디서" 실행 중인지 | 메서드 이름, 라인 번호, 변수 이름 |
| **SourceLink** | "무엇을" 실행 중인지 | 실제 소스 코드 내용 |

**심볼 패키지만 있을 때:**
```
// 디버거가 보여주는 정보
Functorium.dll!Functorium.Error.Create(string message) Line 42
// → 라인 번호는 알지만, 실제 코드는 볼 수 없음
// → "소스를 찾을 수 없습니다" 메시지
```

**SourceLink만 있을 때:**
```
// 심볼 정보가 없어서 디버거가 진입 자체를 못함
// → Step Into (F11) 불가능
// → 스택 트레이스에 라인 번호 없음
```

**둘 다 있을 때:**
```csharp
// 디버거가 GitHub에서 정확한 커밋의 소스를 가져와 표시
public static Error Create(string message)  // ← Line 42에서 중단
{
    return new Error(message);  // ← 실제 코드 확인 가능
}
```

**비유:**
- 심볼 패키지: 책의 목차와 페이지 번호 (어디에 있는지)
- SourceLink: 책의 실제 내용 (무엇이 적혀있는지)

**설정:**
```xml
<!-- 심볼 패키지 생성 -->
<IncludeSymbols>true</IncludeSymbols>
<SymbolPackageFormat>snupkg</SymbolPackageFormat>

<!-- SourceLink 활성화 -->
<PublishRepositoryUrl>true</PublishRepositoryUrl>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
<PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
```

### Q7. 패키지에 README.md를 포함하면 무엇이 좋나요?

**A:** NuGet.org에서 패키지 페이지에 README가 표시됩니다.

```xml
<PropertyGroup>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>

<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

**표시 위치:**
- nuget.org 패키지 페이지
- Visual Studio NuGet 패키지 관리자

### Q8. .nupkg 폴더가 Git에 커밋되나요?

**A:** 아니요, .gitignore에서 제외됩니다.

```gitignore
# .gitignore
.nupkg/
```

패키지는 빌드 산출물이므로 저장소에 포함하지 않습니다.

### Q9. NuGet.org에 배포하려면 어떻게 하나요?

**A:** API 키를 사용하여 배포합니다.

```bash
# 1. nuget.org에서 API 키 생성
# https://www.nuget.org/account/apikeys

# 2. 패키지 푸시
dotnet nuget push .nupkg/Functorium.1.0.0.nupkg \
  --api-key <API_KEY> \
  --source https://api.nuget.org/v3/index.json

# 3. 심볼 패키지 자동 업로드됨 (.snupkg)
```

### Q10. 테스트 프로젝트도 패키지로 배포할 수 있나요?

**A:** `IsTestProject`를 `false`로 설정해야 합니다.

```xml
<!-- 테스트 유틸리티 패키지 (배포 가능) -->
<PropertyGroup>
  <IsTestProject>false</IsTestProject>
</PropertyGroup>

<!-- 실제 테스트 프로젝트 (배포 불가) -->
<!-- IsTestProject 기본값 true -->
```

### Q11. Directory.Build.props와 csproj 설정이 충돌하면?

**A:** csproj 설정이 우선합니다.

```xml
<!-- Directory.Build.props -->
<PackageTags>functorium;functional</PackageTags>

<!-- Functorium.csproj -->
<PackageTags>$(PackageTags);ddd</PackageTags>
<!-- 결과: functorium;functional;ddd -->

<!-- 또는 완전히 재정의 -->
<PackageTags>custom;tags</PackageTags>
<!-- 결과: custom;tags -->
```

## 참고 문서

- [Build-Local.ps1 도움말](../../Build-Local.ps1) - `./Build-Local.ps1 -Help`
- [NuGet 공식 문서](https://docs.microsoft.com/nuget/)
- [SourceLink 문서](https://github.com/dotnet/sourcelink)
