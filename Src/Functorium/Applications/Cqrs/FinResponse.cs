namespace Functorium.Applications.Cqrs;

public class FinResponse<T> : IFinResponse<T>
    where T : IResponse
{
    private readonly Fin<T> _fin;

    public FinResponse(Fin<T> fin)
    {
        _fin = fin;
    }

    public bool IsSucc =>
        _fin.IsSucc;

    public T Value =>
        _fin.IsSucc switch
        {
            true => (T)_fin,
            false => throw new InvalidOperationException("Cannot access Value on failed result")
        };

    public Error Error =>
        _fin.IsSucc switch
        {
            true => throw new InvalidOperationException("Cannot access Error on success"),
            false => (Error)_fin
        };

    public static FinResponse<T> Fail(Error error) =>
        new(error);

    public static TResponse CreateFail<TResponse>(Error error)
    {
        if (!typeof(TResponse).IsGenericType)
        {
            throw new InvalidOperationException($"TResponse must be a generic type, but was {typeof(TResponse).Name}");
        }

        Type finType = typeof(TResponse).GetGenericArguments()[0];
        Type expectedType = typeof(IFinResponse<>).MakeGenericType(finType);
        if (!expectedType.IsAssignableFrom(typeof(TResponse)))
        {
            throw new InvalidOperationException($"TResponse must implement IFinResponse<{finType.Name}>, but was {typeof(TResponse).Name}");
        }

        System.Reflection.MethodInfo? failure = typeof(FinResponse<>).MakeGenericType(finType).GetMethod(nameof(Fail));
        if (failure == null)
        {
            throw new InvalidOperationException($"Fail method not found on FinResponse<{finType.Name}>");
        }
        return (TResponse)failure.Invoke(null, [error])!;

        //if (!typeof(TResponse).IsGenericType || !typeof(IFinResponse<>).IsAssignableFrom(typeof(TResponse).GetGenericTypeDefinition()))
        //{
        //    throw new InvalidOperationException($"TResponse must be IFinResponse<T> type, but was {typeof(TResponse).Name}");
        //}
    }
}
