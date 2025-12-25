using Mediator;

namespace Functorium.Applications.Cqrs;

public interface IQueryRequest<TResponse>
    : IQuery<TResponse>
      where TResponse : IResponse<TResponse>;

public interface IQueryUsecase<in TQuery, TResponse>
    : IQueryHandler<TQuery, TResponse>
      where TQuery : IQueryRequest<TResponse>
      where TResponse : IResponse<TResponse>;
