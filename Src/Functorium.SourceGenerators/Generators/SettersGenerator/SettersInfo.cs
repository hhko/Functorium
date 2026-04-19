using Microsoft.CodeAnalysis;

namespace Functorium.SourceGenerators.Generators.SettersGenerator;

/// <summary>
/// ExecuteUpdate SetProperty 생성에 필요한 메타데이터.
/// </summary>
public readonly record struct SettersInfo
{
    public readonly string Namespace;
    public readonly string ClassName;
    public readonly EquatableArray<string> PropertyNames;
    public readonly Location? Location;

    public static readonly SettersInfo None = new(string.Empty, string.Empty, [], null);

    public SettersInfo(
        string @namespace,
        string className,
        string[] propertyNames,
        Location? location)
    {
        Namespace = @namespace;
        ClassName = className;
        PropertyNames = new EquatableArray<string>(propertyNames);
        Location = location;
    }
}

/// <summary>
/// Incremental generator 캐싱을 위한 배열 래퍼.
/// </summary>
public readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    private readonly T[] _array;

    public EquatableArray(T[] array) => _array = array;

    public int Length => _array?.Length ?? 0;
    public T this[int index] => _array[index];

    public bool Equals(EquatableArray<T> other)
    {
        if (_array is null && other._array is null) return true;
        if (_array is null || other._array is null) return false;
        if (_array.Length != other._array.Length) return false;
        for (int i = 0; i < _array.Length; i++)
        {
            if (!_array[i].Equals(other[i]))
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);
    public override int GetHashCode()
    {
        if (_array is null) return 0;
        unchecked
        {
            int hash = 17;
            foreach (var item in _array)
                hash = hash * 31 + item.GetHashCode();
            return hash;
        }
    }
}
