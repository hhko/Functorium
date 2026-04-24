using Functorium.Abstractions.Errors;
using Functorium.Applications.Usecases;

namespace ObservabilityHost.Usecases;

/// <summary>
/// Exceptional(시스템) 에러를 반환하는 Command.
/// Error 레벨 로그를 검증합니다.
/// </summary>
public sealed class FailExceptionalCommand
{
    public sealed record Request(string OrderId) : ICommandRequest<Response>;
    public sealed record Response(string OrderId);

    public sealed class Usecase : ICommandUsecase<Request, Response>
    {
        public ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken ct)
        {
            var exception = new InvalidOperationException("데이터베이스 연결이 끊어졌습니다");
            var error = ErrorFactory.CreateExceptional(
                errorCode: "Database.ConnectionFailed",
                exception: exception);

            return ValueTask.FromResult(
                FinResponse.Fail<Response>(error));
        }
    }
}
