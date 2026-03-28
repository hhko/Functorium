namespace FinResponseMarker;

/// <summary>
/// IFinResponse 마커를 사용하는 Pipeline 예제.
/// 리플렉션 없이 성공/실패를 확인할 수 있습니다.
/// </summary>
public static class PipelineExample
{
    public static string LogResponse<TResponse>(TResponse response)
        where TResponse : IFinResponse
    {
        // 리플렉션 없이 직접 접근!
        return response.IsSucc ? "Success" : "Fail";
    }
}
