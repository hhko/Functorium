using Functorium.Abstractions.Errors;
using Serilog.Core;
using Serilog.Events;

namespace Functorium.Adapters.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ExpectedDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is Expected;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        Expected e = (Expected)error;
        List<LogEventProperty> props =
        [
            new(ErrorLogFieldNames.Kind, new ScalarValue(e.GetType().Name)),
            // ErrorCode
            new(ErrorLogFieldNames.NumericCode, new ScalarValue(e.Code)),
            // ErrorCurrentValue
            new(ErrorLogFieldNames.Message, new ScalarValue(e.Message))
        ];

        // InnerError
        e.Inner.IfSome(inner =>
        {
            props.Add(new(ErrorLogFieldNames.InnerError, ErrorsDestructuringPolicy.DestructureError(inner, factory)));
        });

        return new StructureValue(props);
    }
}
