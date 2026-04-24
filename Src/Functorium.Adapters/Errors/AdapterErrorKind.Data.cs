namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorKind
{
    /// <summary>
    /// 직렬화 실패
    /// </summary>
    /// <param name="Format">직렬화 형식 (선택적)</param>
    public sealed record Serialization(string? Format = null) : AdapterErrorKind;

    /// <summary>
    /// 역직렬화 실패
    /// </summary>
    /// <param name="Format">역직렬화 형식 (선택적)</param>
    public sealed record Deserialization(string? Format = null) : AdapterErrorKind;

    /// <summary>
    /// 데이터 손상
    /// </summary>
    public sealed record DataCorruption : AdapterErrorKind;
}
