using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace CreateOnlyConstraint;

/// <summary>
/// Create-Only 제약: IFinResponseFactory<TResponse>만 요구합니다.
/// Validation Pipeline은 응답을 읽지 않고, 실패 시 생성만 합니다.
/// </summary>
public sealed class SimpleValidationPipeline<TResponse>
    where TResponse : IFinResponseFactory<TResponse>
{
    public TResponse Validate(bool isValid, Func<TResponse> onSuccess)
    {
        if (!isValid)
        {
            // static abstract 호출 - 리플렉션 없음
            return TResponse.CreateFail(Error.New("Validation failed"));
        }
        return onSuccess();
    }
}

/// <summary>
/// Create-Only 제약: Exception Pipeline
/// </summary>
public sealed class SimpleExceptionPipeline<TResponse>
    where TResponse : IFinResponseFactory<TResponse>
{
    public TResponse Execute(Func<TResponse> action)
    {
        try
        {
            return action();
        }
        catch (Exception ex)
        {
            return TResponse.CreateFail(Error.New(ex));
        }
    }
}
