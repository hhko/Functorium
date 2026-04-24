namespace Functorium.Adapters.Errors;

public abstract partial record AdapterErrorKind
{
    /// <summary>
    /// 값이 비어있음 (null, empty string, empty collection 등)
    /// </summary>
    public sealed record Empty : AdapterErrorKind;

    /// <summary>
    /// 값이 null임
    /// </summary>
    public sealed record Null : AdapterErrorKind;
}
