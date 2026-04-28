namespace ImmutabilityRule.Domains;

/// <summary>
/// 올바른 불변 클래스: 읽기 전용 컬렉션 사용
/// </summary>
public sealed class Palette
{
    public string Name { get; }
    public IReadOnlyList<string> Colors { get; }

    private Palette(string name, IReadOnlyList<string> colors)
    {
        Name = name;
        Colors = colors;
    }

    public static Palette Create(string name, params string[] colors)
        => new(name, colors.ToList().AsReadOnly());
}
