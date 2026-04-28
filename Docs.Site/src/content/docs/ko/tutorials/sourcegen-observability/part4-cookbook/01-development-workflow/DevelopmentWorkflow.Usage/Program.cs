using DevelopmentWorkflow.Usage;

Console.WriteLine($"Type: {SampleService.TypeName}");
Console.WriteLine($"Methods: [{string.Join(", ", SampleService.PublicMethods)}]");
