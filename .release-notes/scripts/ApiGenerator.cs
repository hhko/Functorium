#!/usr/bin/env dotnet

// .NET 10 File-based Program - API Generator
// Usage: dotnet run ApiGenerator.cs -- <dll-path> <output-path>
// Example: dotnet run ApiGenerator.cs -- bin/Release/net10.0/Functorium.dll api.txt

#:package PublicApiGenerator@11.5.4

using System.Reflection;
using PublicApiGenerator;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ApiGenerator <dll-path> <output-path>");
    Console.WriteLine("  or:  ApiGenerator <dll-path> -  (output to stdout)");
    return 1;
}

var dllPath = Path.GetFullPath(args[0]);
var outputPath = args[1];

if (!File.Exists(dllPath))
{
    Console.Error.WriteLine($"Error: File not found: {dllPath}");
    return 1;
}

try
{
    // Get the directory containing the DLL
    var dllDirectory = Path.GetDirectoryName(dllPath)!;

    // Collect search directories for assembly resolution
    var searchDirs = new List<string> { dllDirectory };

    // Add .NET SDK directories
    var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT")
        ?? @"C:\Program Files\dotnet";

    // Find latest .NET 10.x reference assemblies
    var netCorePacksDir = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");
    if (Directory.Exists(netCorePacksDir))
    {
        var net10Versions = Directory.GetDirectories(netCorePacksDir)
            .Select(d => Path.GetFileName(d))
            .Where(v => v.StartsWith("10."))
            .OrderByDescending(v => v)
            .FirstOrDefault();

        if (net10Versions != null)
        {
            var net10RefPath = Path.Combine(netCorePacksDir, net10Versions, "ref", "net10.0");
            if (Directory.Exists(net10RefPath))
            {
                searchDirs.Add(net10RefPath);
            }
        }
    }

    // Find latest ASP.NET Core 10.x reference assemblies
    var aspnetPacksDir = Path.Combine(dotnetRoot, "packs", "Microsoft.AspNetCore.App.Ref");
    if (Directory.Exists(aspnetPacksDir))
    {
        var aspnet10Versions = Directory.GetDirectories(aspnetPacksDir)
            .Select(d => Path.GetFileName(d))
            .Where(v => v.StartsWith("10."))
            .OrderByDescending(v => v)
            .FirstOrDefault();

        if (aspnet10Versions != null)
        {
            var aspnetNet10 = Path.Combine(aspnetPacksDir, aspnet10Versions, "ref", "net10.0");
            if (Directory.Exists(aspnetNet10))
            {
                searchDirs.Add(aspnetNet10);
            }
        }
    }

    // Create temp directory for assembly resolution
    var tempDir = Path.Combine(Path.GetTempPath(), $"ApiGenerator_{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);

    try
    {
        // Copy target DLL
        var targetDll = Path.Combine(tempDir, Path.GetFileName(dllPath));
        File.Copy(dllPath, targetDll, true);

        // Copy all local dependencies
        foreach (var dll in Directory.GetFiles(dllDirectory, "*.dll"))
        {
            var dest = Path.Combine(tempDir, Path.GetFileName(dll));
            if (!File.Exists(dest))
            {
                File.Copy(dll, dest, true);
            }
        }

        // Copy framework assemblies
        foreach (var searchDir in searchDirs.Skip(1))
        {
            if (!Directory.Exists(searchDir)) continue;

            foreach (var dll in Directory.GetFiles(searchDir, "*.dll"))
            {
                var dest = Path.Combine(tempDir, Path.GetFileName(dll));
                if (!File.Exists(dest))
                {
                    File.Copy(dll, dest, true);
                }
            }
        }

        // Load assembly
        #pragma warning disable IL2026 // Suppress trimming warning - this tool uses reflection by design
        var assembly = Assembly.LoadFrom(targetDll);
        #pragma warning restore IL2026

        var options = new ApiGeneratorOptions
        {
            IncludeAssemblyAttributes = false
        };

        var publicApi = assembly.GeneratePublicApi(options);

        if (outputPath == "-")
        {
            Console.WriteLine(publicApi);
        }
        else
        {
            File.WriteAllText(outputPath, publicApi);
            Console.WriteLine($"API written to: {outputPath}");
        }

        return 0;
    }
    finally
    {
        // Cleanup temp directory
        try
        {
            Directory.Delete(tempDir, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.Error.WriteLine($"Inner: {ex.InnerException.Message}");
    }
    return 1;
}
