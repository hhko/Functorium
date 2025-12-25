using LanguageExt.Common;

namespace Functorium.Applications.Cqrs;

public interface IResponse<T> : IResponse where T : IResponse<T>
{
    static abstract T CreateFail(Error error);
}
