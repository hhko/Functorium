namespace InterfaceSegregationAndVariance;

/// <summary>
/// 읽기 전용 인터페이스 - 공변(out)
/// </summary>
public interface IReadable<out T>
{
    T Value { get; }
    bool IsValid { get; }
}

/// <summary>
/// 쓰기(입력) 인터페이스 - 반공변(in)
/// </summary>
public interface IWritable<in T>
{
    void Write(T value);
}

/// <summary>
/// 팩토리 인터페이스 - CRTP (생성)
/// </summary>
public interface IFactory<TSelf> where TSelf : IFactory<TSelf>
{
    static abstract TSelf Create(string value);
    static abstract TSelf CreateEmpty();
}

/// <summary>
/// 읽기+쓰기 = 불변 (out+in이 아닌 두 인터페이스 구현)
/// </summary>
public interface IReadWrite<T> : IReadable<T>, IWritable<T>;

/// <summary>
/// 구체 구현: 3개 인터페이스를 모두 구현
/// </summary>
public sealed record Container(string Value) : IReadable<string>, IFactory<Container>
{
    public bool IsValid => !string.IsNullOrEmpty(Value);

    public static Container Create(string value) => new(value);
    public static Container CreateEmpty() => new(string.Empty);
}

/// <summary>
/// 읽기+쓰기 구현
/// </summary>
public class MutableContainer<T> : IReadWrite<T>
{
    public T Value { get; private set; } = default!;
    public bool IsValid => Value is not null;

    public void Write(T value) => Value = value;
}
