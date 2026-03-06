---
title: "Fin→FinResponse 브릿지"
---

## 개요

Repository 계층은 `Fin<T>`를 반환하고, Usecase 계층은 `FinResponse<T>`를 반환합니다. 이 두 계층을 연결하는 **브릿지**가 `ToFinResponse()` 확장 메서드입니다. 이 장에서는 다양한 변환 오버로드와 사용 시나리오를 학습합니다.

```
계층 간 타입 흐름:

Repository Layer          Usecase Layer
─────────────────        ─────────────────
Fin<Product>       ──→   FinResponse<Product>        직접 변환
Fin<Product>       ──→   FinResponse<ProductDto>     매퍼 변환
Fin<Unit>          ──→   FinResponse<string>         팩토리 변환
Fin<T> (Fail)      ──→   FinResponse<T> (Fail)       실패 전파
```

## 핵심 개념

### 1. 왜 브릿지가 필요한가?

- **`Fin<T>`**: LanguageExt의 Result 타입. sealed struct이므로 제약 조건으로 사용 불가.
- **`FinResponse<T>`**: IFinResponse 인터페이스 계층을 구현한 Discriminated Union. Pipeline 제약에 사용 가능.

Repository가 반환하는 `Fin<T>`를 Usecase의 `FinResponse<T>`로 변환해야 Pipeline 체인에서 타입 안전하게 처리할 수 있습니다.

### 2. 직접 변환: Fin\<A\> -> FinResponse\<A\>

가장 단순한 변환입니다. 성공 값의 타입이 동일할 때 사용합니다.

```csharp
Fin<string> fin = Fin<string>.Succ("Hello");
FinResponse<string> response = fin.ToFinResponse();
```

### 3. 매퍼 변환: Fin\<A\> -> FinResponse\<B\>

성공 값의 타입을 변환할 때 사용합니다. 예: Entity -> DTO

```csharp
Fin<string> fin = Fin<string>.Succ("Hello");
FinResponse<int> response = fin.ToFinResponse(s => s.Length);
```

### 4. 팩토리 변환: Fin\<A\> -> FinResponse\<B\>

원본 성공 값을 무시하고 새로운 값을 생성할 때 사용합니다. 예: `Fin<Unit>` -> `FinResponse<string>`

```csharp
Fin<Unit> fin = Fin<Unit>.Succ(Unit.Default);
FinResponse<string> response = fin.ToFinResponse(() => "Deleted successfully");
```

### 5. 실패 전파

`Fin`이 실패 상태이면, 변환 방식에 관계없이 Error가 그대로 `FinResponse`의 Fail로 전파됩니다.

```csharp
Fin<string> fin = Fin<string>.Fail(Error.New("not found"));
FinResponse<string> response = fin.ToFinResponse();
// response.IsFail == true
```

### 6. 변환 오버로드 정리

| 오버로드 | 시그니처 | 용도 |
|----------|---------|------|
| 직접 변환 | `Fin<A>.ToFinResponse()` | 동일 타입 변환 |
| 매퍼 변환 | `Fin<A>.ToFinResponse(Func<A, B>)` | Entity -> DTO |
| 팩토리 변환 | `Fin<A>.ToFinResponse(Func<B>)` | Unit -> Response |
| 커스텀 변환 | `Fin<A>.ToFinResponse(Func<A, FinResponse<B>>, Func<Error, FinResponse<B>>)` | 완전 제어 |

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. Repository(`Fin<T>`)와 Usecase(`FinResponse<T>`) 계층 간 변환이 필요한 이유를 설명할 수 있다
2. 상황에 맞는 `ToFinResponse()` 오버로드를 선택할 수 있다
3. 실패 상태가 변환 시 자동으로 전파되는 메커니즘을 이해할 수 있다

## 프로젝트 구조

```
04-Fin-To-FinResponse-Bridge/
├── FinToFinResponseBridge/
│   ├── FinToFinResponseBridge.csproj
│   ├── BridgeExamples.cs
│   └── Program.cs
├── FinToFinResponseBridge.Tests.Unit/
│   ├── FinToFinResponseBridge.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinToFinResponseBridgeTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FinToFinResponseBridge

# 테스트 실행
dotnet test --project FinToFinResponseBridge.Tests.Unit
```

