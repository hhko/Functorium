using Functorium.Abstractions.Errors;
using Serilog.Core;
using Serilog.Events;

namespace Functorium.Adapters.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ExceptionalErrorDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is ExceptionalError;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        ExceptionalError e = (ExceptionalError)error;

        List<LogEventProperty> props =
        [
            new(ErrorCodeFieldNames.ErrorType, new ScalarValue(e.GetType().Name)),
            new(ErrorCodeFieldNames.ErrorCode, new ScalarValue(e.ErrorCode)),
            new(ErrorCodeFieldNames.ErrorCodeId, new ScalarValue(e.Code)),
            // ErrorCurrentValue
            new(ErrorCodeFieldNames.Message, new ScalarValue(e.Message))
        ];

        e.Exception.IfSome(ex =>
        {
            props.Add(new(ErrorCodeFieldNames.ExceptionDetails, factory.CreatePropertyValue(ex, true)));
        });

        return new StructureValue(props);
    }
}
