namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType
{
    /// <summary>
    /// 값이 최소 길이보다 짧음
    /// </summary>
    /// <param name="MinLength">요구되는 최소 길이 (0이면 미지정)</param>
    public sealed record TooShort(int MinLength = 0) : DomainErrorType;

    /// <summary>
    /// 값이 최대 길이를 초과함
    /// </summary>
    /// <param name="MaxLength">허용되는 최대 길이 (int.MaxValue면 미지정)</param>
    public sealed record TooLong(int MaxLength = int.MaxValue) : DomainErrorType;

    /// <summary>
    /// 값의 길이가 기대와 불일치 (R6: 두 값 불일치)
    /// </summary>
    /// <param name="Expected">기대되는 길이 (0이면 미지정)</param>
    public sealed record WrongLength(int Expected = 0) : DomainErrorType;
}
