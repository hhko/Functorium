using DebuggingSetup.Generated;

Console.WriteLine($"Generator Version: {DebugInfo.GeneratorVersion}");
Console.WriteLine($"Debug Build: {DebugInfo.IsDebugBuild}");
Console.WriteLine();
Console.WriteLine("To debug a source generator:");
Console.WriteLine("  1. Uncomment Debugger.Launch() in the generator");
Console.WriteLine("  2. Rebuild the generator project");
Console.WriteLine("  3. Build this usage project — debugger will attach");
