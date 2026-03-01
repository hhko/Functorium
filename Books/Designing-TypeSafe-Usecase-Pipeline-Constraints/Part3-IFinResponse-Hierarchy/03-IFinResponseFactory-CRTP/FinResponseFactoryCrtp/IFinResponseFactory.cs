using LanguageExt.Common;

namespace FinResponseFactoryCrtp;

/// <summary>
/// CRTP 팩토리 인터페이스.
/// static abstract를 사용하여 컴파일 타임에 CreateFail 호출을 보장합니다.
/// </summary>
public interface IFinResponseFactory<TSelf>
    where TSelf : IFinResponseFactory<TSelf>
{
    static abstract TSelf CreateFail(Error error);
}
