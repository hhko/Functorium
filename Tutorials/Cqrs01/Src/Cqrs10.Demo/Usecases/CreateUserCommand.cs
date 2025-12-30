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
        Guid UserId,
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
            // 이메일 중복 검사
            Fin<bool> existsResult = await _userRepository.ExistsByEmailAsync(request.Email, cancellationToken);

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
            User newUser = new(Guid.NewGuid(), request.Name, request.Email, DateTime.UtcNow);
            Fin<User> createResult = await _userRepository.CreateAsync(newUser, cancellationToken);

            return createResult.ToFinResponse(user =>
                new Response(user.Id, user.Name, user.Email, user.CreatedAt));
        }
    }
}
