using Microsoft.Extensions.Logging;

namespace Cqrs.Demo.Usecases;

/// <summary>
/// 사용자 생성 Command 예제
/// 중첩 클래스 패턴으로 Request, Response, Usecase를 하나의 파일에 구성
/// </summary>
public sealed class CreateUserCommand
{
    /// <summary>
    /// Command Request - 사용자 생성에 필요한 데이터
    /// </summary>
    public sealed record class Request(
        string Name,
        string Email) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 사용자 정보
    /// </summary>
    public sealed record class Response(
        Guid UserId,
        string Name,
        string Email,
        DateTime CreatedAt) : IResponse;

    /// <summary>
    /// Command Usecase - 실제 비즈니스 로직 구현
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IUserRepository userRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IUserRepository _userRepository = userRepository;

        public async ValueTask<IFinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Creating user: {Name}, {Email}", request.Name, request.Email);

            // 이메일 중복 검사
            Fin<bool> existsResult = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);

            if (existsResult.IsFail)
            {
                Error error = (Error)existsResult;
                //_logger.LogError("Failed to check email existence: {Error}", error.Message);
                return FinResponseUtilites.ToResponseFail<Response>(error);
            }

            bool exists = (bool)existsResult;
            if (exists)
            {
                //_logger.LogWarning("Email already exists: {Email}", request.Email);
                return FinResponseUtilites.ToResponseFail<Response>(
                    Error.New($"Email '{request.Email}' already exists"));
            }

            // 사용자 생성
            User newUser = new(Guid.NewGuid(), request.Name, request.Email, DateTime.UtcNow);
            Fin<User> createResult = await _userRepository.CreateAsync(newUser, cancellationToken);

            return createResult.Match<IFinResponse<Response>>(
                Succ: user =>
                {
                    //_logger.LogInformation("User created successfully: {UserId}", user.Id);
                    return FinResponseUtilites.ToResponse(
                        new Response(user.Id, user.Name, user.Email, user.CreatedAt));
                },
                Fail: error =>
                {
                    //_logger.LogError("Failed to create user: {Error}", error.Message);
                    return FinResponseUtilites.ToResponseFail<Response>(error);
                });
        }
    }
}
