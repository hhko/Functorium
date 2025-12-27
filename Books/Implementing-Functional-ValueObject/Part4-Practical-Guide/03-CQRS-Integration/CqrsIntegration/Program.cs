using Functorium.Abstractions.Errors;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using LanguageExt;
using LanguageExt.Common;
using static LanguageExt.Prelude;

namespace CqrsIntegration;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== CQRS와 값 객체 통합 ===\n");

        // DI 설정
        var services = new ServiceCollection();
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddSingleton<UserRepository>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // 1. Command에서 값 객체 사용
        await DemonstrateCommand(mediator);

        // 2. Query에서 값 객체 사용
        await DemonstrateQuery(mediator);

        // 3. Fin<T> → Response 변환
        DemonstrateFinToResponse();
    }

    static async Task DemonstrateCommand(IMediator mediator)
    {
        Console.WriteLine("1. Command에서 값 객체 사용");
        Console.WriteLine("─".PadRight(40, '─'));

        // 유효한 요청
        var validCommand = new CreateUserCommand("홍길동", "hong@example.com", 25);
        var result = await mediator.Send(validCommand);

        result.Match(
            Succ: response => Console.WriteLine($"   성공: 사용자 ID = {response.UserId}"),
            Fail: error => Console.WriteLine($"   실패: {error.Message}")
        );

        // 유효하지 않은 요청
        var invalidCommand = new CreateUserCommand("", "invalid-email", -5);
        var result2 = await mediator.Send(invalidCommand);

        result2.Match(
            Succ: response => Console.WriteLine($"   성공: 사용자 ID = {response.UserId}"),
            Fail: error => Console.WriteLine($"   실패: {error.Message}")
        );

        Console.WriteLine();
    }

    static async Task DemonstrateQuery(IMediator mediator)
    {
        Console.WriteLine("2. Query에서 값 객체 사용");
        Console.WriteLine("─".PadRight(40, '─'));

        var query = new GetUserQuery(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"));
        var result = await mediator.Send(query);

        result.Match(
            Succ: user => Console.WriteLine($"   사용자: {user.Name}, 이메일: {user.Email}, 나이: {user.Age}"),
            Fail: error => Console.WriteLine($"   오류: {error.Message}")
        );

        // 존재하지 않는 사용자
        var notFoundQuery = new GetUserQuery(Guid.NewGuid());
        var notFoundResult = await mediator.Send(notFoundQuery);

        notFoundResult.Match(
            Succ: user => Console.WriteLine($"   사용자: {user.Name}"),
            Fail: error => Console.WriteLine($"   오류: {error.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateFinToResponse()
    {
        Console.WriteLine("3. Fin<T> → Response 변환 (FinExtensions)");
        Console.WriteLine("─".PadRight(40, '─'));

        // 성공 케이스
        Fin<UserDto> success = new UserDto("홍길동", "hong@example.com", 25);
        var successResponse = success.ToApiResponse();
        Console.WriteLine($"   성공 응답: Status={successResponse.IsSuccess}, Data={successResponse.Data}");

        // 실패 케이스
        Fin<UserDto> failure = Error.New("사용자를 찾을 수 없습니다.");
        var failureResponse = failure.ToApiResponse();
        Console.WriteLine($"   실패 응답: Status={failureResponse.IsSuccess}, Error={failureResponse.ErrorMessage}");

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 정의
// ========================================

public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Fin<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");
        if (!value.Contains('@'))
            return DomainErrors.InvalidFormat(value);
        return new Email(value.ToLowerInvariant());
    }

    public static Email CreateFromValidated(string value) => new(value.ToLowerInvariant());

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "이메일 주소가 비어있습니다.");
        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: "이메일 형식이 올바르지 않습니다.");
    }
}

public sealed class Age : IEquatable<Age>
{
    public int Value { get; }

    private Age(int value) => Value = value;

    public static Fin<Age> Create(int value)
    {
        if (value < 0)
            return DomainErrors.Negative(value);
        if (value > 150)
            return DomainErrors.TooOld(value);
        return new Age(value);
    }

    public static Age CreateFromValidated(int value) => new(value);

    public bool Equals(Age? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Age other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    internal static class DomainErrors
    {
        public static Error Negative(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Age)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: "나이는 음수일 수 없습니다.");
        public static Error TooOld(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(Age)}.{nameof(TooOld)}",
                errorCurrentValue: value,
                errorMessage: "나이는 150세를 초과할 수 없습니다.");
    }
}

public sealed class UserName : IEquatable<UserName>
{
    public string Value { get; }

    private UserName(string value) => Value = value;

    public static Fin<UserName> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DomainErrors.Empty(value ?? "null");
        if (value.Length > 100)
            return DomainErrors.TooLong(value.Length);
        return new UserName(value.Trim());
    }

    public static UserName CreateFromValidated(string value) => new(value.Trim());

    public bool Equals(UserName? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is UserName other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;

    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(UserName)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: "사용자 이름이 비어있습니다.");
        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(UserName)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: "사용자 이름은 100자를 초과할 수 없습니다.");
    }
}

// ========================================
// CQRS Commands & Queries (Mediator)
// ========================================

public sealed record CreateUserCommand(string Name, string Email, int Age)
    : IRequest<Fin<CreateUserResponse>>;

public sealed record CreateUserResponse(Guid UserId);

public sealed record GetUserQuery(Guid UserId) : IRequest<Fin<UserDto>>;

public sealed record UserDto(string Name, string Email, int Age);

// ========================================
// Handlers (Mediator)
// ========================================

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Fin<CreateUserResponse>>
{
    private readonly UserRepository _repository;

    public CreateUserCommandHandler(UserRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<Fin<CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Bind 패턴으로 순차적 검증
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

public sealed class GetUserQueryHandler : IRequestHandler<GetUserQuery, Fin<UserDto>>
{
    private readonly UserRepository _repository;

    public GetUserQueryHandler(UserRepository repository)
    {
        _repository = repository;
    }

    public ValueTask<Fin<UserDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var result = _repository.FindById(request.UserId);
        return ValueTask.FromResult(result);
    }
}

// ========================================
// Repository (In-Memory)
// ========================================

public class UserRepository
{
    private readonly Dictionary<Guid, UserEntity> _users = new()
    {
        [Guid.Parse("550e8400-e29b-41d4-a716-446655440000")] = new UserEntity(
            UserName.CreateFromValidated("기존 사용자"),
            Email.CreateFromValidated("existing@example.com"),
            Age.CreateFromValidated(30)
        )
    };

    public Guid Save(UserName name, Email email, Age age)
    {
        var id = Guid.NewGuid();
        _users[id] = new UserEntity(name, email, age);
        return id;
    }

    public Fin<UserDto> FindById(Guid id)
    {
        if (_users.TryGetValue(id, out var user))
        {
            return new UserDto(user.Name.Value, user.Email.Value, user.Age.Value);
        }
        return RepositoryErrors.UserNotFound(id);
    }

    internal static class RepositoryErrors
    {
        public static Error UserNotFound(Guid id) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(UserRepository)}.{nameof(UserNotFound)}",
                errorCurrentValue: id,
                errorMessage: "사용자를 찾을 수 없습니다.");
    }
}

public sealed record UserEntity(UserName Name, Email Email, Age Age);

// ========================================
// FinExtensions - Fin<T> → Response 변환
// ========================================

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

    private ApiResponse() { }

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
