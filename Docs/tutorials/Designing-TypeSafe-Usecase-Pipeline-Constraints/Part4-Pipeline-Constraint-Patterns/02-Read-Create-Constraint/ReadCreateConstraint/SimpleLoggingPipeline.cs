using Functorium.Applications.Usecases;

namespace ReadCreateConstraint;

/// <summary>
/// Read + Create 제약: IFinResponse + IFinResponseFactory<TResponse>
/// </summary>
public sealed class SimpleLoggingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public List<string> Logs { get; } = [];

    public TResponse LogAndReturn(TResponse response)
    {
        // Read: 성공/실패 확인
        if (response.IsSucc)
        {
            Logs.Add("Success");
        }
        else
        {
            // Error 접근: 패턴 매칭
            if (response is IFinResponseWithError fail)
                Logs.Add($"Fail: {fail.Error}");
            else
                Logs.Add("Fail: unknown");
        }

        return response;
    }
}

/// <summary>
/// Read + Create 제약: Tracing Pipeline
/// </summary>
public sealed class SimpleTracingPipeline<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public List<string> Tags { get; } = [];

    public TResponse TraceAndReturn(TResponse response)
    {
        Tags.Add($"status:{(response.IsSucc ? "ok" : "error")}");

        if (response is IFinResponseWithError fail)
            Tags.Add($"error.message:{fail.Error}");

        return response;
    }
}
