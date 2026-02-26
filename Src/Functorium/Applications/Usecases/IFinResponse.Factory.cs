using System.Diagnostics.Contracts;
using LanguageExt.Common;

namespace Functorium.Applications.Usecases;

public static class FinResponse
{
    /// <summary>
    /// Construct a FinResponse in a Succ state
    /// </summary>
    [Pure]
    public static FinResponse<A> Succ<A>(A value) => new FinResponse<A>.Succ(value);

    [Pure]
    public static FinResponse<A> Succ<A>() where A : new() => new FinResponse<A>.Succ(new A());


    /// <summary>
    /// Construct a FinResponse in a Fail state
    /// </summary>
    [Pure]
    public static FinResponse<A> Fail<A>(Error error) => new FinResponse<A>.Fail(error);
}
