using GenericTypes.Usage;

Console.WriteLine($"=== Repository<T> ===");
Console.WriteLine($"  Arity: {Repository<string>.GenericArity}");
foreach (var info in Repository<string>.TypeParameterInfo)
    Console.WriteLine($"  {info}");

Console.WriteLine($"\n=== KeyValueStore<TKey, TValue> ===");
Console.WriteLine($"  Arity: {KeyValueStore<string, int>.GenericArity}");
foreach (var info in KeyValueStore<string, int>.TypeParameterInfo)
    Console.WriteLine($"  {info}");
