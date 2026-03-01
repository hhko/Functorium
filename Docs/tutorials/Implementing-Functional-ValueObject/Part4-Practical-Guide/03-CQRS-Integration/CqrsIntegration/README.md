# 4.3 CQRS와 값 객체 통합
> **Part 4: 실전 가이드** | [← 이전: 4.2 ORM 통합](../../02-ORM-Integration/OrmIntegration/README.md) | [목차](../../../README.md) | [다음: 4.4 테스트 전략 →](../../04-Testing-Strategies/TestingStrategies/README.md)

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

---

## 다음 단계

테스트 전략을 학습합니다.

→ [4.4 테스트 전략](../../04-Testing-Strategies/TestingStrategies/README.md)
