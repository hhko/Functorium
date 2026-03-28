namespace FinResponseWithError;

public static class LoggingPipelineExample
{
    public static string LogResponse<TResponse>(TResponse response)
        where TResponse : IFinResponse
    {
        if (response.IsSucc)
            return "Success";

        // 패턴 매칭으로 에러 접근 - Fail에서만 IFinResponseWithError 구현
        if (response is IFinResponseWithError { } failResponse)
            return $"Fail: {failResponse.Error}";

        return "Fail: unknown error";
    }
}
