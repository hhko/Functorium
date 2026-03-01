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
    public sealed class Handler
    {
        public FinResponse<Response> Handle(Request request)
        {
            // Validation
            var validated = Validator.Validate(request);
            if (validated.IsFail)
                return validated.Match<FinResponse<Response>>(_ => throw new InvalidOperationException(), FinResponse.Fail<Response>);

            // Business logic - create product
            var productId = Guid.NewGuid().ToString("N")[..8];
            return new Response(productId, request.Name, request.Price);
        }
    }
}
