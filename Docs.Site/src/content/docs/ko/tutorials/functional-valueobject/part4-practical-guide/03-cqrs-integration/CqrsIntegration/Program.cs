using Functorium.Applications.Usecases;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using Functorium.Domains.Errors;
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
        Console.WriteLine("=== CQRS와 값 객체 통합 (Functorium 프레임워크 기반) ===\n");

        // DI 설정
        var services = new ServiceCollection();
        services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // 1. Command에서 값 객체 사용
        await DemonstrateCommand(mediator);

        // 2. Query에서 값 객체 사용
        await DemonstrateQuery(mediator);

        // 3. FinResponse 직접 사용
        DemonstrateFinResponse();
    }

    static async Task DemonstrateCommand(IMediator mediator)
    {
        Console.WriteLine("1. Command에서 값 객체 사용");
        Console.WriteLine("─".PadRight(40, '─'));

        // 유효한 요청
        var validCommand = new CreateUserCommand.Request("홍길동", "hong@example.com", 25);
        var result = await mediator.Send(validCommand);

        result.Match(
            Succ: response => Console.WriteLine($"   성공: 사용자 ID = {response.UserId}"),
            Fail: error => Console.WriteLine($"   실패: {error.Message}")
        );

        // 유효하지 않은 요청
        var invalidCommand = new CreateUserCommand.Request("", "invalid-email", -5);
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

        var query = new GetUserByIdQuery.Request(Guid.Parse("550e8400-e29b-41d4-a716-446655440000"));
        var result = await mediator.Send(query);

        result.Match(
            Succ: user => Console.WriteLine($"   사용자: {user.Name}, 이메일: {user.Email}, 나이: {user.Age}"),
            Fail: error => Console.WriteLine($"   오류: {error.Message}")
        );

        // 존재하지 않는 사용자
        var notFoundQuery = new GetUserByIdQuery.Request(Guid.NewGuid());
        var notFoundResult = await mediator.Send(notFoundQuery);

        notFoundResult.Match(
            Succ: user => Console.WriteLine($"   사용자: {user.Name}"),
            Fail: error => Console.WriteLine($"   오류: {error.Message}")
        );

        Console.WriteLine();
    }

    static void DemonstrateFinResponse()
    {
        Console.WriteLine("3. FinResponse 직접 사용");
        Console.WriteLine("─".PadRight(40, '─'));

        // 성공 케이스 - 암시적 변환
        FinResponse<string> success = "성공 데이터";
        Console.WriteLine($"   성공 응답: IsSucc={success.IsSucc}, Value={success.Match(s => s, _ => "")}");

        // 실패 케이스 - 암시적 변환
        FinResponse<string> failure = Error.New("사용자를 찾을 수 없습니다.");
        Console.WriteLine($"   실패 응답: IsFail={failure.IsFail}, Error={failure.Match(_ => "", e => e.Message)}");

        // Map 연산
        FinResponse<int> mapped = success.Map(s => s.Length);
        Console.WriteLine($"   Map 연산: Value={mapped.Match(v => v.ToString(), _ => "N/A")}");

        Console.WriteLine();
    }
}

// ========================================
// 값 객체 정의 (Functorium 프레임워크 기반)
// ========================================

/// <summary>
/// Email 값 객체 (SimpleValueObject 기반)
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Email : SimpleValueObject<string>
{
    private Email(string value) : base(value) { }

    public string Address => Value;

    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    public static Email CreateFromValidated(string value) => new(value);

    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant());

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<Email>(new DomainErrorKind.Empty(), value ?? "null",
                $"Email address cannot be empty. Current value: '{value}'");

    private static Validation<Error, string> ValidateFormat(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@')
            ? value
            : DomainError.For<Email>(new DomainErrorKind.InvalidFormat(), value ?? "null",
                $"Invalid email format. Current value: '{value}'");

    public static implicit operator string(Email email) => email.Value;
}

/// <summary>
/// Age 값 객체 (ComparableSimpleValueObject 기반)
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class Age : ComparableSimpleValueObject<int>
{
    private Age(int value) : base(value) { }

    public int Years => Value;

    public static Fin<Age> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Age(validValue));

    public static Age CreateFromValidated(int value) => new(value);

    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotTooOld(value))
            .Map(_ => value);

    private static Validation<Error, int> ValidateNotNegative(int value) =>
        value >= 0
            ? value
            : DomainError.For<Age, int>(new DomainErrorKind.Negative(), value,
                $"Age cannot be negative. Current value: '{value}'");

    private static Validation<Error, int> ValidateNotTooOld(int value) =>
        value <= 150
            ? value
            : DomainError.For<Age, int>(new DomainErrorKind.AboveMaximum(), value,
                $"Age cannot exceed 150 years. Current value: '{value}'");

    public static implicit operator int(Age age) => age.Value;
}

/// <summary>
/// UserName 값 객체 (SimpleValueObject 기반)
/// DomainError 헬퍼를 사용한 간결한 에러 처리
/// </summary>
public sealed class UserName : SimpleValueObject<string>
{
    private UserName(string value) : base(value) { }

    public string Name => Value;

    public static Fin<UserName> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new UserName(validValue));

    public static UserName CreateFromValidated(string value) => new(value);

    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateLength(value))
            .Map(valid => valid.Trim());

    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainError.For<UserName>(new DomainErrorKind.Empty(), value ?? "null",
                $"User name cannot be empty. Current value: '{value}'");

    private static Validation<Error, string> ValidateLength(string value) =>
        value.Length <= 100
            ? value
            : DomainError.For<UserName, int>(new DomainErrorKind.TooLong(), value.Length,
                $"User name cannot exceed 100 characters. Current length: '{value.Length}'");

    public static implicit operator string(UserName userName) => userName.Value;
}

// ========================================
// CQRS Command - 중첩 클래스 패턴
// ========================================

/// <summary>
/// 사용자 생성 Command
/// </summary>
public sealed class CreateUserCommand
{
    /// <summary>
    /// Command Request - 사용자 생성에 필요한 데이터
    /// </summary>
    public sealed record Request(string Name, string Email, int Age)
        : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 사용자 정보
    /// </summary>
    public sealed record Response(Guid UserId);

    /// <summary>
    /// Command Handler - 실제 비즈니스 로직 구현
    /// </summary>
    internal sealed class Usecase(IUserRepository repository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IUserRepository _repository = repository;

        public ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Bind 패턴으로 순차적 검증 및 생성
            var result = UserName.Create(request.Name)
                .Bind(name => Email.Create(request.Email)
                    .Bind(email => Age.Create(request.Age)
                        .Map(age =>
                        {
                            var userId = _repository.Save(name, email, age);
                            return new Response(userId);
                        })));

            // Fin → FinResponse 변환
            return ValueTask.FromResult(result.ToFinResponse());
        }
    }
}

// ========================================
// CQRS Query - 중첩 클래스 패턴
// ========================================

/// <summary>
/// ID로 사용자 조회 Query
/// </summary>
public sealed class GetUserByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 사용자 ID
    /// </summary>
    public sealed record Request(Guid UserId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 사용자 정보
    /// </summary>
    public sealed record Response(string Name, string Email, int Age);

    /// <summary>
    /// Query Handler - 사용자 조회 로직
    /// </summary>
    internal sealed class Usecase(IUserRepository repository)
        : IQueryUsecase<Request, Response>
    {
        private readonly IUserRepository _repository = repository;

        public ValueTask<FinResponse<Response>> Handle(
            Request request,
            CancellationToken cancellationToken)
        {
            // Repository가 없는 경우 Fail을 반환하므로 간단하게 처리
            Fin<UserEntity> getResult = _repository.FindById(request.UserId);

            return ValueTask.FromResult(getResult.ToFinResponse(user =>
                new Response(user.Name.Name, user.Email.Address, user.Age.Years)));
        }
    }
}

// ========================================
// Repository 인터페이스 및 구현
// ========================================

/// <summary>
/// 사용자 Repository 인터페이스
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// 사용자 저장
    /// </summary>
    Guid Save(UserName name, Email email, Age age);

    /// <summary>
    /// ID로 사용자 조회. 없으면 Fail 반환
    /// </summary>
    Fin<UserEntity> FindById(Guid id);
}

/// <summary>
/// In-Memory 사용자 Repository 구현
/// </summary>
public class InMemoryUserRepository : IUserRepository
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

    public Fin<UserEntity> FindById(Guid id)
    {
        if (_users.TryGetValue(id, out var user))
        {
            return user; // 암시적 변환으로 Fin.Succ(user)
        }
        return DomainError.For<InMemoryUserRepository, Guid>(new DomainErrorKind.NotFound(), id,
            $"User not found. Current value: '{id}'");
    }
}

/// <summary>
/// 사용자 엔티티
/// </summary>
public sealed record UserEntity(UserName Name, Email Email, Age Age);
