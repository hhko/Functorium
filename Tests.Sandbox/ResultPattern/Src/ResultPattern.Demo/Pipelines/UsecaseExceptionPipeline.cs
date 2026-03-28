using LanguageExt.Common;
using Mediator;
using ResultPattern.Demo.Cqrs;

namespace ResultPattern.Demo.Pipelines;

/// <summary>
/// Result 패턴을 위한 예외 처리 Pipeline.
/// 예외 발생 시 Result.Fail로 변환합니다.
///
/// IResultFactory{TSelf}의 static abstract 메서드를 활용하여 리플렉션 없이 타입 안전하게 구현.
/// </summary>
public sealed class UsecaseExceptionPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponseFactory<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"    [ExceptionPipeline] Enter: {typeof(TRequest).Name}");
        try
        {
            var result = await next(request, cancellationToken);
            Console.WriteLine($"    [ExceptionPipeline] Exit: Success");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    [ExceptionPipeline] Exit: Exception caught - {ex.Message}");
            // IFinResponseFactory<TResponse>.CreateFail을 통해 타입 안전하게 Fail 생성
            return TResponse.CreateFail(Error.New(ex));
        }
    }
}
