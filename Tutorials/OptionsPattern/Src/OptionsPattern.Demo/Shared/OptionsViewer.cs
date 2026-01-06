using System.Reflection;

namespace OptionsPattern.Demo.Shared;

/// <summary>
/// Options 객체를 읽기 쉬운 형식으로 출력하는 헬퍼 클래스
/// </summary>
public static class OptionsViewer
{
    public static void PrintOptions<T>(T options, string title = "Options")
    {
        Console.WriteLine($"╔════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ {title.PadRight(86)} ║");
        Console.WriteLine($"╚════════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (options == null)
        {
            Console.WriteLine("  (null)");
            Console.WriteLine();
            return;
        }

        PrintProperties(options, "", 0);
        Console.WriteLine();
    }

    private static void PrintProperties(object obj, string prefix, int depth)
    {
        if (depth > 3) // 깊이 제한
        {
            Console.WriteLine($"{prefix}... (max depth reached)");
            return;
        }

        var properties = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj);
            var propName = prop.Name;
            var displayValue = FormatValue(value);

            Console.WriteLine($"{prefix}  {propName,-30} : {displayValue}");
        }
    }

    private static string FormatValue(object? value)
    {
        if (value == null)
            return "(null)";

        if (value is string str)
            return string.IsNullOrEmpty(str) ? "(empty)" : str;

        if (value is bool b)
            return b ? "True" : "False";

        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var items = enumerable.Cast<object>().Take(5).ToList();
            if (items.Count == 0)
                return "[]";
            
            var display = string.Join(", ", items.Select(FormatValue));
            return $"[{display}]";
        }

        return value.ToString() ?? "(null)";
    }

    public static void PrintComparison<T>(T options1, T options2, string label1 = "Before", string label2 = "After")
    {
        Console.WriteLine($"╔════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ Options Comparison".PadRight(90) + " ║");
        Console.WriteLine($"╚════════════════════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (options1 == null || options2 == null)
        {
            Console.WriteLine("  Cannot compare null options.");
            Console.WriteLine();
            return;
        }

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Console.WriteLine($"  {"Property",-30} : {label1,-20} → {label2}");
        Console.WriteLine($"  {"".PadRight(30, '-')} : {"".PadRight(20, '-')}   {"".PadRight(20, '-')}");

        foreach (var prop in properties)
        {
            var value1 = prop.GetValue(options1);
            var value2 = prop.GetValue(options2);
            var formatted1 = FormatValue(value1);
            var formatted2 = FormatValue(value2);

            var changed = !Equals(value1, value2);
            var marker = changed ? " ⚠️" : "";

            Console.WriteLine($"  {prop.Name,-30} : {formatted1,-20} → {formatted2}{marker}");
        }

        Console.WriteLine();
    }
}
