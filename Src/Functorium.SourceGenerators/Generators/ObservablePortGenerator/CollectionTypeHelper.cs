using Functorium.SourceGenerators.Abstractions;

using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.ObservablePortGenerator;

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
        "LanguageExt.Seq<",
        "global::LanguageExt.Seq<",
    ];

    private const string ValueObjectInterfaceFullName = "Functorium.Domains.ValueObjects.IValueObject";
    private const string EntityIdInterfacePrefix = "Functorium.Domains.Entities.IEntityId<";

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

        // LanguageExt.Seq<T>는 struct이므로 null-conditional 불필요
        if (IsSeqType(typeFullName))
            return $"{variableName}.Count";

        // 나머지 컬렉션은 Count 사용
        return $"{variableName}?.Count ?? 0";
    }

    /// <summary>
    /// Request 파라미터에 대한 필드 이름을 생성합니다.
    /// 예: "customerId" -> "request.params.customer_id"
    /// </summary>
    public static string GetRequestFieldName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return parameterName;

        return $"request.params.{SnakeCaseConverter.ToSnakeCase(parameterName)}";
    }

    /// <summary>
    /// Request 파라미터에 대한 Count 필드 이름을 생성합니다.
    /// 예: "orderLines" -> "request.params.order_lines_count"
    /// </summary>
    /// <returns>Count 필드 이름. parameterName이 비어있으면 null</returns>
    public static string? GetRequestCountFieldName(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
            return null;

        return $"request.params.{SnakeCaseConverter.ToSnakeCase(parameterName)}_count";
    }

    /// <summary>
    /// Response 결과에 대한 필드 이름을 생성합니다.
    /// 반환값: "response.result"
    /// </summary>
    public static string GetResponseFieldName()
    {
        return "response.result";
    }

    /// <summary>
    /// Response 결과에 대한 Count 필드 이름을 생성합니다.
    /// 반환값: "response.result_count"
    /// </summary>
    public static string GetResponseCountFieldName()
    {
        return "response.result_count";
    }

    /// <summary>
    /// LanguageExt.Seq&lt;T&gt; 타입인지 확인합니다.
    /// Seq는 struct이므로 null-conditional 연산자를 사용할 수 없습니다.
    /// </summary>
    public static bool IsSeqType(string typeFullName)
    {
        if (string.IsNullOrEmpty(typeFullName))
            return false;

        return typeFullName.Contains("LanguageExt.Seq<")
            || typeFullName.Contains("global::LanguageExt.Seq<");
    }

    /// <summary>
    /// 타입이 IValueObject 또는 IEntityId&lt;T&gt;를 구현하는지 확인합니다.
    /// </summary>
    public static bool ImplementsValueObjectOrEntityId(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol namedType)
            return false;

        foreach (var iface in namedType.AllInterfaces)
        {
            string ifaceFullName = iface.ToDisplayString();
            if (ifaceFullName == ValueObjectInterfaceFullName)
                return true;
            if (ifaceFullName.StartsWith(EntityIdInterfacePrefix))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 타입이 복합 타입(class, record 등)인지 확인합니다.
    /// 스칼라, 열거형, 컬렉션, 널러블, Option 등은 복합 타입이 아닙니다.
    /// </summary>
    public static bool IsComplexType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.SpecialType != SpecialType.None)
            return false;

        if (typeSymbol.TypeKind == TypeKind.Enum)
            return false;

        string fullName = typeSymbol.ToDisplayString();

        if (fullName == "System.Guid" || fullName == "System.DateTime"
            || fullName == "System.DateTimeOffset" || fullName == "System.TimeSpan"
            || fullName == "System.DateOnly" || fullName == "System.TimeOnly"
            || fullName == "decimal" || fullName == "System.Decimal"
            || fullName == "System.Uri")
            return false;

        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            && namedType.TypeArguments.Length == 1)
        {
            return IsComplexType(namedType.TypeArguments[0]);
        }

        if (fullName.Contains("LanguageExt.Option<"))
            return false;

        if (IsCollectionType(fullName))
            return false;

        if (typeSymbol.TypeKind == TypeKind.Class
            || typeSymbol.TypeKind == TypeKind.Struct
            || typeSymbol.TypeKind == TypeKind.Interface)
            return true;

        return false;
    }
}
