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

    // .NET 10.0 reference assemblies
    var net10RefPath = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref", "10.0.0", "ref", "net10.0");
    if (Directory.Exists(net10RefPath))
    {
        searchDirs.Add(net10RefPath);
    }

    // ASP.NET Core reference assemblies (contains Microsoft.Extensions.*)
    var aspnetNet10 = Path.Combine(dotnetRoot, "packs", "Microsoft.AspNetCore.App.Ref", "10.0.0", "ref", "net10.0");
    if (Directory.Exists(aspnetNet10))
    {
        searchDirs.Add(aspnetNet10);
    }

    // Runtime directory
    var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
    searchDirs.Add(runtimeDir);

    // Create symlinks or copy required assemblies to a temp directory
    // that PublicApiGenerator can use for resolution
    var tempDir = Path.Combine(Path.GetTempPath(), $"ApiGenerator_{Guid.NewGuid():N}");
    Directory.CreateDirectory(tempDir);

    try
    {
        // Copy target DLL
        var targetDll = Path.Combine(tempDir, Path.GetFileName(dllPath));
        File.Copy(dllPath, targetDll, true);

        // Copy all local dependencies from publish folder
        foreach (var dll in Directory.GetFiles(dllDirectory, "*.dll"))
        {
            var dest = Path.Combine(tempDir, Path.GetFileName(dll));
            if (!File.Exists(dest))
            {
                File.Copy(dll, dest, true);
            }
        }

        // Copy required framework assemblies
        foreach (var searchDir in searchDirs.Skip(1)) // Skip dllDirectory (already copied)
        {
            foreach (var dll in Directory.GetFiles(searchDir, "*.dll"))
            {
                var dest = Path.Combine(tempDir, Path.GetFileName(dll));
                if (!File.Exists(dest))
                {
                    File.Copy(dll, dest, true);
                }
            }
        }

        // Load assembly from temp directory
        var assembly = Assembly.LoadFrom(targetDll);

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
