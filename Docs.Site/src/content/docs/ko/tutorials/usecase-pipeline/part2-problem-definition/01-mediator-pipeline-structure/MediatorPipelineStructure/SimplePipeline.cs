using Mediator;

namespace MediatorPipelineStructure;

/// <summary>
/// Pipeline Behavior 구조를 보여주는 간단한 예제.
/// IPipelineBehavior의 where 제약이 Pipeline의 적용 범위를 결정합니다.
/// </summary>
public sealed class SimpleLoggingPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
{
    public List<string> Logs { get; } = [];

    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        Logs.Add($"Before: {typeof(TRequest).Name}");
        var response = await next(request, cancellationToken);
        Logs.Add($"After: {typeof(TResponse).Name}");
        return response;
    }
}

/// <summary>
/// TResponse에 제약을 추가하는 Pipeline.
/// where TResponse : IResult 같은 제약이 있으면
/// Pipeline은 TResponse의 멤버에 직접 접근할 수 있습니다.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
}

public sealed class ConstrainedPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IResult
{
    public List<string> Logs { get; } = [];

    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next(request, cancellationToken);

        // TResponse가 IResult를 구현하므로 직접 접근 가능
        if (response.IsSuccess)
            Logs.Add("Success");
        else
            Logs.Add("Failure");

        return response;
    }
}
