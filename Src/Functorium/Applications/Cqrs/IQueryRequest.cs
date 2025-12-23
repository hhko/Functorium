using Mediator;

namespace Functorium.Applications.Cqrs;

public interface IQueryRequest<TResponse>
    : IQuery<IFinResponse<TResponse>>
      where TResponse : IResponse;

public interface IQueryUsecase<in TQuery, TResponse>
    : IQueryHandler<TQuery, IFinResponse<TResponse>>
      where TQuery : IQueryRequest<TResponse>
      where TResponse : IResponse;
