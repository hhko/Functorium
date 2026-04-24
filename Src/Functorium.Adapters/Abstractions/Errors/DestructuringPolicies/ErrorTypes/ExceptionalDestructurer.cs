using Functorium.Abstractions.Errors;
using Serilog.Core;
using Serilog.Events;

namespace Functorium.Adapters.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ExceptionalDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is Exceptional;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        Exceptional e = (Exceptional)error;

        List<LogEventProperty> props =
        [
            new(ErrorLogFieldNames.Kind, new ScalarValue(e.GetType().Name)),
            // ErrorCode
            new(ErrorLogFieldNames.NumericCode, new ScalarValue(e.Code))
            // ErrorCurrentValue
            // Message
        ];

        // ExceptionDetails
        e.Exception.IfSome(ex =>
        {
            props.Add(new(ErrorLogFieldNames.ExceptionDetails, factory.CreatePropertyValue(ex, true)));
        });

        return new StructureValue(props);
    }
}
