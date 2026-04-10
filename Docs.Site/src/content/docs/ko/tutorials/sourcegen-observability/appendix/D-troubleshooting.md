---
title: "문제 해결"
---

소스 생성기 개발 중 자주 발생하는 문제와 해결 방법을 정리한 부록입니다. 디버깅 설정에 대한 상세 내용은 [Part 1-03. Debugging 설정](../Part1-Fundamentals/03-Debugging-Setup/)을 참고하십시오.

---

## 빌드/생성 문제

| 증상 | 원인 | 해결 |
|------|------|------|
| 생성된 코드가 보이지 않음 | 빌드 캐시에 이전 결과가 남아 있음 | `bin/obj` 폴더 삭제 후 재빌드 |
| 코드 변경이 반영되지 않음 | IDE가 이전 생성기 DLL을 캐싱 | Visual Studio 완전 종료 → `bin/obj` 삭제 → 재시작 |
| 컴파일 오류: 중복 타입 정의 | 프로젝트 참조에서 `OutputItemType` 누락 | `OutputItemType="Analyzer"` 확인 |
| 생성기가 실행되지 않음 | `[Generator]` 속성 누락 또는 `IIncrementalGenerator` 미구현 | 생성기 클래스에 `[Generator(LanguageNames.CSharp)]` 확인 |

```powershell
# bin/obj 일괄 삭제
Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force
```

---

## 디버깅 문제

| 증상 | 원인 | 해결 |
|------|------|------|
| 브레이크포인트가 작동하지 않음 (빈 원 표시) | 빌드 캐시와 소스 불일치 | 캐시 삭제 → `dotnet clean` → 재빌드 |
| `Debugger.Launch()`가 팝업되지 않음 | Release 빌드 또는 `#if DEBUG` 조건 미충족 | Debug 빌드 구성 확인, `AttachDebugger: true` 설정 확인 |
| 테스트에서 생성기 내부로 진입 불가 | 테스트 프로젝트에서 `ReferenceOutputAssembly="false"` | `ReferenceOutputAssembly="true"`로 변경 |
| `Debugger.Launch()` 후 IDE가 선택되지 않음 | 여러 Visual Studio 인스턴스 실행 중 | 하나만 남기고 다른 인스턴스 종료 |

---

## 테스트 문제

| 증상 | 원인 | 해결 |
|------|------|------|
| Verify 스냅샷 불일치 | 생성 코드가 변경됨 (의도적 또는 비의도적) | 변경 확인 후 `Build-VerifyAccept.ps1` 실행 |
| `CSharpCompilation`에 필요한 어셈블리 누락 | `RequiredTypes` 배열에 참조 타입 부족 | `SourceGeneratorTestRunner`의 `RequiredTypes`에 필요한 타입 추가 |
| 테스트에서 `NullReferenceException` | 생성기가 코드를 생성하지 않음 | 입력 소스에 `[GenerateObservablePort]`와 `IObservablePort` 구현 확인 |
| 진단 테스트 실패 | `GenerateWithDiagnostics` 대신 `Generate` 사용 | 진단 검증 시 `GenerateWithDiagnostics` 메서드 사용 |

```powershell
# 모든 pending 스냅샷 승인
./Build-VerifyAccept.ps1
```

---

## 성능 문제

| 증상 | 원인 | 해결 |
|------|------|------|
| 빌드가 느림 | 증분 캐싱이 동작하지 않음 | 데이터 모델이 `record struct`인지 확인 (값 동등성 필요) |
| IDE 응답 없음 | `Debugger.Launch()`가 `true` 상태로 남아 있음 | `AttachDebugger: false`로 복원 |
| 대규모 프로젝트에서 빌드 지연 | 생성기가 모든 Syntax Node를 탐색 | `ForAttributeWithMetadataName`으로 필터링 범위 축소 |

---

## 디버깅 팁

### 생성된 코드 확인

Visual Studio Solution Explorer에서 생성된 코드를 직접 확인할 수 있습니다:

```
Solution Explorer
→ Dependencies
→ Analyzers
→ Functorium.SourceGenerators
→ Functorium.SourceGenerators.ObservablePortGenerator
   → GenerateObservablePortAttribute.g.cs
   → Repositories.UserRepositoryObservable.g.cs
```

### 유용한 Watch 표현식

디버깅 중 소스 생성기 내부 상태를 파악하는 데 유용한 표현식입니다:

```csharp
// 클래스 전체 이름
classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
// → "global::MyApp.Adapters.UserRepository"

// 모든 인터페이스
classSymbol.AllInterfaces.Select(i => i.Name).ToArray()
// → ["IUserRepository", "IObservablePort"]

// 메서드 시그니처
method.ToDisplayString()
// → "GetUserAsync(int)"

// 반환 타입
method.ReturnType.ToDisplayString()
// → "LanguageExt.FinT<LanguageExt.IO, User>"
```

### 빌드 로그 분석

```bash
# 상세 로그 생성
dotnet build MyProject.csproj -v:diag > build.log

# 소스 생성기 관련 로그 검색
grep -i "sourcegenerator" build.log
```

---

## 상세 학습

→ [Part 1-03. Debugging 설정](../Part1-Fundamentals/03-Debugging-Setup/)
