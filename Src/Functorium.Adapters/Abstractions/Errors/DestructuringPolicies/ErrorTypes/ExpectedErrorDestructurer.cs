using Functorium.Abstractions.Errors;
using Serilog.Core;
using Serilog.Events;

namespace Functorium.Adapters.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ExpectedErrorDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is ExpectedError;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        ExpectedError e = (ExpectedError)error;

        List<LogEventProperty> props =
        [
            new(ErrorLogFieldNames.Kind, new ScalarValue(e.GetType().Name)),
            new(ErrorLogFieldNames.ErrorCode, new ScalarValue(e.ErrorCode)),
            new(ErrorLogFieldNames.NumericCode, new ScalarValue(e.Code)),
            new(ErrorLogFieldNames.ErrorCurrentValue, new ScalarValue(e.ErrorCurrentValue)),
            new(ErrorLogFieldNames.Message, new ScalarValue(e.Message))
        ];

        return new StructureValue(props);
    }
}
