using Microsoft.Extensions.Logging;

namespace Cqrs10.Demo.Usecases;

/// <summary>
/// 사용자 조회 Query 예제
/// 중첩 클래스 패턴으로 Request, Response, Handler를 하나의 파일에 구성
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
    public sealed record Response(
        Guid UserId,
        string Name,
        string Email,
        DateTime CreatedAt);

    /// <summary>
    /// Query Handler - 조회 로직 구현
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IUserRepository userRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IUserRepository _userRepository = userRepository;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            Fin<User?> result = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            return result.Match<FinResponse<Response>>(
                Succ: user =>
                {
                    if (user is null)
                    {
                        return Error.New($"User with ID '{request.UserId}' not found");
                    }

                    return new Response(user.Id, user.Name, user.Email, user.CreatedAt);
                },
                Fail: error => error);
        }
    }
}
