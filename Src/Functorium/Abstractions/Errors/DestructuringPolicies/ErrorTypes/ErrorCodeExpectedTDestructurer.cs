using System.Reflection;

using Serilog.Core;
using Serilog.Events;

namespace Functorium.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ErrorCodeExpectedTDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error)
    {
        Type type = error.GetType();
        return type.IsGenericType && type.Name.StartsWith(nameof(ErrorCodeExpected));
    }

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        Type type = error.GetType();
        List<LogEventProperty> props =
        [
            new(ErrorCodeFieldNames.ErrorType, new ScalarValue(type.Name)),
            new(ErrorCodeFieldNames.ErrorCodeId, new ScalarValue(error.Code))
        ];

        string errorCode = type.GetProperty(ErrorCodeFieldNames.ErrorCode)?.GetValue(error)?.ToString()
                           ?? ErrorCodeFieldNames.UnknownErrorCode;

        string message = type.GetProperty(ErrorCodeFieldNames.Message)?.GetValue(error)?.ToString()
                         ?? ErrorCodeFieldNames.UnknownErrorMessage;

        props.Add(new(ErrorCodeFieldNames.ErrorCode, new ScalarValue(errorCode)));
        props.Add(new(ErrorCodeFieldNames.Message, new ScalarValue(message)));

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                 .Where(p => p.Name.StartsWith(ErrorCodeFieldNames.ErrorCurrentValue)))
        {
            object? value = prop.GetValue(error);
            if (value is not null)
                props.Add(new(prop.Name, factory.CreatePropertyValue(value, true)));
        }

        return new StructureValue(props);
    }
}
