using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace CommandUsecaseExample;

/// <summary>
/// Command Usecase 예제 - Nested class 패턴
/// </summary>
public sealed class CreateProductCommand
{
    /// <summary>
    /// Command Request
    /// </summary>
    public sealed record Request(
        string Name,
        decimal Price) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response
    /// </summary>
    public sealed record Response(
        string ProductId,
        string Name,
        decimal Price);

    /// <summary>
    /// 간단한 유효성 검사
    /// </summary>
    public static class Validator
    {
        public static FinResponse<Request> Validate(Request request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Error.New("Name is required");

            if (request.Price <= 0)
                return Error.New("Price must be positive");

            return request;
        }
    }

    /// <summary>
    /// Command Handler
    /// </summary>
    public sealed class Handler : ICommandUsecase<Request, Response>
    {
        public ValueTask<FinResponse<Response>> Handle(Request command, CancellationToken cancellationToken)
        {
            var result = Validator.Validate(command)
                .Bind(req =>
                {
                    var productId = Guid.NewGuid().ToString("N")[..8];
                    return FinResponse.Succ(new Response(productId, req.Name, req.Price));
                });

            return new ValueTask<FinResponse<Response>>(result);
        }
    }
}
