---
title: "API 레퍼런스"
---

ObservablePortGenerator 관련 핵심 타입과 클래스의 빠른 참조입니다. 각 타입의 상세 설명은 본문의 해당 Part를 참고하십시오.

---

## IncrementalGeneratorBase&lt;TValue&gt;

증분 소스 생성기의 템플릿 메서드 패턴 기반 추상 클래스입니다.

**네임스페이스:** `Functorium.SourceGenerators.Generators`

| 생성자 파라미터 | 타입 | 설명 |
|----------------|------|------|
| `registerSourceProvider` | `Func<IncrementalGeneratorInitializationContext, IncrementalValuesProvider<TValue>>` | 소스 제공자 등록 (1단계) |
| `generate` | `Action<SourceProductionContext, ImmutableArray<TValue>>` | 코드 생성 (2단계) |
| `AttachDebugger` | `bool` (기본값: `false`) | DEBUG 빌드에서 디버거 연결 여부 |

---

## ObservableClassInfo

소스 생성기가 추출한 대상 클래스 정보를 담는 record struct입니다.

**네임스페이스:** `Functorium.SourceGenerators.Generators.ObservablePortGenerator`

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Namespace` | `string` | 클래스의 네임스페이스 |
| `ClassName` | `string` | 클래스명 |
| `Methods` | `List<MethodInfo>` | 메서드 목록 |
| `BaseConstructorParameters` | `List<ParameterInfo>` | 기반 클래스 생성자 파라미터 |
| `Location` | `Location?` | 소스 코드 위치 (진단용) |

정적 필드 `ObservableClassInfo.None`은 빈 인스턴스를 나타냅니다.

---

## MethodInfo

메서드 시그니처 정보를 담는 클래스입니다.

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Name` | `string` | 메서드명 |
| `Parameters` | `List<ParameterInfo>` | 파라미터 목록 |
| `ReturnType` | `string` | 반환 타입 문자열 |

---

## ParameterInfo

파라미터 정보를 담는 클래스입니다.

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Name` | `string` | 파라미터명 |
| `Type` | `string` | 타입 문자열 |
| `RefKind` | `RefKind` | 참조 종류 (in, out, ref 등) |
| `IsCollection` | `bool` | 컬렉션 타입 여부 |

---

## TypeExtractor

`FinT<IO, T>`에서 두 번째 타입 파라미터를 추출하는 유틸리티입니다.

| 메서드 | 설명 |
|--------|------|
| `ExtractSecondTypeParameter(string returnType)` | 제네릭 타입의 두 번째 파라미터 추출 |

**입출력 예시:**

| 입력 | 출력 |
|------|------|
| `FinT<IO, User>` | `User` |
| `FinT<IO, List<User>>` | `List<User>` |
| `FinT<IO, Dictionary<string, int>>` | `Dictionary<string, int>` |
| `FinT<IO, (int Id, string Name)>` | `(int Id, string Name)` |
| `FinT<IO, string[]>` | `string[]` |

---

## CollectionTypeHelper

컬렉션 타입 판별 및 Count/Length 표현식 생성 유틸리티입니다.

| 메서드 | 설명 |
|--------|------|
| `IsCollectionType(string typeFullName)` | 컬렉션 타입 여부 (튜플 제외) |
| `IsTupleType(string typeFullName)` | 튜플 타입 여부 |
| `GetCountExpression(string variableName, string typeFullName)` | `.Count` 또는 `.Length` 표현식 생성 |
| `GetRequestFieldName(string parameterName)` | `request.params.{name}` 필드명 생성 |
| `GetResponseFieldName()` | `response.result` 필드명 반환 |
| `GetResponseCountFieldName()` | `response.result.count` 필드명 반환 |

**컬렉션 인식 타입:** `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>`, `HashSet<T>`, `Dictionary<K,V>`, `IDictionary<K,V>`, `IReadOnlyDictionary<K,V>`, `Queue<T>`, `Stack<T>`, 배열(`T[]`)

---

## ConstructorParameterExtractor

대상 클래스의 생성자 파라미터를 추출하는 유틸리티입니다.

| 메서드 | 설명 |
|--------|------|
| `ExtractParameters(INamedTypeSymbol classSymbol)` | 생성자 파라미터 추출 |

**우선순위 규칙:**

1. Primary Constructor가 있으면 해당 파라미터 사용
2. 여러 생성자가 있으면 가장 많은 파라미터를 가진 생성자 선택
3. 파라미터가 없으면 빈 리스트 반환

---

## ParameterNameResolver

생성된 Observable 클래스의 생성자 파라미터명 충돌을 해결하는 유틸리티입니다.

| 메서드 | 설명 |
|--------|------|
| `ResolveName(string parameterName)` | 단일 파라미터명 변환 |
| `ResolveNames(List<ParameterInfo> parameters)` | 배치 파라미터명 변환 |

**변환 예시:**

| 원본 | 변환 결과 | 이유 |
|------|-----------|------|
| `logger` | `baseLogger` | Observable 클래스의 `logger`와 충돌 |
| `_logger` | `baseLogger` | 언더스코어 접두사 제거 후 `base` 접두사 추가 |
| `activitySource` | `baseActivitySource` | Observable 클래스의 예약 파라미터와 충돌 |
| `connectionString` | `connectionString` | 충돌 없음 — 변환하지 않음 |

---

## SymbolDisplayFormats

결정적 타입 문자열 생성을 위한 표시 형식입니다.

| 필드 | 설명 |
|------|------|
| `GlobalQualifiedFormat` | `global::` 접두사를 항상 포함하는 형식 |

`FullyQualifiedFormat`은 using 문에 따라 출력이 달라질 수 있지만, `GlobalQualifiedFormat`은 항상 `global::System.Collections.Generic.List<T>` 형태로 출력하여 결정적 코드 생성을 보장합니다.

---

## IObservablePort

소스 생성기 대상 클래스를 식별하는 마커 인터페이스입니다.

**네임스페이스:** `Functorium.Abstractions.Observabilities`

```csharp
public interface IObservablePort
{
    string RequestCategory { get; }
}
```

`RequestCategory`는 관측 가능성 태그에서 요청 카테고리를 구분하는 데 사용됩니다.

---

## SourceGeneratorTestRunner

소스 생성기 테스트를 위한 유틸리티 클래스입니다.

**네임스페이스:** `Functorium.Testing.Actions.SourceGenerators`

| 메서드 | 설명 |
|--------|------|
| `Generate<TGenerator>(this TGenerator, string sourceCode)` | 생성기를 실행하고 생성된 코드 반환 (진단 오류 시 실패) |
| `GenerateWithDiagnostics<TGenerator>(this TGenerator, string sourceCode)` | 생성된 코드와 Diagnostics를 함께 반환 |

두 메서드 모두 `IIncrementalGenerator`에 대한 확장 메서드이며, 내부적으로 `CSharpCompilation`과 `CSharpGeneratorDriver`를 사용하여 격리된 컴파일 환경에서 생성기를 실행합니다.
