using Microsoft.Extensions.Logging;

namespace Cqrs01.Demo.Usecases;

/// <summary>
/// 모든 사용자 조회 Query 예제
/// </summary>
public sealed class GetAllUsersQuery
{
    /// <summary>
    /// Query Request - 파라미터 없음
    /// </summary>
    public sealed record Request() : IQueryRequest<Response>;

    /// <summary>
    /// Query Response - 사용자 목록
    /// </summary>
    public sealed record Response(Seq<UserDto> Users);

    /// <summary>
    /// 사용자 DTO
    /// </summary>
    public sealed record UserDto(
        Guid UserId,
        string Name,
        string Email);

    /// <summary>
    /// Query Handler
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
            Fin<Seq<User>> result = await _userRepository.GetAllAsync(cancellationToken);

            return result.ToFinResponse(users =>
            {
                Seq<UserDto> userDtos = users.Map(u => new UserDto(u.Id, u.Name, u.Email));
                return new Response(userDtos);
            });
        }
    }
}
