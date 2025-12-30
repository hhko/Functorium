using Functorium.Abstractions.Errors;
using Functorium.Applications.Cqrs;
using Functorium.Domains.ValueObjects;
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
/// </summary>
public sealed class Email : SimpleValueObject<string>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private Email(string value) : base(value) { }

    /// <summary>
    /// 이메일 주소에 대한 public 접근자
    /// </summary>
    public string Address => Value;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Email> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new Email(validValue));

    // 4. Internal CreateFromValidated 메서드
    internal static Email CreateFromValidated(string value) => new(value);

    // 5. Public Validate 메서드 - 독립 검증 규칙들을 병렬로 실행
    public static Validation<Error, string> Validate(string value) =>
        (ValidateNotEmpty(value), ValidateFormat(value))
            .Apply((_, validFormat) => validFormat.ToLowerInvariant())
            .As();

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    // 5.2 형식 검증
    private static Validation<Error, string> ValidateFormat(string value) =>
        !string.IsNullOrWhiteSpace(value) && value.Contains('@')
            ? value
            : DomainErrors.InvalidFormat(value);

    public static implicit operator string(Email email) => email.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"Email address cannot be empty. Current value: '{value}'");

        public static Error InvalidFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(InvalidFormat)}",
                errorCurrentValue: value,
                errorMessage: $"Invalid email format. Current value: '{value}'");
    }
}

/// <summary>
/// Age 값 객체 (ComparableSimpleValueObject 기반)
/// </summary>
public sealed class Age : ComparableSimpleValueObject<int>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private Age(int value) : base(value) { }

    /// <summary>
    /// 나이 값에 대한 public 접근자
    /// </summary>
    public int Years => Value;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<Age> Create(int value) =>
        CreateFromValidation(
            Validate(value),
            validValue => new Age(validValue));

    // 4. Internal CreateFromValidated 메서드
    internal static Age CreateFromValidated(int value) => new(value);

    // 5. Public Validate 메서드 - 순차 검증 (범위 검증은 의존성이 있음)
    public static Validation<Error, int> Validate(int value) =>
        ValidateNotNegative(value)
            .Bind(_ => ValidateNotTooOld(value))
            .Map(_ => value);

    // 5.1 음수 검증
    private static Validation<Error, int> ValidateNotNegative(int value) =>
        value >= 0
            ? value
            : DomainErrors.Negative(value);

    // 5.2 최대값 검증
    private static Validation<Error, int> ValidateNotTooOld(int value) =>
        value <= 150
            ? value
            : DomainErrors.TooOld(value);

    public static implicit operator int(Age age) => age.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Negative(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Age)}.{nameof(Negative)}",
                errorCurrentValue: value,
                errorMessage: $"Age cannot be negative. Current value: '{value}'");

        public static Error TooOld(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Age)}.{nameof(TooOld)}",
                errorCurrentValue: value,
                errorMessage: $"Age cannot exceed 150 years. Current value: '{value}'");
    }
}

/// <summary>
/// UserName 값 객체 (SimpleValueObject 기반)
/// </summary>
public sealed class UserName : SimpleValueObject<string>
{
    // 2. Private 생성자 - 단순 대입만 처리
    private UserName(string value) : base(value) { }

    /// <summary>
    /// 이름 값에 대한 public 접근자
    /// </summary>
    public string Name => Value;

    // 3. Public Create 메서드 - 검증과 생성을 연결
    public static Fin<UserName> Create(string? value) =>
        CreateFromValidation(
            Validate(value ?? "null"),
            validValue => new UserName(validValue));

    // 4. Internal CreateFromValidated 메서드
    internal static UserName CreateFromValidated(string value) => new(value);

    // 5. Public Validate 메서드 - 순차 검증 (길이 검증은 빈 값 검증에 의존)
    public static Validation<Error, string> Validate(string value) =>
        ValidateNotEmpty(value)
            .Bind(_ => ValidateLength(value))
            .Map(valid => valid.Trim());

    // 5.1 빈 값 검증
    private static Validation<Error, string> ValidateNotEmpty(string value) =>
        !string.IsNullOrWhiteSpace(value)
            ? value
            : DomainErrors.Empty(value);

    // 5.2 길이 검증
    private static Validation<Error, string> ValidateLength(string value) =>
        value.Length <= 100
            ? value
            : DomainErrors.TooLong(value.Length);

    public static implicit operator string(UserName userName) => userName.Value;

    // 7. DomainErrors 중첩 클래스
    internal static class DomainErrors
    {
        public static Error Empty(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(UserName)}.{nameof(Empty)}",
                errorCurrentValue: value,
                errorMessage: $"User name cannot be empty. Current value: '{value}'");

        public static Error TooLong(int length) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(UserName)}.{nameof(TooLong)}",
                errorCurrentValue: length,
                errorMessage: $"User name cannot exceed 100 characters. Current length: '{length}'");
    }
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
        return RepositoryErrors.UserNotFound(id);
    }

    internal static class RepositoryErrors
    {
        public static Error UserNotFound(Guid id) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(RepositoryErrors)}.{nameof(InMemoryUserRepository)}.{nameof(UserNotFound)}",
                errorCurrentValue: id,
                errorMessage: $"User not found. Current value: '{id}'");
    }
}

/// <summary>
/// 사용자 엔티티
/// </summary>
public sealed record UserEntity(UserName Name, Email Email, Age Age);
