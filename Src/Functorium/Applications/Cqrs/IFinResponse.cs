namespace Functorium.Applications.Cqrs;

public interface IFinResponse
{
    bool IsSucc { get; }

    Error Error { get; }
}

public interface IFinResponse<out T> : IFinResponse
    where T : IResponse
{
    T Value { get; }
}
