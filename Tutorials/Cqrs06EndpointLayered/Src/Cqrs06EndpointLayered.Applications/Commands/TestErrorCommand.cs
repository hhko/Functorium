using FluentValidation;
using Functorium.Abstractions.Errors;
using Microsoft.Extensions.Logging;
using static LanguageExt.Prelude;

namespace Cqrs06EndpointLayered.Applications.Commands;

/// <summary>
/// 에러 처리 테스트 Command - UsecaseMetricsPipeline의 에러 태그 기능 검증
/// ErrorCodeExpected, ErrorCodeExceptional, ManyErrors 시나리오 테스트
/// </summary>
public sealed class TestErrorCommand
{
    /// <summary>
    /// 에러 시나리오 타입
    /// </summary>
    public enum ErrorScenario
    {
        /// <summary>성공 케이스 (에러 없음)</summary>
        Success,

        /// <summary>단일 Expected 에러 (비즈니스 에러)</summary>
        SingleExpected,

        /// <summary>단일 Exceptional 에러 (시스템 에러)</summary>
        SingleExceptional,

        /// <summary>복합 에러 (ManyErrors - Expected 2개)</summary>
        ManyExpected,

        /// <summary>복합 에러 (ManyErrors - Expected + Exceptional)</summary>
        ManyMixed,

        /// <summary>제네릭 Expected 에러 (ErrorCodeExpected&lt;T&gt;)</summary>
        GenericExpected
    }

    /// <summary>
    /// Command Request - 테스트할 에러 시나리오 지정
    /// </summary>
    public sealed record Request(
        ErrorScenario Scenario,
        string TestMessage) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 테스트 실행 결과
    /// </summary>
    public sealed record Response(
        ErrorScenario Scenario,
        string Message,
        DateTime ExecutedAt);

    /// <summary>
    /// Request Validator - 입력 검증
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.Scenario)
                .IsInEnum().WithMessage("유효한 에러 시나리오를 선택해야 합니다");

            RuleFor(x => x.TestMessage)
                .NotEmpty().WithMessage("테스트 메시지는 필수입니다")
                .MaximumLength(200).WithMessage("테스트 메시지는 200자를 초과할 수 없습니다");
        }
    }

    /// <summary>
    /// Command Handler - 각 에러 시나리오별 처리
    /// </summary>
    internal sealed class Usecase(ILogger<Usecase> logger) : ICommandUsecase<Request, Response>
    {
        private readonly ILogger<Usecase> _logger = logger;

        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Testing error scenario: {Scenario}", request.Scenario);

            return request.Scenario switch
            {
                ErrorScenario.Success => HandleSuccess(request),
                ErrorScenario.SingleExpected => HandleSingleExpected(request),
                ErrorScenario.SingleExceptional => HandleSingleExceptional(request),
                ErrorScenario.ManyExpected => HandleManyExpected(request),
                ErrorScenario.ManyMixed => HandleManyMixed(request),
                ErrorScenario.GenericExpected => HandleGenericExpected(request),
                _ => Fin.Fail<Response>(TestErrors.UnsupportedScenario(request.Scenario)).ToFinResponse()
            };
        }

        /// <summary>
        /// 성공 케이스 - 에러 없음
        /// 메트릭: response.status=success (에러 태그 없음)
        /// </summary>
        private FinResponse<Response> HandleSuccess(Request request)
        {
            var response = new Response(
                request.Scenario,
                $"Success: {request.TestMessage}",
                DateTime.UtcNow);

            return Fin.Succ(response).ToFinResponse();
        }

        /// <summary>
        /// 단일 Expected 에러 - 비즈니스 규칙 위반
        /// 메트릭: response.status=failure, error.type=expected, error.code=TestErrors.TestErrorCommand.BusinessRuleViolation
        /// </summary>
        private FinResponse<Response> HandleSingleExpected(Request request)
        {
            return Fin.Fail<Response>(TestErrors.BusinessRuleViolation(request.TestMessage)).ToFinResponse();
        }

        /// <summary>
        /// 단일 Exceptional 에러 - 시스템 에러
        /// 메트릭: response.status=failure, error.type=exceptional, error.code=TestErrors.TestErrorCommand.SystemFailure
        /// </summary>
        private FinResponse<Response> HandleSingleExceptional(Request request)
        {
            var exception = new InvalidOperationException($"System failure: {request.TestMessage}");
            return Fin.Fail<Response>(TestErrors.SystemFailure(exception)).ToFinResponse();
        }

        /// <summary>
        /// 복합 Expected 에러 - 여러 비즈니스 규칙 위반
        /// 메트릭: response.status=failure, error.type=aggregate, error.code=TestErrors.TestErrorCommand.BusinessRuleViolation (첫 번째)
        /// </summary>
        private FinResponse<Response> HandleManyExpected(Request request)
        {
            var errors = Seq(
                TestErrors.BusinessRuleViolation($"{request.TestMessage} - Error 1"),
                TestErrors.ValidationFailed($"{request.TestMessage} - Error 2")
            );

            return Fin.Fail<Response>(Error.Many(errors)).ToFinResponse();
        }

        /// <summary>
        /// 복합 Mixed 에러 - Expected + Exceptional
        /// 메트릭: response.status=failure, error.type=aggregate, error.code=TestErrors.TestErrorCommand.SystemFailure (Exceptional 우선)
        /// </summary>
        private FinResponse<Response> HandleManyMixed(Request request)
        {
            var exception = new InvalidOperationException($"System failure: {request.TestMessage}");

            var errors = Seq(
                TestErrors.BusinessRuleViolation($"{request.TestMessage} - Business Error"),
                TestErrors.SystemFailure(exception)
            );

            return Fin.Fail<Response>(Error.Many(errors)).ToFinResponse();
        }

        /// <summary>
        /// 제네릭 Expected 에러 - ErrorCodeExpected&lt;T&gt; 테스트
        /// 메트릭: response.status=failure, error.type=expected, error.code=TestErrors.TestErrorCommand.GenericError
        /// </summary>
        private FinResponse<Response> HandleGenericExpected(Request request)
        {
            return Fin.Fail<Response>(
                TestErrors.GenericError(request.TestMessage, 42)
            ).ToFinResponse();
        }
    }

    /// <summary>
    /// TestErrors - 테스트용 에러 정의
    /// </summary>
    internal static class TestErrors
    {
        private static string ErrorCodePrefix => $"{nameof(TestErrors)}.{nameof(TestErrorCommand)}";

        /// <summary>
        /// 비즈니스 규칙 위반 (Expected Error)
        /// </summary>
        public static Error BusinessRuleViolation(string message) =>
            ErrorCodeFactory.Create(
                errorCode: $"{ErrorCodePrefix}.{nameof(BusinessRuleViolation)}",
                errorCurrentValue: message,
                errorMessage: $"Business rule violated: {message}");

        /// <summary>
        /// 검증 실패 (Expected Error)
        /// </summary>
        public static Error ValidationFailed(string message) =>
            ErrorCodeFactory.Create(
                errorCode: $"{ErrorCodePrefix}.{nameof(ValidationFailed)}",
                errorCurrentValue: message,
                errorMessage: $"Validation failed: {message}");

        /// <summary>
        /// 시스템 에러 (Exceptional Error)
        /// </summary>
        public static Error SystemFailure(Exception exception) =>
            ErrorCodeFactory.CreateFromException(
                errorCode: $"{ErrorCodePrefix}.{nameof(SystemFailure)}",
                exception: exception);

        /// <summary>
        /// 지원되지 않는 시나리오 (Expected Error)
        /// </summary>
        public static Error UnsupportedScenario(ErrorScenario scenario) =>
            ErrorCodeFactory.Create(
                errorCode: $"{ErrorCodePrefix}.{nameof(UnsupportedScenario)}",
                errorCurrentValue: scenario.ToString(),
                errorMessage: $"Unsupported scenario: {scenario}");

        /// <summary>
        /// 제네릭 에러 (Expected Error with Generic Type)
        /// </summary>
        public static Error GenericError(string message, int errorCode) =>
            ErrorCodeFactory.Create(
                errorCode: $"{ErrorCodePrefix}.{nameof(GenericError)}",
                errorCurrentValue: (message, errorCode),
                errorMessage: $"Generic error occurred: {message} (code: {errorCode})");
    }
}
