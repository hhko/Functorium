using Functorium.Abstractions.Errors;
using Functorium.Applications.Usecases;

namespace ObservabilityHost.Usecases;

/// <summary>
/// Expected(비즈니스) 에러를 반환하는 Command.
/// Warning 레벨 로그를 검증합니다.
/// </summary>
public sealed class FailExpectedCommand
{
    public sealed record Request(string OrderId) : ICommandRequest<Response>;
    public sealed record Response(string OrderId);

    public sealed class Usecase : ICommandUsecase<Request, Response>
    {
        public ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
        {
            var error = ErrorFactory.CreateExpected(
                errorCode: "Order.NotFound",
                errorCurrentValue: request.OrderId,
                errorMessage: "주문을 찾을 수 없습니다");

            return ValueTask.FromResult(
                FinResponse.Fail<Response>(error));
        }
    }
}
