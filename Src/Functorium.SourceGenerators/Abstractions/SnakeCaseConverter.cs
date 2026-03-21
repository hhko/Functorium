using System.Text;

namespace Functorium.SourceGenerators.Abstractions;

/// <summary>
/// PascalCase를 snake_case로 변환합니다.
/// OpenSearchJsonFormatter.ToSnakeCase와 동일 로직 (netstandard2.0 호환).
/// </summary>
public static class SnakeCaseConverter
{
    /// <summary>
    /// PascalCase를 snake_case로 변환합니다.
    /// 예: "CustomerId" → "customer_id", "OrderLineCount" → "order_line_count"
    /// </summary>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length + 4);
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (char.IsUpper(c))
            {
                if (i > 0)
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
