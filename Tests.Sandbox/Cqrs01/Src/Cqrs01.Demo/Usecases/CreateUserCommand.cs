using Cqrs01.Demo.Domain;
using Cqrs01.Demo.Domain.ValueObjects;

using Functorium.Domains.ValueObjects.Validations;

using LanguageExt;
using LanguageExt.Common;

using Microsoft.Extensions.Logging;

namespace Cqrs01.Demo.Usecases;

/// <summary>
/// 사용자 생성 Command 예제
/// 중첩 클래스 패턴으로 Request, Response, Handler를 하나의 파일에 구성
/// </summary>
public sealed class CreateUserCommand
{
    /// <summary>
    /// Command Request - 사용자 생성에 필요한 데이터
    /// </summary>
    public sealed record Request(
        string Name,
        string Email) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 사용자 정보
    /// </summary>
    public sealed record Response(
        string UserId,
        string Name,
        string Email,
        DateTime CreatedAt);

    /// <summary>
    /// Command Handler - 실제 비즈니스 로직 구현
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IUserRepository userRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IUserRepository _userRepository = userRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // Value Objects 생성 및 유효성 검사
            var validation = (
                UserName.Validate(request.Name),
                UserEmail.Validate(request.Email)
            ).Apply((name, email) => (Name: name, Email: email));

            if (validation.IsFail)
            {
                return validation.ToFin().Match<FinResponse<Response>>(
                    Succ: _ => Error.New("Unexpected"),
                    Fail: error => error);
            }

            // Validation 성공 시 Value Objects 생성
            var validated = validation.Match(
                Succ: v => v,
                Fail: _ => throw new InvalidOperationException("Should not reach here"));

            var userName = UserName.Create(validated.Name)
                .IfFail(error => throw new InvalidOperationException(error.Message));
            var userEmail = UserEmail.Create(validated.Email)
                .IfFail(error => throw new InvalidOperationException(error.Message));

            // 이메일 중복 검사
            Fin<bool> existsResult = await _userRepository.ExistsByEmailAsync(userEmail, cancellationToken);

            if (existsResult.IsFail)
            {
                Error error = (Error)existsResult;
                return error;
            }

            bool exists = (bool)existsResult;
            if (exists)
            {
                return Error.New($"Email '{request.Email}' already exists");
            }

            // 사용자 생성
            var createResult = User.Create(userName, userEmail, DateTime.UtcNow);

            return await createResult.Match<Task<FinResponse<Response>>>(
                Succ: async user =>
                {
                    var saveResult = await _userRepository.CreateAsync(user, cancellationToken);
                    return saveResult.ToFinResponse(u =>
                        new Response(u.Id.ToString(), (string)u.Name, (string)u.Email, u.CreatedAt));
                },
                Fail: error => Task.FromResult<FinResponse<Response>>(error));
        }
    }
}
