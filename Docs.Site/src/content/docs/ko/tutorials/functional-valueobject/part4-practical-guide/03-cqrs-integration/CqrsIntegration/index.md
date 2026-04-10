---
title: "CQRS와 값 객체 통합"
---
## 개요

CQRS(Command Query Responsibility Segregation) 패턴에서 값 객체를 활용하는 방법을 학습합니다.

---

## 학습 목표

- Command에서 값 객체 검증
- Query에서 값 객체 반환
- `Fin<T>` → API Response 변환
- Apply 패턴으로 모든 검증 오류 수집

---

## 실행 방법

```bash
cd Docs/tutorials/Functional-ValueObject/04-practical-guide/03-CQRS-Integration/CqrsIntegration
dotnet run
```

---

## 예상 출력

```
=== CQRS와 값 객체 통합 ===

1. Command에서 값 객체 사용
────────────────────────────────────────
   성공: 사용자 ID = ...
   실패:
      - 이름은 필수입니다.
      - 유효한 이메일 형식이 아닙니다.
      - 나이는 0 이상이어야 합니다.

2. Query에서 값 객체 사용
────────────────────────────────────────
   사용자: 기존 사용자, 이메일: existing@example.com, 나이: 30

3. Fin<T> → Response 변환 (FinExtensions)
────────────────────────────────────────
   성공 응답: Status=True, Data=...
   실패 응답: Status=False, Error=사용자를 찾을 수 없습니다.
```

---

## 핵심 코드 설명

### 1. Command Handler에서 Apply 패턴

```csharp
public class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Validation<Error, CreateUserResponse>>
{
    public Task<Validation<Error, CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Apply 패턴으로 모든 검증 오류 수집
        var result = (
            UserName.Create(request.Name),
            Email.Create(request.Email),
            Age.Create(request.Age)
        ).Apply((name, email, age) =>
        {
            var userId = _repository.Save(name, email, age);
            return new CreateUserResponse(userId);
        });

        return Task.FromResult(result);
    }
}
```

### 2. Query Handler에서 Fin<T> 반환

```csharp
public class GetUserQueryHandler : IRequestHandler<GetUserQuery, Fin<UserDto>>
{
    public Task<Fin<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var result = _repository.FindById(request.UserId);
        return Task.FromResult(result);
    }
}
```

### 3. FinExtensions - Response 변환

```csharp
public static class FinExtensions
{
    public static ApiResponse<T> ToApiResponse<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: data => ApiResponse<T>.Success(data),
            Fail: error => ApiResponse<T>.Failure(error.Code.ToString(), error.Message)
        );
    }
}
```

---

## CQRS + 값 객체 흐름

```
HTTP Request
     │
     ▼
┌─────────────┐
│  Controller │
└──────┬──────┘
       │ CreateUserCommand(string, string, int)
       ▼
┌─────────────────────────────────────────────────┐
│              Command Handler                     │
│  ┌──────────────────────────────────────────┐  │
│  │ (                                         │  │
│  │   UserName.Create(name),     ←──┐        │  │
│  │   Email.Create(email),          │ Apply  │  │
│  │   Age.Create(age)               │        │  │
│  │ ).Apply(...)                 ───┘        │  │
│  └──────────────────────────────────────────┘  │
└──────┬──────────────────────────────────────────┘
       │
       ▼
Validation<Error, Response>
       │
       ▼
┌─────────────┐
│ API Response│
└─────────────┘
```

## FAQ

### Q1: Command Handler에서 `Apply` 패턴을 사용하는 이유는 무엇인가요?
**A**: `Apply` 패턴은 모든 검증을 병렬로 실행하여 실패한 검증의 오류를 한 번에 수집합니다. `Bind`를 사용하면 첫 번째 오류에서 중단되어 나머지 오류를 알 수 없지만, `Apply`는 사용자에게 모든 입력 오류를 한꺼번에 알려줄 수 있습니다.

### Q2: Query Handler의 반환 타입은 왜 `Fin<T>`를 사용하나요?
**A**: 조회 결과가 없는 경우(예: 존재하지 않는 사용자 ID)를 명시적으로 표현하기 위해서입니다. `null` 대신 `Fin<T>`를 사용하면 호출 측에서 실패 케이스를 반드시 처리해야 하므로 `NullReferenceException`을 방지할 수 있습니다.

### Q3: `FinExtensions.ToApiResponse`는 실제 API에서 어떻게 활용하나요?
**A**: Controller에서 Handler의 결과를 받아 `ToApiResponse()`를 호출하면, 성공 시 데이터가 포함된 200 응답을, 실패 시 에러 메시지가 포함된 응답을 일관된 형식으로 반환합니다. 모든 엔드포인트에서 동일한 응답 구조를 유지할 수 있습니다.

---

## 다음 단계

테스트 전략을 학습합니다.

→ [4.4 테스트 전략](../../04-Testing-Strategies/TestingStrategies/)
