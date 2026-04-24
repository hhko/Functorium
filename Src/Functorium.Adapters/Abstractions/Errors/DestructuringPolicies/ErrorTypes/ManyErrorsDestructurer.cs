using Functorium.Abstractions.Errors;
using Serilog.Core;
using Serilog.Events;

namespace Functorium.Adapters.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ManyErrorsDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error) =>
        error is ManyErrors;

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        ManyErrors e = (ManyErrors)error;

        LogEventPropertyValue[] nested = e.Errors
            .Map(inner => ErrorsDestructuringPolicy.DestructureError(inner, factory))
            .ToArray();

        return new StructureValue(
        [
            new LogEventProperty(ErrorLogFieldNames.Kind, new ScalarValue(e.GetType().Name)),
            // ErrorCode
            new LogEventProperty(ErrorLogFieldNames.NumericCode, new ScalarValue(e.Code)),
            // ErrorCurrentValue
            // Message

            new LogEventProperty(ErrorLogFieldNames.Count, new ScalarValue(e.Count)),
            new LogEventProperty(ErrorLogFieldNames.Errors, new SequenceValue(nested))
        ]);
    }
}
