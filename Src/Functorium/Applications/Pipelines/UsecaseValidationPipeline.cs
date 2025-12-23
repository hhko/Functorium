using FluentValidation;
using Functorium.Abstractions.Errors;
using Functorium.Abstractions.Utilities;
using Functorium.Applications.Cqrs;

using Mediator;

namespace Functorium.Applications.Pipelines;

public sealed class UsecaseValidationPipeline<TRequest, TResponse>
    : UsecasePipelineBase<TRequest>
    , IPipelineBehavior<TRequest, TResponse>
        where TRequest : IMessage
        where TResponse : IFinResponse<IResponse>
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
            return FinResponse<IResponse>.CreateFail<TResponse>(Error.Many(errors));
        }

        return await next(request, cancellationToken);
    }

    internal static partial class ApplicationErrors
    {
        public static Error Validator(Dictionary<string, object> errorValue, string errorMessage) =>
            ErrorCodeFactory.Create(
                //errorCode: $"{nameof(ApplicationErrors)}.UsecaseValidationPipeline.{nameof(Validator)}",
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(UsecaseValidationPipeline<TRequest, TResponse>)}.{nameof(Validator)}",
                errorCurrentValue: errorValue,
                errorMessage: $"{errorMessage}");
    }
}
