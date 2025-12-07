using Serilog.Core;
using Serilog.Events;

namespace Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ExceptionalDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is Exceptional;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        Exceptional e = (Exceptional)error;

        List<LogEventProperty> props =
        [
            new(ErrorCodeFieldNames.ErrorType, new ScalarValue(e.GetType().Name)),
            // ErrorCode
            new(ErrorCodeFieldNames.ErrorCodeId, new ScalarValue(e.Code))
            // ErrorCurrentValue
            // Message
        ];

        // ExceptionDetails
        e.Exception.IfSome(ex =>
        {
            props.Add(new(ErrorCodeFieldNames.ExceptionDetails, factory.CreatePropertyValue(ex, true)));
        });

        return new StructureValue(props);
    }
}
