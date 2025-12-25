using LanguageExt.Common;

namespace Functorium.Applications.Cqrs;

public interface IResponse
{
    bool IsSuccess { get; }
    Error? Error { get; }
}
