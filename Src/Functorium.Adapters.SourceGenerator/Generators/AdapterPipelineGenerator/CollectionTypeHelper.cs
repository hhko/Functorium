namespace Functorium.Adapters.SourceGenerator.Generators.AdapterPipelineGenerator;

/// <summary>
/// 컬렉션 타입 여부를 확인하는 헬퍼 클래스
/// </summary>
public static class CollectionTypeHelper
{
    private static readonly string[] CollectionTypePatterns = [
        "System.Collections.Generic.List<",
        "System.Collections.Generic.IList<",
        "System.Collections.Generic.ICollection<",
        "System.Collections.Generic.IEnumerable<",
        "System.Collections.Generic.IReadOnlyList<",
        "System.Collections.Generic.IReadOnlyCollection<",
        "System.Collections.Generic.HashSet<",
        "System.Collections.Generic.Dictionary<",
        "System.Collections.Generic.IDictionary<",
        "System.Collections.Generic.IReadOnlyDictionary<",
        "System.Collections.Generic.Queue<",
        "System.Collections.Generic.Stack<",
        "global::System.Collections.Generic.List<",
        "global::System.Collections.Generic.IList<",
        "global::System.Collections.Generic.ICollection<",
        "global::System.Collections.Generic.IEnumerable<",
        "global::System.Collections.Generic.IReadOnlyList<",
        "global::System.Collections.Generic.IReadOnlyCollection<",
        "global::System.Collections.Generic.HashSet<",
        "global::System.Collections.Generic.Dictionary<",
        "global::System.Collections.Generic.IDictionary<",
        "global::System.Collections.Generic.IReadOnlyDictionary<",
        "global::System.Collections.Generic.Queue<",
        "global::System.Collections.Generic.Stack<",
    ];

    /// <summary>
    /// 타입이 Count 속성을 가진 컬렉션인지 확인합니다.
    /// 튜플 타입은 내부에 컬렉션이 있더라도 컬렉션으로 취급하지 않습니다.
    /// </summary>
    public static bool IsCollectionType(string typeFullName)
    {
        if (string.IsNullOrEmpty(typeFullName))
            return false;

        // 튜플 타입은 컬렉션으로 취급하지 않음
        // ValueTuple 또는 괄호로 시작하는 튜플 구문 (int Id, string Name)
        if (IsTupleType(typeFullName))
            return false;

        // 배열 타입 확인 (예: int[], string[])
        if (typeFullName.Contains("[]"))
            return true;

        // 컬렉션 타입 패턴 확인
        return CollectionTypePatterns.Any(pattern => typeFullName.Contains(pattern));
    }

    /// <summary>
    /// 타입이 튜플인지 확인합니다.
    /// </summary>
    public static bool IsTupleType(string typeFullName)
    {
        if (string.IsNullOrEmpty(typeFullName))
            return false;

        // C# 튜플 구문: (int Id, string Name)
        if (typeFullName.StartsWith("(") && typeFullName.EndsWith(")"))
            return true;

        // ValueTuple 타입
        if (typeFullName.Contains("System.ValueTuple") || typeFullName.Contains("global::System.ValueTuple"))
            return true;

        return false;
    }

    /// <summary>
    /// 컬렉션 타입에 대한 Count 접근 표현식을 생성합니다.
    /// 배열은 Length, 나머지는 Count를 사용합니다.
    /// </summary>
    /// <returns>Count 표현식. 컬렉션 타입이 아니거나 입력이 유효하지 않으면 null</returns>
    public static string? GetCountExpression(string variableName, string typeFullName)
    {
        if (string.IsNullOrEmpty(variableName) || string.IsNullOrEmpty(typeFullName))
            return null;

        if (!IsCollectionType(typeFullName))
            return null;

        // 배열은 Length 사용
        if (typeFullName.Contains("[]"))
            return $"{variableName}?.Length ?? 0";

        // 나머지 컬렉션은 Count 사용
        return $"{variableName}?.Count ?? 0";
    }

    /// <summary>
    /// Request 파라미터에 대한 필드 이름을 생성합니다.
    /// 예: "ms" -> "request.params.ms", "name" -> "request.params.name"
    /// 동적 필드는 request.params.{name} 형식으로 정적 필드와 구분됩니다.
    /// </summary>
    public static string GetRequestFieldName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return parameterName;

        // 소문자로 변환하여 snake_case + dot 형식 사용
        return $"request.params.{parameterName.ToLowerInvariant()}";
    }

    /// <summary>
    /// Request 파라미터에 대한 Count 필드 이름을 생성합니다.
    /// 예: "orders" -> "request.params.orders.count"
    /// </summary>
    /// <returns>Count 필드 이름. parameterName이 비어있으면 null</returns>
    public static string? GetRequestCountFieldName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return null;

        // 소문자로 변환하여 snake_case + dot 형식 사용
        return $"request.params.{parameterName.ToLowerInvariant()}.count";
    }

    /// <summary>
    /// Response 결과에 대한 필드 이름을 생성합니다.
    /// 반환값: "response.result"
    /// Usecase Pipeline의 @response.message와 구분되는 개별 반환값 필드입니다.
    /// </summary>
    public static string GetResponseFieldName()
    {
        return "response.result";
    }

    /// <summary>
    /// Response 결과에 대한 Count 필드 이름을 생성합니다.
    /// 반환값: "response.result.count"
    /// </summary>
    public static string GetResponseCountFieldName()
    {
        return "response.result.count";
    }
}
