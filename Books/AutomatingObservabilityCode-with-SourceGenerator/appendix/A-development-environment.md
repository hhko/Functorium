# 부록 A: 개발 환경

## 필수 소프트웨어

### .NET SDK

```bash
# .NET 10 SDK 설치 확인
dotnet --version
# 10.0.xxx

# 설치
# https://dotnet.microsoft.com/download/dotnet/10.0
```

### Visual Studio 2022

```
버전: 17.13 이상
워크로드:
- .NET 데스크톱 개발
- ASP.NET 및 웹 개발

구성 요소:
- .NET Compiler Platform SDK (필수!)
```

### Visual Studio Code (대안)

```
확장:
- C# Dev Kit
- .NET Install Tool
```

---

## NuGet 패키지

### 소스 생성기 프로젝트

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" />
</ItemGroup>
```

### 테스트 프로젝트

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1" />
  <PackageReference Include="Shouldly" Version="4.3.0" />
  <PackageReference Include="Verify.Xunit" Version="28.9.2" />
</ItemGroup>
```

---

## 프로젝트 설정

### 소스 생성기 .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <!-- 필수: netstandard2.0 -->
    <TargetFramework>netstandard2.0</TargetFramework>

    <!-- 필수: Roslyn 구성 요소 표시 -->
    <IsRoslynComponent>true</IsRoslynComponent>

    <!-- 필수: 분석기 규칙 강제 -->
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>

    <!-- 권장 설정 -->
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.12.0" PrivateAssets="all" />
  </ItemGroup>

</Project>
```

### 테스트 .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Shouldly" Version="4.3.0" />
    <PackageReference Include="Verify.Xunit" Version="28.9.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MySourceGenerator\MySourceGenerator.csproj" />
  </ItemGroup>

</Project>
```

---

## 디렉토리 구조

```
MySolution/
├── src/
│   └── MySourceGenerator/
│       ├── MySourceGenerator.csproj
│       ├── MyGenerator.cs
│       └── Generators/
│           └── ...
├── tests/
│   └── MySourceGenerator.Tests/
│       ├── MySourceGenerator.Tests.csproj
│       ├── MyGeneratorTests.cs
│       └── *.verified.txt
└── MySolution.sln
```

---

## Visual Studio 설정

### 소스 생성기 디버깅

1. 소스 생성기 프로젝트 속성
2. 디버그 → 시작 옵션
3. "다른 프로그램 시작": `C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\devenv.exe`
4. 명령줄 인수: `/rootsuffix Exp`

### 생성된 파일 보기

```
솔루션 탐색기 → 프로젝트 → Dependencies → Analyzers
                                         → MySourceGenerator
                                           → 생성된 파일들
```

---

## 환경 변수

### 디버깅용

```bash
# Windows
set ROSLYN_DEBUGGER_LAUNCH=1

# PowerShell
$env:ROSLYN_DEBUGGER_LAUNCH = "1"
```

---

## 트러블슈팅

### 빌드 오류: "Generator not found"

```
1. 솔루션 다시 빌드 (Ctrl+Shift+B)
2. Visual Studio 재시작
3. NuGet 캐시 클리어: dotnet nuget locals all --clear
```

### 생성된 파일이 안 보임

```
1. 솔루션 탐색기에서 "모든 파일 표시" 활성화
2. Dependencies → Analyzers 확인
3. obj/Debug/*/generated 폴더 확인
```

### IntelliSense 오류

```
1. .suo 파일 삭제
2. .vs 폴더 삭제
3. Visual Studio 재시작
```

---

➡️ [부록 B: API 레퍼런스](B-api-reference.md)
