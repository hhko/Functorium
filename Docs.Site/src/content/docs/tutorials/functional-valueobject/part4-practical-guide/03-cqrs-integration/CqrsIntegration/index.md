---
title: "CQRS and Value Object Integration"
---
## Overview

Learn how to use value objects in the CQRS (Command Query Responsibility Segregation) pattern.

---

## Learning Objectives

- Validate value objects in Commands
- Return value objects in Queries
- Convert `Fin<T>` to API Response
- Collect all validation errors with the Apply pattern

---

## How to Run

```bash
cd Docs/tutorials/Functional-ValueObject/04-practical-guide/03-CQRS-Integration/CqrsIntegration
dotnet run
```

---

## Expected Output

```
=== CQRS and Value Object Integration ===

1. Using Value Objects in Commands
────────────────────────────────────────
   Success: User ID = ...
   Failure:
      - Name is required.
      - Not a valid email format.
      - Age must be 0 or greater.

2. Using Value Objects in Queries
────────────────────────────────────────
   User: Existing User, Email: existing@example.com, Age: 30

3. Fin<T> -> Response Conversion (FinExtensions)
────────────────────────────────────────
   Success response: Status=True, Data=...
   Failure response: Status=False, Error=User not found.
```

---

## Core Code Explanation

### 1. Apply Pattern in Command Handler

```csharp
public class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Validation<Error, CreateUserResponse>>
{
    public Task<Validation<Error, CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Apply pattern collects all validation errors
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

### 2. Returning Fin<T> in Query Handler

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

### 3. FinExtensions - Response Conversion

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

## CQRS + Value Object Flow

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

### Q1: Why is the `Apply` pattern used in the Command Handler?
**A**: The `Apply` pattern runs all validations in parallel and collects errors from failed validations at once. If `Bind` is used, it stops at the first error and the remaining errors remain unknown, but `Apply` can inform the user of all input errors at once.

### Q2: Why does the Query Handler return `Fin<T>`?
**A**: To explicitly express cases where the query result does not exist (e.g., a non-existent user ID). Using `Fin<T>` instead of `null` forces the caller to handle the failure case, preventing `NullReferenceException`.

### Q3: How is `FinExtensions.ToApiResponse` used in actual APIs?
**A**: The Controller receives the result from the Handler and calls `ToApiResponse()`, returning a 200 response with data on success and a response with an error message on failure, in a consistent format. This maintains the same response structure across all endpoints.

---

## Next Steps

Learn testing strategies.

-> [4.4 Testing Strategies](../../04-Testing-Strategies/TestingStrategies/)
