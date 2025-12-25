using Microsoft.Extensions.Logging;

namespace Cqrs.Demo.Usecases;

/// <summary>
/// 모든 사용자 조회 Query 예제
/// </summary>
public sealed class GetAllUsersQuery
{
    /// <summary>
    /// Query Request - 파라미터 없음
    /// </summary>
    public sealed record class Request() : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 사용자 목록
    /// </summary>
    public sealed record class Response : ResponseBase<Response>
    {
        public Seq<UserDto> Users { get; init; } = Seq<UserDto>.Empty;

        public Response() { }
        public Response(Seq<UserDto> users)
        {
            Users = users;
        }
    }

    /// <summary>
    /// 사용자 DTO
    /// </summary>
    public sealed record class UserDto(
        Guid UserId,
        string Name,
        string Email);

    /// <summary>
    /// Query Usecase
    /// </summary>
    internal sealed class Usecase(
        ILogger<Usecase> logger,
        IUserRepository userRepository)
        : IQueryUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;
        private readonly IUserRepository _userRepository = userRepository;

        public async ValueTask<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            //_logger.LogInformation("Getting all users");

            Fin<Seq<User>> result = await _userRepository.GetAllAsync(cancellationToken);

            return result.Match<Response>(
                Succ: users =>
                {
                    Seq<UserDto> userDtos = users.Map(u => new UserDto(u.Id, u.Name, u.Email));
                    //_logger.LogInformation("Found {Count} users", userDtos.Count);
                    return new Response(userDtos);
                },
                Fail: error =>
                {
                    //_logger.LogError("Failed to get users: {Error}", error.Message);
                    return Response.CreateFail(error);
                });
        }
    }
}
