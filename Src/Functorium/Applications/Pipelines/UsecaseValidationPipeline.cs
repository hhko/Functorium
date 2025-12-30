using FluentValidation;

using Functorium.Abstractions.Errors;
using Functorium.Abstractions.Utilities;
using Functorium.Applications.Cqrs;

using LanguageExt.Common;

using Mediator;

namespace Functorium.Applications.Pipelines;

/// <summary>
/// Result 패턴을 위한 검증 Pipeline.
/// 검증 실패 시 FinResponse.Fail로 변환합니다.
///
/// IFinResponseFactory{TSelf}의 static abstract 메서드를 활용하여 리플렉션 없이 타입 안전하게 구현.
/// </summary>
public sealed class UsecaseValidationPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponseFactory<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public UsecaseValidationPipeline(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.IsEmpty())
        {
            return await next(request, cancellationToken);
        }

        Error[] errors = _validators
            .Select(validator => validator.Validate(request))
            .SelectMany(validationResult => validationResult.Errors)
            .Where(validationFailure => validationFailure is not null)
            .Select(failure => ApplicationErrors.Validator(

                // FormattedMessagePlaceholderValues: Dictionary<string, object>
                //  - 예: .NotEmpty()
                //        - "PropertyName": "Input",
                //        - "PropertyValue": "",
                //        - "PropertyPath": "Input"
                //  - 예: .MinimumLength(3)
                //        - "PropertyName": "Input",
                //        - "PropertyValue": "",
                //        - "PropertyPath": "Input"
                //        - "MinLength": 3,
                //        - "MaxLength": -1,
                //        - "TotalLength": 0,
                failure.FormattedMessagePlaceholderValues,
                $"{failure.PropertyName}: {failure.ErrorMessage}"))
            .Distinct()
            .ToArray();

        if (errors.Length is not 0)
        {
            var error = errors.Length == 1 ? errors[0] : Error.Many(errors);
            return TResponse.CreateFail(error);
        }

        return await next(request, cancellationToken);
    }

    internal static partial class ApplicationErrors
    {
        public static Error Validator(Dictionary<string, object> errorValue, string errorMessage) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(UsecaseValidationPipeline<TRequest, TResponse>)}.{nameof(Validator)}",
                errorCurrentValue: errorValue,
                errorMessage: $"{errorMessage}");
    }
}
