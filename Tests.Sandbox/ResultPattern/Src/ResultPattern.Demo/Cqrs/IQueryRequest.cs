using Mediator;

namespace ResultPattern.Demo.Cqrs;

/// <summary>
/// Result 패턴을 사용하는 Query 인터페이스.
/// TSuccess는 성공 시 반환할 데이터 타입입니다.
/// </summary>
public interface IQueryRequest<TSuccess> : IQuery<FinResponse<TSuccess>>
{
}

/// <summary>
/// Result 패턴을 사용하는 Query Handler 인터페이스
/// </summary>
public interface IQueryUsecase<in TQuery, TSuccess>
    : IQueryHandler<TQuery, FinResponse<TSuccess>>
    where TQuery : IQueryRequest<TSuccess>
{
}
