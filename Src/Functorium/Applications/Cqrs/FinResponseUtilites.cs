namespace Functorium.Applications.Cqrs;

public static class FinResponseUtilites
{
    public static IFinResponse<TTo> ToResponse<TFrom, TTo>(this Fin<TFrom> result, Func<TFrom, TTo> mapper)
        where TFrom : notnull
        where TTo : IResponse
    {
        return result.Match<IFinResponse<TTo>>(
            Succ: value => new FinResponse<TTo>(Fin.Succ(mapper(value))),
            Fail: error => new FinResponse<TTo>(Fin.Fail<TTo>(error))
        );
    }

    public static IFinResponse<T> ToResponse<T>() where T : IResponse, new()
    {
        return new FinResponse<T>(Fin.Succ(new T()));
    }

    public static IFinResponse<T> ToResponse<T>(T t) where T : IResponse
    {
        return new FinResponse<T>(Fin.Succ(t));
    }

    public static IFinResponse<T> ToResponseFail<T>(Error error) where T : IResponse
    {
        return new FinResponse<T>(Fin.Fail<T>(error));
    }
}
