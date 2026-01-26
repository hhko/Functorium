namespace Functorium.Domains.Errors;

public abstract partial record DomainErrorType
{
    /// <summary>
    /// 도메인 특화 커스텀 에러 (표준 에러에 해당하지 않는 경우)
    /// </summary>
    /// <param name="Name">커스텀 에러 이름</param>
    /// <remarks>
    /// 표준 에러로 표현할 수 없는 도메인 특화 에러에 사용합니다.
    /// 예: new Custom("Unsupported"), new Custom("AlreadyShipped"), new Custom("NotVerified")
    /// </remarks>
    public sealed record Custom(string Name) : DomainErrorType
    {
        public override string ErrorName => Name;
    }
}
