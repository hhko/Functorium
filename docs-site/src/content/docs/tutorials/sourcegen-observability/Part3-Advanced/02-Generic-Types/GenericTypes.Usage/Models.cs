using GenericTypes.Generated;

namespace GenericTypes.Usage;

[AutoTypeInfo]
public partial class Repository<T> where T : class
{
    public T? GetById(string id) => default;
}

[AutoTypeInfo]
public partial class KeyValueStore<TKey, TValue>
    where TKey : notnull
    where TValue : struct
{
    public TValue Get(TKey key) => default;
}
