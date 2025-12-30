using Functorium.Abstractions;
using Functorium.Adapters.Observabilities;
using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Applications.Cqrs;

using LanguageExt.Common;

using Mediator;

using Microsoft.Extensions.Logging;

using ObservabilityFields = Functorium.Adapters.Observabilities.ObservabilityFields;

namespace Functorium.Applications.Pipelines;

/// <summary>
/// Result 패턴을 위한 로깅 Pipeline.
/// IsSucc/IsFail 패턴을 사용하여 안전하게 응답을 로깅합니다.
/// </summary>
public sealed class UsecaseLoggerPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse, IFinResponseFactory<TResponse>
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
            _logger.LogResponseMessageSuccess(
                ObservabilityFields.Request.Layer.Application,
                ObservabilityFields.Request.Category.Usecase,
                requestCqrs,
                requestHandler,
                requestHandlerMethod,
                response,
                ObservabilityFields.Response.Status.Success,
                elapsed);
        }
        else if (response.IsFail)
        {
            //          | Framework            | Language-Ext   | bool
            // ---------|----------------------|----------------|----------------
            // Error    | ErrorCodeExceptional | Exceptional    | IsExceptional
            // Warnning | ErrorCodeExpected    | Expected       | IsExpected

            // IFinResponseWithError를 통해 Error 접근
            if (response is IFinResponseWithError errorResponse)
            {
                Error error = errorResponse.Error;

                if (error.IsExceptional)
                {
                    _logger.LogResponseMessageError(
                        ObservabilityFields.Request.Layer.Application,
                        ObservabilityFields.Request.Category.Usecase,
                        requestCqrs,
                        requestHandler,
                        requestHandlerMethod,
                        ObservabilityFields.Response.Status.Failure,
                        elapsed,
                        error);
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
                        error);
                }
            }
        }
    }
}
