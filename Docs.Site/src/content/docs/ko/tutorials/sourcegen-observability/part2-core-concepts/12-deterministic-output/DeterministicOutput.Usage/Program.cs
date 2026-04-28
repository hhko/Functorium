using DeterministicOutput.Generated;

Console.WriteLine("=== Registered Types (deterministic order) ===");
foreach (var type in ServiceRegistry.RegisteredTypes)
{
    Console.WriteLine($"  - {type}");
}
Console.WriteLine();
Console.WriteLine("Types are always in the same order regardless of");
Console.WriteLine("file processing order — ensuring deterministic output.");
