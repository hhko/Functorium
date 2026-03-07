---
title: "Fin 직접 사용의 한계"
---

## 개요

앞 장에서 Pipeline의 `where` 제약이 응답 타입의 멤버 접근 범위를 결정한다는 것을 확인했습니다. 이제 `Fin<T>`를 직접 응답 타입으로 사용했을 때 어떤 한계가 발생하는지 살펴봅니다.

LanguageExt의 `Fin<T>`는 성공/실패를 표현하는 모나드로, Usecase의 응답 타입으로 이상적입니다. 하지만 `Fin<T>`는 **sealed struct**이기 때문에 Pipeline의 `where` 제약 조건으로 사용할 수 없습니다. 이 장에서는 `Fin<T>`를 Pipeline에서 직접 사용하려 할 때 발생하는 리플렉션 문제를 분석합니다.

## 핵심 개념

### 1. sealed struct는 제약 조건이 될 수 없다

`Fin<T>`는 sealed struct입니다. C#에서 struct는 상속이 불가능하므로, 제네릭 제약 조건으로 사용할 수 없습니다:

```csharp
// 이것은 컴파일 에러!
public class ValidationPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : Fin<???>  // 불가능! sealed struct는 제약이 될 수 없음
```

이 제약 때문에 Pipeline 내부에서 `TResponse`가 `Fin<T>`인지 알 수 없고, `IsSucc`, `Error` 등의 멤버에 접근할 수 없습니다.

### 2. 리플렉션이 필요한 3곳

`Fin<T>`를 Pipeline에서 사용하려면 **3곳에서 리플렉션**이 필요합니다:

#### 리플렉션 1: IsSucc 확인

```csharp
// 성공/실패를 확인하려면 리플렉션으로 IsSucc 프로퍼티를 조회해야 함
var type = response.GetType();
var property = type.GetProperty("IsSucc");
var isSucc = (bool)property.GetValue(response)!;
```

#### 리플렉션 2: Error 추출

```csharp
// 에러 정보를 가져오려면 리플렉션으로 Match 메서드를 호출해야 함
var matchMethod = type.GetMethod("Match", ...);
// 제네릭 Match를 리플렉션으로 호출하는 것은 매우 복잡
```

#### 리플렉션 3: 실패 Fin<T> 생성

```csharp
// 실패 응답을 생성하려면 Fin<T>.Fail을 리플렉션으로 호출해야 함
var innerType = responseType.GetGenericArguments()[0];
var finType = typeof(Fin<>).MakeGenericType(innerType);
var failMethod = finType.GetMethod("Fail", BindingFlags.Public | BindingFlags.Static);
return (TResponse)failMethod.Invoke(null, new object[] { error })!;
```

### 3. 리플렉션의 문제점

3곳의 리플렉션이 실제 코드베이스에서 어떤 비용을 발생시키는지 정리하면 다음과 같습니다.

| 문제 | 설명 |
|------|------|
| 런타임 성능 저하 | 매 요청마다 타입 정보를 동적으로 조회 |
| 컴파일 타임 안전성 상실 | 프로퍼티 이름 오타가 런타임에야 발견됨 |
| 유지보수 복잡성 | LanguageExt 버전 변경 시 리플렉션 코드 동기화 필요 |
| 코드 가독성 저하 | 비즈니스 로직과 리플렉션 코드가 혼재 |

리플렉션 3곳이라는 비용은 분명히 과도합니다. 다음 장에서는 래퍼 인터페이스를 도입하여 이 리플렉션을 줄일 수 있는지, 그리고 그 접근이 어디까지 유효한지 검토합니다.

## 학습 목표

이 장을 완료하면 다음을 할 수 있습니다:

1. `Fin<T>`가 sealed struct인 이유로 Pipeline 제약에 사용할 수 없음을 이해할 수 있다
2. Pipeline에서 `Fin<T>`를 직접 사용하면 리플렉션이 3곳에서 필요한 이유를 설명할 수 있다
3. 리플렉션 기반 접근의 구체적인 문제점을 나열할 수 있다

## 프로젝트 구조

```
02-Fin-Direct-Limitation/
├── FinDirectLimitation/
│   ├── FinDirectLimitation.csproj
│   ├── FinReflectionUtility.cs
│   └── Program.cs
├── FinDirectLimitation.Tests.Unit/
│   ├── FinDirectLimitation.Tests.Unit.csproj
│   ├── xunit.runner.json
│   └── FinReflectionUtilityTests.cs
└── README.md
```

## 실행 방법

```bash
# 프로그램 실행
dotnet run --project FinDirectLimitation

# 테스트 실행
dotnet test --project FinDirectLimitation.Tests.Unit
```

