---
title: "개발 환경"
---

소스 생성기 개발에 필요한 환경을 빠르게 참조할 수 있도록 정리한 부록입니다. 본문 학습 후 환경을 재구성할 때 사용하세요. 각 항목의 상세 설명은 [Part 1-01. 개발 환경 설정](../Part1-Fundamentals/01-development-environment.md)을 참고하십시오.

---

## 필수 도구

| 도구 | 최소 버전 | 용도 |
|------|-----------|------|
| .NET SDK | 10.0 | 소스 생성기 빌드 및 테스트 |
| Visual Studio 2022 | 17.12+ | IDE (소스 생성기 디버깅 지원) |
| VS Code + C# Dev Kit | 최신 | 대안 IDE |

```bash
# 설치 확인
dotnet --version
# 출력 예: 10.0.100
```

---

## NuGet 패키지

| 패키지 | 용도 |
|--------|------|
| `Microsoft.CodeAnalysis.CSharp` | Roslyn C# 컴파일러 API (Syntax, Semantic) |
| `Microsoft.CodeAnalysis.Analyzers` | 분석기 개발 규칙 검증 |

두 패키지 모두 `PrivateAssets="all"`을 지정하여 소비자 프로젝트로 전이되지 않도록 합니다.

---

## 프로젝트 설정 체크리스트

| 속성 | 값 | 설명 |
|------|----|------|
| `TargetFramework` | `netstandard2.0` | 모든 IDE/CLI 환경에서 동작하기 위한 필수 타겟 |
| `IsRoslynComponent` | `true` | 소스 생성기 컴포넌트로 인식 |
| `EnforceExtendedAnalyzerRules` | `true` | 분석기 패키징 규칙 적용 |
| `IncludeBuildOutput` | `false` | NuGet 패키지 배포 시 빌드 출력 제외 |

---

## 프로젝트 참조 설정

### 프로덕션 프로젝트 (소스 생성기 사용)

```xml
<ProjectReference Include="..\MySourceGenerator\MySourceGenerator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

| 속성 | 설명 |
|------|------|
| `OutputItemType="Analyzer"` | 분석기/생성기로 인식 |
| `ReferenceOutputAssembly="false"` | 런타임 참조 제외 (컴파일 타임만 사용) |

### 테스트 프로젝트 (소스 생성기 디버깅)

```xml
<ProjectReference Include="..\MySourceGenerator\MySourceGenerator.csproj"
                  ReferenceOutputAssembly="true" />
```

테스트 프로젝트에서는 `ReferenceOutputAssembly="true"`로 설정하여 디버거가 소스 생성기 내부로 진입할 수 있도록 합니다.

---

## 상세 학습

→ [Part 1-01. 개발 환경 설정](../Part1-Fundamentals/01-development-environment.md)
