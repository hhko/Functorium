using Functorium.Abstractions.Errors;
using Functorium.Applications.Cqrs;

using Mediator;

namespace Functorium.Applications.Pipelines;

public sealed class UsecaseExceptionPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IResponse<TResponse>
{
    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(request, cancellationToken);
        }
        catch (Exception exp)
        {
            return TResponse.CreateFail(ApplicationErrors.Exception(exp));
        }
    }

    internal static partial class ApplicationErrors
    {
        public static Error Exception(Exception exception) =>
            ErrorCodeFactory.CreateFromException(
                //errorCode: $"{nameof(ApplicationErrors)}.UsecaseExceptionPipeline.{nameof(Exception)}",
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(UsecaseExceptionPipeline<TRequest, TResponse>)}.{nameof(Exception)}",
                exception: exception);
    }
}