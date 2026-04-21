namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorType
{
    /// <summary>
    /// 낙관적 동시성 충돌.
    /// Update/UpdateRange 호출 시 로드 이후 DB의 동일 엔티티가
    /// 다른 주체에 의해 변경되었을 때 반환됩니다.
    /// 재시도 가능성 판단은 호출자 책임입니다.
    /// </summary>
    public sealed record ConcurrencyConflict : AdapterErrorType;
}
