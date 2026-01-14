using Functorium.Adapters.Observabilities.Loggers;
using Functorium.Adapters.Observabilities.Naming;
using Functorium.Applications.Cqrs;

using LanguageExt.Common;

using Mediator;

using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Pipelines;

/// <summary>
/// Result 패턴을 위한 로깅 Pipeline.
/// IsSucc/IsFail 패턴을 사용하여 안전하게 응답을 로깅합니다.
/// </summary>
internal sealed class UsecaseLoggingPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    private readonly ILogger<UsecaseLoggingPipeline<TRequest, TResponse>> _logger;

    public UsecaseLoggingPipeline(ILogger<UsecaseLoggingPipeline<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        string requestCqrs = GetRequestCqrs(request);
        string requestHandler = GetRequestHandler();
        string requestHandlerMethod = ObservabilityNaming.Methods.Handle;

        // 요청 로그
        _logger.LogRequestMessage(
            ObservabilityNaming.Layers.Application,
            ObservabilityNaming.Categories.Usecase,
            requestCqrs,
            requestHandler,
            requestHandlerMethod,
            request);

        long startTimestamp = ElapsedTimeCalculator.GetCurrentTimestamp();

        TResponse response = await next(request, cancellationToken);

        double elapsed = ElapsedTimeCalculator.CalculateElapsedSeconds(startTimestamp);
        LogResponse(response, requestCqrs, requestHandler, requestHandlerMethod, elapsed);

        return response;
    }

    private void LogResponse(TResponse response, string requestCqrs, string requestHandler, string requestHandlerMethod, double elapsed)
    {
        if (response.IsSucc)
        {
            _logger.LogResponseMessageSuccess(
                ObservabilityNaming.Layers.Application,
                ObservabilityNaming.Categories.Usecase,
                requestCqrs,
                requestHandler,
                requestHandlerMethod,
                response,
                ObservabilityNaming.Status.Success,
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
                var (errorType, errorCode) = GetErrorInfo(error);

                if (error.IsExceptional)
                {
                    _logger.LogResponseMessageError(
                        ObservabilityNaming.Layers.Application,
                        ObservabilityNaming.Categories.Usecase,
                        requestCqrs,
                        requestHandler,
                        requestHandlerMethod,
                        ObservabilityNaming.Status.Failure,
                        elapsed,
                        errorType,
                        errorCode,
                        error);
                }
                else
                {
                    _logger.LogResponseMessageWarning(
                        ObservabilityNaming.Layers.Application,
                        ObservabilityNaming.Categories.Usecase,
                        requestCqrs,
                        requestHandler,
                        requestHandlerMethod,
                        ObservabilityNaming.Status.Failure,
                        elapsed,
                        errorType,
                        errorCode,
                        error);
                }
            }
        }
    }
}
