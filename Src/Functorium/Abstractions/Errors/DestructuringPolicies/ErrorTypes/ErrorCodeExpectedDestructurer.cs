using Serilog.Core;
using Serilog.Events;

namespace Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ErrorCodeExpectedDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is ErrorCodeExpected;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        ErrorCodeExpected e = (ErrorCodeExpected)error;

        List<LogEventProperty> props =
        [
            new(ErrorCodeFieldNames.ErrorType, new ScalarValue(e.GetType().Name)),
            new(ErrorCodeFieldNames.ErrorCode, new ScalarValue(e.ErrorCode)),
            new(ErrorCodeFieldNames.ErrorCodeId, new ScalarValue(e.Code)),
            new(ErrorCodeFieldNames.ErrorCurrentValue, new ScalarValue(e.ErrorCurrentValue)),
            new(ErrorCodeFieldNames.Message, new ScalarValue(e.Message))
        ];

        return new StructureValue(props);
    }
}
