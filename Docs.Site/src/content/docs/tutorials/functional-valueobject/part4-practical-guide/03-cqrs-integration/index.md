---
title: "CQRS and value object Integration"
---
## Overview

If an API endpoint receives a username, email, and age as strings and integers, where should the validation logic reside? In the Controller? In the Application Layer? By integrating value objects with a CQRS architecture, validation logic is encapsulated inside the value objects themselves, preventing invalid data from ever reaching the domain layer.

In this chapter, we cover patterns for validating input using value objects in Mediator pattern-based Command/Query Handlers, and converting `Fin<T>` to API Responses.

## Learning Objectives

- Implement patterns for converting input values to value objects for validation within Command Handlers.
- Apply methods for extracting value object values when converting query results to DTOs.
- Implement extension methods for converting `Fin<T>` to HTTP API Responses.
- Sequentially validate and compose multiple value objects using the `Bind` pattern.

## Why Is This Needed?

Integrating value objects in a CQRS architecture provides several benefits.

When primitive types are converted to value objects in Command Handlers, validation logic is encapsulated within the value objects, eliminating the need for duplicate validation in Controllers or the Application Layer. Inside Handlers, work is done with validated types like `Email`, `Age`, and `UserName`, so invalid data cannot reach the domain layer. Additionally, converting `Fin<T>` to `ApiResponse<T>` maintains a consistent response format across all API endpoints.

## Core Concepts

### value object Validation in Commands

The Command Handler converts input values to value objects for validation. The `Bind` pattern allows sequential validation of multiple value objects.

```csharp
public sealed record CreateUserCommand(string Name, string Email, int Age)
    : IRequest<Fin<CreateUserResponse>>;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Fin<CreateUserResponse>>
{
    public ValueTask<Fin<CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Sequential validation via Bind pattern
        var result = UserName.Create(request.Name)
            .Bind(name => Email.Create(request.Email)
                .Bind(email => Age.Create(request.Age)
                    .Map(age =>
                    {
                        var userId = _repository.Save(name, email, age);
                        return new CreateUserResponse(userId);
                    })));

        return ValueTask.FromResult(result);
    }
}
```

`Bind` only proceeds to the next step on success. If the first validation fails, subsequent validations are not performed and an error is returned immediately.

### Query and DTO Conversion

Queries data stored as value objects from the Repository and converts it to DTOs for return.

```csharp
public sealed record GetUserQuery(Guid UserId) : IRequest<Fin<UserDto>>;

public sealed record UserDto(string Name, string Email, int Age);

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, Fin<UserDto>>
{
    public ValueTask<Fin<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var result = _repository.FindById(request.UserId);
        return ValueTask.FromResult(result);
    }
}

// Repository
public Fin<UserDto> FindById(Guid id)
{
    if (_users.TryGetValue(id, out var user))
    {
        // Extract Value from value objects to create DTO
        return new UserDto(user.Name.Value, user.Email.Value, user.Age.Value);
    }
    return RepositoryErrors.UserNotFound(id);
}
```

The domain uses `UserName`, `Email`, and `Age` value objects, while API responses return DTOs with primitive types, separating the domain model from the API contract.

### Fin\<T\> -> ApiResponse Conversion

An extension method that converts `Fin<T>` to `ApiResponse<T>` for use in HTTP APIs.

```csharp
public static class FinExtensions
{
    public static ApiResponse<T> ToApiResponse<T>(this Fin<T> fin)
    {
        return fin.Match(
            Succ: data => ApiResponse<T>.Success(data),
            Fail: error => ApiResponse<T>.Failure(error.Message)
        );
    }
}

public class ApiResponse<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ApiResponse<T> Success(T data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static ApiResponse<T> Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}
```

Internally, `Fin<T>` handles success/failure, and at the API boundary it is converted to a format clients can understand.

### Mediator Pattern and value objects

The Mediator pattern reduces coupling between Commands/Queries and Handlers. Combined with value objects, input validation is encapsulated within Handlers.

```csharp
// DI configuration
services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
services.AddSingleton<UserRepository>();

// Send command
var command = new CreateUserCommand("Hong Gildong", "hong@example.com", 25);
var result = await mediator.Send(command);

// Process result
result.Match(
    Succ: response => Console.WriteLine($"Success: User ID = {response.UserId}"),
    Fail: error => Console.WriteLine($"Failure: {error.Message}")
);
```

The Controller converts requests to Commands and sends them, while the Handler is responsible for validation and business logic. The responsibilities of each layer become clear.

## Practical Guidelines

### Expected Output
```
=== CQRS and value object Integration ===

1. Using value objects in Commands
────────────────────────────────────────
   Success: User ID = 550e8400-e29b-41d4-a716-446655440001
   Failure: Username cannot be empty.

2. Using value objects in Queries
────────────────────────────────────────
   User: Existing User, Email: existing@example.com, Age: 30
   Error: User not found.

3. Fin<T> -> Response Conversion (FinExtensions)
────────────────────────────────────────
   Success response: Status=True, Data=UserDto { Name = Hong Gildong, Email = hong@example.com, Age = 25 }
   Failure response: Status=False, Error=User not found.
```

### Usage Example in Controllers

A pattern combining Mediator with `ToApiResponse()` in an actual Web API project.

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var command = new CreateUserCommand(request.Name, request.Email, request.Age);
        var result = await _mediator.Send(command);

        var response = result.ToApiResponse();

        return response.IsSuccess
            ? Ok(response)
            : BadRequest(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var query = new GetUserQuery(id);
        var result = await _mediator.Send(query);

        var response = result.ToApiResponse();

        return response.IsSuccess
            ? Ok(response)
            : NotFound(response);
    }
}
```

## Project Description

### Project Structure
```
03-CQRS-Integration/
├── CqrsIntegration/
│   ├── Program.cs                # Main executable (includes value objects, Command/Query, Handler)
│   └── CqrsIntegration.csproj    # Project file
└── README.md                     # Project documentation
```

### Dependencies
```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\..\..\Src\Functorium\Functorium.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Mediator.Abstractions" />
  <PackageReference Include="Mediator.SourceGenerator" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" />
</ItemGroup>
```

### Core Code

**value object Definitions**
```csharp
public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Fin<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Domain.Empty(value ?? "null");
        if (!value.Contains('@'))
            return Domain.InvalidFormat(value);
        return new Email(value.ToLowerInvariant());
    }

    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

    // IEquatable<Email> implementation...
}
```

**Command/Query Definitions**
```csharp
// Command: Create user
public sealed record CreateUserCommand(string Name, string Email, int Age)
    : IRequest<Fin<CreateUserResponse>>;

public sealed record CreateUserResponse(Guid UserId);

// Query: Get user
public sealed record GetUserQuery(Guid UserId) : IRequest<Fin<UserDto>>;

public sealed record UserDto(string Name, string Email, int Age);
```

**Handler Implementation**
```csharp
public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Fin<CreateUserResponse>>
{
    private readonly UserRepository _repository;

    public CreateUserCommandHandler(UserRepository repository) => _repository = repository;

    public ValueTask<Fin<CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var result = UserName.Create(request.Name)
            .Bind(name => Email.Create(request.Email)
                .Bind(email => Age.Create(request.Age)
                    .Map(age =>
                    {
                        var userId = _repository.Save(name, email, age);
                        return new CreateUserResponse(userId);
                    })));

        return ValueTask.FromResult(result);
    }
}
```

## Summary at a Glance

### CQRS and value object Integration Patterns

Summarizes how each layer utilizes value objects.

| Layer | Role | value object Usage |
|-------|------|-------------------|
| Controller | Receive requests, return responses | Uses DTOs, `ToApiResponse()` conversion |
| Command/Query | Deliver request data | Delivered as primitive types |
| Handler | Validation, business logic | Converts to value objects for validation |
| Repository | Data storage/retrieval | Stores as value objects, returns as DTOs |

### Bind vs Apply Pattern Selection

Choose the appropriate pattern based on the validation strategy.

| Pattern | Characteristics | Suitable Scenarios |
|---------|----------------|-------------------|
| `Bind` (sequential validation) | Stops at first failure | Dependent validations, resource conservation |
| `Apply` (parallel validation) | Collects all errors | Form validation, user feedback |

### API Response Structure

```
On success:
{
  "isSuccess": true,
  "data": { ... },
  "errorMessage": null
}

On failure:
{
  "isSuccess": false,
  "data": null,
  "errorMessage": "Username cannot be empty."
}
```

## FAQ

### Q1: Can Commands receive value objects directly instead of primitive types?
**A**: While technically possible, it is not recommended. Commands/Queries are API boundary contracts, so using serializable primitive types is standard practice. Converting to value objects in the Handler is clearer and easier to test.

### Q2: How do I collect all errors at once with the Apply pattern?
**A**: Use `Validation<Error, T>` with the `Apply` pattern.

```csharp
public ValueTask<Fin<CreateUserResponse>> Handle(CreateUserCommand request, CancellationToken ct)
{
    var validation = (UserName.Validate(request.Name),
                      Email.Validate(request.Email),
                      Age.Validate(request.Age))
        .Apply((name, email, age) =>
        {
            var userId = _repository.Save(name, email, age);
            return new CreateUserResponse(userId);
        });

    return ValueTask.FromResult(validation.ToFin());
}
```

For form validation where user feedback is important, the Apply pattern is more suitable as it can show all field errors at once.

### Q3: Why does the Repository return Fin\<T\>?
**A**: "User not found" is not an exceptional situation but a business-expected result. Returning `Fin<T>` forces callers to explicitly handle both success and failure, enabling safe code without missing edge cases.

---

## Tests

This project includes unit tests.

### Running Tests
```bash
cd CqrsIntegration.Tests.Unit
dotnet test
```

### Test Structure
```
CqrsIntegration.Tests.Unit/
├── CreateUserCommandHandlerTests.cs  # Command handler tests
├── GetUserQueryHandlerTests.cs       # Query handler tests
└── FinExtensionsTests.cs             # Fin->ApiResponse conversion tests
```

### Key Test Cases

| Test Class | Test Content |
|------------|-------------|
| CreateUserCommandHandlerTests | value object validation, Bind sequential validation, success/failure scenarios |
| GetUserQueryHandlerTests | Querying existing users, handling non-existing users |
| FinExtensionsTests | ToApiResponse conversion, Success/Failure mapping |

With CQRS integration, we now have a structure where value objects flow naturally from the API layer to the domain layer. The next chapter covers testing strategies for effectively verifying all these patterns.

---

→ [Chapter 4: Testing Strategies](../04-Testing-Strategies/)
