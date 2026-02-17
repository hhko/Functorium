using LanguageExt;
using static LanguageExt.Prelude;

namespace Functorium.Applications.Queries;

/// <summary>
/// 다중 필드 정렬 표현.
/// </summary>
public sealed class SortExpression
{
    private readonly Seq<SortField> _fields;
    public Seq<SortField> Fields => _fields;
    public bool IsEmpty => _fields.IsEmpty;

    private SortExpression(Seq<SortField> fields) => _fields = fields;

    public static SortExpression Empty => new(LanguageExt.Seq<SortField>.Empty);

    public static SortExpression By(string fieldName, SortDirection direction = SortDirection.Ascending)
        => new(Seq([new SortField(fieldName, direction)]));

    public SortExpression ThenBy(string fieldName, SortDirection direction = SortDirection.Ascending)
        => new(_fields.Add(new SortField(fieldName, direction)));
}
