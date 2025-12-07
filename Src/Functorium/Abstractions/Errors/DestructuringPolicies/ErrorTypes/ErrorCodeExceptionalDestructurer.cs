using Serilog.Core;
using Serilog.Events;

namespace Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ErrorCodeExceptionalDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is ErrorCodeExceptional;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        ErrorCodeExceptional e = (ErrorCodeExceptional)error;

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
