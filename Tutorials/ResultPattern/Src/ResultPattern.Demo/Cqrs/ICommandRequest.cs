using Mediator;

namespace ResultPattern.Demo.Cqrs;

/// <summary>
/// Result 패턴을 사용하는 Command 인터페이스.
/// TSuccess는 성공 시 반환할 데이터 타입입니다.
/// </summary>
public interface ICommandRequest<TSuccess> : ICommand<FinResponse<TSuccess>>
{
}

/// <summary>
/// Result 패턴을 사용하는 Command Handler 인터페이스
/// </summary>
public interface ICommandUsecase<in TCommand, TSuccess>
    : ICommandHandler<TCommand, FinResponse<TSuccess>>
    where TCommand : ICommandRequest<TSuccess>
{
}
