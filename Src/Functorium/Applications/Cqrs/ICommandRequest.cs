using Mediator;

namespace Functorium.Applications.Cqrs;

public interface ICommandRequest<TResponse>
    : ICommand<TResponse>
      where TResponse : IResponse<TResponse>;

public interface ICommandUsecase<in TCommand, TResponse>
    : ICommandHandler<TCommand, TResponse>
      where TCommand : ICommandRequest<TResponse>
      where TResponse : IResponse<TResponse>;
