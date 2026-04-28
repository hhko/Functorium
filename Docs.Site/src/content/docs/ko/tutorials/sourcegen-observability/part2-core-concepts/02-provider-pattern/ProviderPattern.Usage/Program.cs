using ProviderPattern.Generated;

Console.WriteLine("=== Discovered Classes ===");
foreach (var name in ClassList.All)
{
    Console.WriteLine($"  - {name}");
}
