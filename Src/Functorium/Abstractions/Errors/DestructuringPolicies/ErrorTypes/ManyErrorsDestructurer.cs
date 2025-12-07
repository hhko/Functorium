using Serilog.Core;
using Serilog.Events;

namespace Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ManyErrorsDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is ManyErrors;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        ManyErrors e = (ManyErrors)error;

        LogEventPropertyValue[] nested = e.Errors.Map(inner => ErrorsDestructuringPolicy.DestructureError(inner, factory))
                             .ToArray();

        return new StructureValue(
        [
            new LogEventProperty(ErrorCodeFieldNames.ErrorType, new ScalarValue(e.GetType().Name)),
            // ErrorCode
            new LogEventProperty(ErrorCodeFieldNames.ErrorCodeId, new ScalarValue(e.Code)),
            // ErrorCurrentValue
            // Message

            new LogEventProperty(ErrorCodeFieldNames.Count, new ScalarValue(e.Count)),
            new LogEventProperty(ErrorCodeFieldNames.Errors, new SequenceValue(nested))
        ]);
    }
}
