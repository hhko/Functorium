using Microsoft.Extensions.Logging;

namespace Cqrs.Demo.Usecases;

/// <summary>
/// 사용자 조회 Query 예제
/// 중첩 클래스 패턴으로 Request, Response, Usecase를 하나의 파일에 구성
/// </summary>
public sealed class GetUserByIdQuery
{
    /// <summary>
    /// Query Request - 조회할 사용자 ID
    /// </summary>
    public sealed record class Request(Guid UserId) : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 조회된 사용자 정보
    /// </summary>
    public sealed record class Response(
        Guid UserId,
        string Name,
        string Email,
        DateTime CreatedAt) : IResponse;

    /// <summary>
    /// Query Usecase - 조회 로직 구현
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IUserRepository userRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IUserRepository _userRepository = userRepository;

        public async ValueTask<IFinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Getting user by ID: {UserId}", request.UserId);

            Fin<User?> result = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            return result.Match<IFinResponse<Response>>(
                Succ: user =>
                {
                    if (user is null)
                    {
                        //_logger.LogWarning("User not found: {UserId}", request.UserId);
                        return FinResponseUtilites.ToResponseFail<Response>(
                            Error.New($"User with ID '{request.UserId}' not found"));
                    }

                    //_logger.LogInformation("User found: {UserId}, {Name}", user.Id, user.Name);
                    return FinResponseUtilites.ToResponse(
                        new Response(user.Id, user.Name, user.Email, user.CreatedAt));
                },
                Fail: error =>
                {
                    //_logger.LogError("Failed to get user: {Error}", error.Message);
                    return FinResponseUtilites.ToResponseFail<Response>(error);
                });
        }
    }
}
