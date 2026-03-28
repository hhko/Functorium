using FluentValidation;
using LanguageExt.Common;
using Mediator;
using ResultPattern.Demo.Cqrs;

namespace ResultPattern.Demo.Pipelines;

/// <summary>
/// Result 패턴을 위한 검증 Pipeline.
/// 검증 실패 시 Result.Fail로 변환합니다.
///
/// IResultFactory{TSelf}의 static abstract 메서드를 활용하여 리플렉션 없이 타입 안전하게 구현.
/// </summary>
public sealed class UsecaseValidationPipeline<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMessage
    where TResponse : IFinResponseFactory<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public UsecaseValidationPipeline(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async ValueTask<TResponse> Handle(
        TRequest request,
        MessageHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"    [ValidationPipeline] Enter: {typeof(TRequest).Name} (Validators: {_validators.Count()})");

        if (!_validators.Any())
        {
            Console.WriteLine($"    [ValidationPipeline] Exit: No validators, passing through");
            return await next(request, cancellationToken);
        }

        var errors = _validators
            .Select(v => v.Validate(request))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.New($"{f.PropertyName}: {f.ErrorMessage}"))
            .ToArray();

        if (errors.Length > 0)
        {
            Console.WriteLine($"    [ValidationPipeline] Exit: Validation failed ({errors.Length} errors)");
            var error = errors.Length == 1 ? errors[0] : Error.Many(errors);
            // IFinResponseFactory<TResponse>.CreateFail을 통해 타입 안전하게 Fail 생성
            return TResponse.CreateFail(error);
        }

        Console.WriteLine($"    [ValidationPipeline] Exit: Validation passed");
        return await next(request, cancellationToken);
    }
}
