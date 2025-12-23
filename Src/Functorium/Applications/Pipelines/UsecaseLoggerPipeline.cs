using Functorium.Abstractions;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Applications.Cqrs;

using Mediator;

using Microsoft.Extensions.Logging;

using ObservabilityFields = Functorium.Adapters.Observabilities.ObservabilityFields;

namespace Functorium.Applications.Pipelines;

public sealed class UsecaseLoggerPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse<IResponse>
{
    private readonly ILogger<UsecaseLoggerPipeline<TRequest, TResponse>> _logger;

    public UsecaseLoggerPipeline(ILogger<UsecaseLoggerPipeline<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        string requestCqrs = GetRequestCqrs(request);
        string requestHandler = GetRequestHandler();
        string requestHandlerMethod = "Handle";

        // 요청 로그
        _logger.LogRequestMessage(
            ObservabilityFields.Request.Layer.Application,
            ObservabilityFields.Request.Category.Usecase,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            request);

        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

        TResponse response = await next(request, cancellationToken);

        double elapsed = ElapsedTimeCalculator.CalculateElapsedMilliseconds(startTimestamp);
        LogResponse(response, requestCqrs, requestHandler, requestHandlerMethod, elapsed);

        return response;
    }

    private void LogResponse(TResponse response, string requestCqrs, string requestHandler, string requestHandlerMethod, double elapsed)
    {
        if (response.IsSucc)
        {
            _logger.LogResponseMessage(
                ObservabilityFields.Request.Layer.Application,
                ObservabilityFields.Request.Category.Usecase,
                requestCqrs,
                requestHandler,
                requestHandlerMethod,
                response,
                ObservabilityFields.Response.Status.Success,
                elapsed);
        }
        else
        {
            //          | Framework            | Language-Ext   | bool
            // ---------|----------------------|----------------|----------------
            // Error    | ErrorCodeExceptional | Exceptional    | IsExceptional
            // Warnning | ErrorCodeExpected    | Expected       | IsExpected

            if (response.Error.IsExceptional)
            {
                _logger.LogResponseMessageError(
                    ObservabilityFields.Request.Layer.Application,
                    ObservabilityFields.Request.Category.Usecase,
                    requestCqrs,
                    requestHandler,
                    requestHandlerMethod,
                    ObservabilityFields.Response.Status.Failure,
                    elapsed,
                    response.Error);
            }
            else
            {
                _logger.LogResponseMessageWarning(
                    ObservabilityFields.Request.Layer.Application,
                    ObservabilityFields.Request.Category.Usecase,
                    requestCqrs,
                    requestHandler,
                    requestHandlerMethod,
                    ObservabilityFields.Response.Status.Failure,
                    elapsed,
                    response.Error);
            }
        }
    }
}
