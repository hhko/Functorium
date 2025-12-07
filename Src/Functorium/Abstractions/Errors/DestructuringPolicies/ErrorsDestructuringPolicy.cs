using System.Diagnostics.CodeAnalysis;

using Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

using Serilog.Core;
using Serilog.Events;

namespace Functorium.Abstractions.Errors.DestructuringPolicies;

public class ErrorsDestructuringPolicy : IDestructuringPolicy
{
    private static readonly List<IErrorDestructurer> Destructurers =
    [
        // ErrorCode
        new ErrorCodeExpectedDestructurer(),
        new ErrorCodeExceptionalDestructurer(),
        new ErrorCodeExpectedTDestructurer(),

        // ManyErrors
        new ManyErrorsDestructurer(),

        // Error
        new ExpectedDestructurer(),
        new ExceptionalDestructurer(),
    ];

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result)
    {
        if (value is not Error error)
        {
            result = null;
            return false;
        }

        IErrorDestructurer? destructurer = Destructurers.FirstOrDefault(d => d.CanHandle(error));
        if (destructurer is not null)
        {
            result = destructurer.Destructure(error, propertyValueFactory);
            return true;
        }

        result = null;
        return false;
    }

    // For recursive destructuring
    public static LogEventPropertyValue DestructureError(Error error, ILogEventPropertyValueFactory propertyValueFactory)
    {
        return Destructurers
            .First(d => d.CanHandle(error))
            .Destructure(error, propertyValueFactory);
    }
}
