namespace Functorium.Applications.Errors;

public abstract partial record ApplicationErrorKind
{
    /// <summary>
    /// 값이 비어있음 (null, empty string, empty collection 등)
    /// </summary>
    public sealed record Empty : ApplicationErrorKind;

    /// <summary>
    /// 값이 null임
    /// </summary>
    public sealed record Null : ApplicationErrorKind;
}
