using Serilog.Core;
using Serilog.Events;

namespace Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ExpectedDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is Expected;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        Expected e = (Expected)error;
        List<LogEventProperty> props =
        [
            new(ErrorCodeFieldNames.ErrorType, new ScalarValue(e.GetType().Name)),
            // ErrorCode
            new(ErrorCodeFieldNames.ErrorCodeId, new ScalarValue(e.Code)),
            // ErrorCurrentValue
            new(ErrorCodeFieldNames.Message, new ScalarValue(e.Message))
        ];

        // InnerError
        e.Inner.IfSome(inner =>
        {
            props.Add(new(ErrorCodeFieldNames.InnerError, ErrorsDestructuringPolicy.DestructureError(inner, factory)));
        });

        return new StructureValue(props);
    }
}
