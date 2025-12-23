using Mediator;

namespace Functorium.Applications.Cqrs;

public interface ICommandRequest<TResponse>
    : ICommand<IFinResponse<TResponse>>
      where TResponse : IResponse;

public interface ICommandUsecase<in TCommand, TResponse>
    : ICommandHandler<TCommand, IFinResponse<TResponse>>
      where TCommand : ICommandRequest<TResponse>
      where TResponse : IResponse;
