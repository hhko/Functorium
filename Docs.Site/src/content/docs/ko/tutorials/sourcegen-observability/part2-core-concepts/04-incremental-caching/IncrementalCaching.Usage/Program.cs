using IncrementalCaching.Usage;

Console.WriteLine($"Type: {CachedService.TypeName}");
Console.WriteLine($"Namespace: {CachedService.TypeNamespace}");
Console.WriteLine();
Console.WriteLine("Value equality in the generator's data model ensures");
Console.WriteLine("the source output is only regenerated when data changes.");
