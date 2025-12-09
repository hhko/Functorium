namespace Functorium.Testing.Assertions.Logging;

/// <summary>
/// Serilog의 ILogEventPropertyValueFactory를 구현하는 테스트용 팩토리 클래스
/// </summary>
public sealed class SerilogTestPropertyValueFactory : Serilog.Core.ILogEventPropertyValueFactory
{
    /// <summary>
    /// 객체를 LogEventPropertyValue로 변환합니다.
    /// </summary>
    public Serilog.Events.LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false)
    {
        return value switch
        {
            null => new Serilog.Events.ScalarValue(null),
            string s => new Serilog.Events.ScalarValue(s),
            int i => new Serilog.Events.ScalarValue(i),
            long l => new Serilog.Events.ScalarValue(l),
            double d => new Serilog.Events.ScalarValue(d),
            bool b => new Serilog.Events.ScalarValue(b),
            Exception ex => CreateExceptionValue(ex),
            _ when IsTupleType(value.GetType()) => CreateTupleValue(value),
            _ when destructureObjects => CreateObjectValue(value),
            _ => throw new ArgumentException(
                $"Unsupported type for test property value: {value.GetType().Name}. " +
                $"Supported types: null, string, int, long, double, bool, Exception, ValueTuple, " +
                $"or any type with destructureObjects=true.",
                nameof(value))
        };
    }

    /// <summary>
    /// 타입이 ValueTuple인지 확인합니다.
    /// </summary>
    private static bool IsTupleType(Type type)
    {
        return type.IsGenericType &&
               type.FullName?.StartsWith("System.ValueTuple") == true;
    }

    /// <summary>
    /// ValueTuple을 StructureValue로 변환합니다.
    /// </summary>
    private Serilog.Events.LogEventPropertyValue CreateTupleValue(object tuple)
    {
        var type = tuple.GetType();
        var fields = type.GetFields();
        var props = new List<Serilog.Events.LogEventProperty>();

        foreach (var field in fields)
        {
            var fieldValue = field.GetValue(tuple);
            props.Add(new Serilog.Events.LogEventProperty(
                field.Name,
                CreatePropertyValue(fieldValue, true)));
        }

        return new Serilog.Events.StructureValue(props, type.Name);
    }

    /// <summary>
    /// 일반 객체를 StructureValue로 변환합니다.
    /// </summary>
    private Serilog.Events.LogEventPropertyValue CreateObjectValue(object obj)
    {
        var type = obj.GetType();
        var properties = type.GetProperties();
        var props = new List<Serilog.Events.LogEventProperty>();

        foreach (var prop in properties)
        {
            var propValue = prop.GetValue(obj);
            props.Add(new Serilog.Events.LogEventProperty(
                prop.Name,
                CreatePropertyValue(propValue, true)));
        }

        return new Serilog.Events.StructureValue(props, type.Name);
    }

    /// <summary>
    /// Exception을 StructureValue로 변환합니다.
    /// </summary>
    private static Serilog.Events.StructureValue CreateExceptionValue(Exception ex)
    {
        var props = new List<Serilog.Events.LogEventProperty>
        {
            new("Type", new Serilog.Events.ScalarValue(ex.GetType().Name)),
            new("Message", new Serilog.Events.ScalarValue(ex.Message)),
            new("HResult", new Serilog.Events.ScalarValue(ex.HResult))
        };

        if (ex.StackTrace != null)
            props.Add(new("StackTrace", new Serilog.Events.ScalarValue(ex.StackTrace)));

        if (ex.InnerException != null)
            props.Add(new("InnerException", CreateExceptionValue(ex.InnerException)));

        return new Serilog.Events.StructureValue(props, ex.GetType().Name);
    }
}
