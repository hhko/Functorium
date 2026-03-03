using LanguageExt.Common;

namespace FinResponseFactoryCrtp;

/// <summary>
/// IFinResponseFactory를 사용하는 Validation Pipeline 예제.
/// TResponse.CreateFail()로 리플렉션 없이 실패 응답을 생성합니다.
/// </summary>
public static class ValidationPipelineExample
{
    public static TResponse ValidateAndCreate<TResponse>(
        bool isValid,
        Func<TResponse> onSuccess,
        string errorMessage)
        where TResponse : IFinResponseFactory<TResponse>
    {
        if (!isValid)
        {
            // static abstract 호출 - 리플렉션 없음!
            return TResponse.CreateFail(Error.New(errorMessage));
        }
        return onSuccess();
    }
}
