namespace Functorium.Abstractions.Utilities;

public static class DictionaryUtilities
{
    public static void AddOrUpdate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value) where TKey : notnull
    {
        dictionary.TryAdd(key, value);
        dictionary[key] = value;
    }
}
