namespace FinResponseCovariant;

public interface IFinResponse
{
    bool IsSucc { get; }
    bool IsFail { get; }
}

/// <summary>
/// 공변 인터페이스: out A로 선언하여 파생->기본 대입 가능
/// </summary>
public interface IFinResponse<out A> : IFinResponse
{
}
