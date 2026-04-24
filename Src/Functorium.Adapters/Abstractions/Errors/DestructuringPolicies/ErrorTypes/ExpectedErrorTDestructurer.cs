using System.Reflection;
using Functorium.Abstractions.Errors;
using Serilog.Core;
using Serilog.Events;

namespace Functorium.Adapters.Abstractions.Errors.DestructuringPolicies.ErrorTypes;

public class ExpectedErrorTDestructurer : IErrorDestructurer
{
    public bool CanHandle(Error error)
    {
        Type type = error.GetType();
        return type.IsGenericType && type.Name.StartsWith(nameof(ExpectedError));
    }

    public LogEventPropertyValue Destructure(Error error, ILogEventPropertyValueFactory factory)
    {
        Type type = error.GetType();
        List<LogEventProperty> props =
        [
            new(ErrorLogFieldNames.Kind, new ScalarValue(type.Name)),
            new(ErrorLogFieldNames.NumericCode, new ScalarValue(error.Code))
        ];

        string errorCode = type.GetProperty(ErrorLogFieldNames.ErrorCode)?.GetValue(error)?.ToString()
                           ?? ErrorLogFieldNames.UnknownErrorCode;

        string message = type.GetProperty(ErrorLogFieldNames.Message)?.GetValue(error)?.ToString()
                         ?? ErrorLogFieldNames.UnknownErrorMessage;

        props.Add(new(ErrorLogFieldNames.ErrorCode, new ScalarValue(errorCode)));
        props.Add(new(ErrorLogFieldNames.Message, new ScalarValue(message)));

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                 .Where(p => p.Name.StartsWith(ErrorLogFieldNames.ErrorCurrentValue)))
        {
            object? value = prop.GetValue(error);
            if (value is not null)
                props.Add(new(prop.Name, factory.CreatePropertyValue(value, true)));
        }

        return new StructureValue(props);
    }
}
