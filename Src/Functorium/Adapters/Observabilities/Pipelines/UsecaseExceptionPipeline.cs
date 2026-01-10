using Functorium.Abstractions.Errors;
using Functorium.Applications.Cqrs;

using LanguageExt.Common;

using Mediator;

namespace Functorium.Adapters.Observabilities.Pipelines;

/// <summary>
/// Result 패턴을 위한 예외 처리 Pipeline.
/// 예외 발생 시 FinResponse.Fail로 변환합니다.
///
/// IFinResponseFactory{TSelf}의 static abstract 메서드를 활용하여 리플렉션 없이 타입 안전하게 구현.
/// </summary>
internal sealed class UsecaseExceptionPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponseFactory<TResponse>
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
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(UsecaseExceptionPipeline<TRequest, TResponse>)}.{nameof(Exception)}",
                exception: exception);
    }
}
