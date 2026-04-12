---
title: "Writing Your Own Script"
---

In the previous section, we ran the release note automation using the `/release-note` command. In that process, we confirmed that C# scripts like `AnalyzeAllComponents.cs` and `ExtractApiChanges.cs` play a core role. These scripts are all built using .NET 10's File-based App feature.

In this section, we will write .NET 10 File-based Apps from scratch. Starting with a simple Hello World, we will progressively increase the difficulty through CLI argument handling, file system analysis, and Git commit analysis. By the end of this process, you will have the foundation to read and modify the release note automation scripts.

## Exercise 1: Hello World

Everything starts with the simplest program. .NET 10 File-based Apps run with just a single `.cs` file without a `.csproj` file. You can write and execute code immediately without project setup or build configuration, making it suitable for script writing.

```csharp
#!/usr/bin/env dotnet

// hello.cs - Simple Hello World
Console.WriteLine("Hello, World!");
```

The first line `#!/usr/bin/env dotnet` is a Shebang line that allows direct execution with `./hello.cs` on Unix environments. On Windows, use `dotnet hello.cs`.

```bash
dotnet hello.cs
# Output: Hello, World!
```

## Exercise 2: Argument Handling

To build real tools, you need to receive input from users. Let's create a simple program that takes a name and greets, using the `System.CommandLine` package to build a structured CLI interface. Handling options like `--base` and `--target` in the release note scripts follows exactly this pattern.

```csharp
#!/usr/bin/env dotnet

// greet.cs - Greet someone by name
#:package System.CommandLine@2.0.1

using System.CommandLine;

// Argument definition
var nameArgument = new Argument<string>("name", "Your name");

// Option definition
var loudOption = new Option<bool>("--loud", "Print in uppercase");
loudOption.AddAlias("-l");

// Command definition
var rootCommand = new RootCommand("Greet someone")
{
    nameArgument,
    loudOption
};

// Handler
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var name = parseResult.GetValue(nameArgument)!;
    var loud = parseResult.GetValue(loudOption);

    var message = $"Hello, {name}!";

    if (loud)
    {
        message = message.ToUpper();
    }

    Console.WriteLine(message);
    return 0;
});

return rootCommand.Parse(args).Invoke();
```

The `#:package` directive is how File-based Apps reference NuGet packages. It serves the role of `PackageReference` in `.csproj`.

```bash
# Basic execution
dotnet greet.cs Alice
# Output: Hello, Alice!

# Uppercase option
dotnet greet.cs Alice --loud
# Output: HELLO, ALICE!

# Help
dotnet greet.cs --help
```

Check that `System.CommandLine` automatically generates the `--help` option. The descriptions of arguments and options are displayed directly in the help output.

## Exercise 3: File Analysis Tool

Now let's build a practical tool. It's a tool that traverses a directory and shows file statistics by extension. Similar to the pattern in release note automation that produces statistics like "31 files, 19 commits", it explores the file system and presents organized results.

Here we additionally use the `Spectre.Console` package. It's a library that makes it easy to output visual elements like tables, colors, and separators to the console.

```csharp
#!/usr/bin/env dotnet

// file-stats.cs - Analyze file statistics in a directory
#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using Spectre.Console;

// Option definitions
var pathOption = new Option<string>("--path", "Directory to analyze");
pathOption.DefaultValueFactory = (_) => ".";
pathOption.AddAlias("-p");

var topOption = new Option<int>("--top", "Number of extensions to show");
topOption.DefaultValueFactory = (_) => 10;
topOption.AddAlias("-t");

// Command definition
var rootCommand = new RootCommand("Analyze file statistics")
{
    pathOption,
    topOption
};

// Handler
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var path = parseResult.GetValue(pathOption)!;
    var top = parseResult.GetValue(topOption);

    AnalyzeDirectory(path, top);
    return 0;
});

return rootCommand.Parse(args).Invoke();

// Analysis function
static void AnalyzeDirectory(string path, int top)
{
    // Verify directory
    if (!Directory.Exists(path))
    {
        AnsiConsole.MarkupLine($"[red]Error:[/] Directory not found: {path}");
        return;
    }

    // Header
    AnsiConsole.Write(new Rule("[bold blue]File Statistics[/]").RuleStyle("blue"));
    AnsiConsole.WriteLine();

    // Collect files
    var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

    if (files.Length == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No files found[/]");
        return;
    }

    // Group by extension
    var stats = files
        .GroupBy(f => Path.GetExtension(f).ToLower())
        .Select(g => new
        {
            Extension = string.IsNullOrEmpty(g.Key) ? "(no ext)" : g.Key,
            Count = g.Count(),
            TotalSize = g.Sum(f => new FileInfo(f).Length)
        })
        .OrderByDescending(x => x.Count)
        .Take(top);

    // Create table
    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Extension")
        .AddColumn(new TableColumn("Files").RightAligned())
        .AddColumn(new TableColumn("Size").RightAligned());

    foreach (var stat in stats)
    {
        var size = FormatSize(stat.TotalSize);
        table.AddRow(
            $"[cyan]{stat.Extension}[/]",
            stat.Count.ToString(),
            $"[dim]{size}[/]"
        );
    }

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();

    // Summary
    var totalSize = files.Sum(f => new FileInfo(f).Length);
    AnsiConsole.MarkupLine($"[dim]Total: {files.Length} files, {FormatSize(totalSize)}[/]");
}

// Size format function
static string FormatSize(long bytes)
{
    string[] units = { "B", "KB", "MB", "GB" };
    double size = bytes;
    int unit = 0;

    while (size >= 1024 && unit < units.Length - 1)
    {
        size /= 1024;
        unit++;
    }

    return $"{size:0.##} {units[unit]}";
}
```

Running it outputs a table organized by file count and size per extension.

```bash
# Analyze current directory
dotnet file-stats.cs

# Analyze a specific directory
dotnet file-stats.cs --path ./src

# Show only top 5
dotnet file-stats.cs --path ./src --top 5
```

The output looks like this.

```txt
───────────────── File Statistics ─────────────────

╭───────────┬───────┬──────────╮
│ Extension │ Files │     Size │
├───────────┼───────┼──────────┤
│ .cs       │    45 │ 125.3 KB │
│ .json     │    12 │   8.5 KB │
│ .md       │     8 │  15.2 KB │
│ .csproj   │     5 │   3.1 KB │
│ .txt      │     3 │   1.2 KB │
╰───────────┴───────┴──────────╯

Total: 73 files, 153.3 KB
```

## Exercise 4: Commit Analysis Tool

The final exercise is the tool closest to the core of release note automation. It reads Git commit messages, classifies them by Conventional Commits type, and displays them as a visual bar chart. You can think of it as a miniature version of the commit analysis that Claude performs in Phase 3.

This script also includes an asynchronous pattern for executing an external process (`git log`) and parsing its output, allowing you to learn techniques frequently used in practice.

```csharp
#!/usr/bin/env dotnet

// commit-analyzer.cs - Git commit analysis tool
#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Spectre.Console;

// Option definition
var countOption = new Option<int>("--count", "Number of commits to analyze");
countOption.DefaultValueFactory = (_) => 50;
countOption.AddAlias("-n");

// Command definition
var rootCommand = new RootCommand("Analyze git commits by type")
{
    countOption
};

// Handler
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var count = parseResult.GetValue(countOption);
    await AnalyzeCommitsAsync(count);
    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();

// Analysis function
static async Task AnalyzeCommitsAsync(int count)
{
    // Header
    AnsiConsole.Write(new Rule("[bold blue]Commit Analysis[/]").RuleStyle("blue"));
    AnsiConsole.WriteLine();

    // Get Git commits
    var commits = await GetCommitsAsync(count);

    if (commits.Count == 0)
    {
        AnsiConsole.MarkupLine("[yellow]No commits found[/]");
        return;
    }

    // Classify by commit type
    var types = new Dictionary<string, int>
    {
        { "feat", 0 },
        { "fix", 0 },
        { "docs", 0 },
        { "refactor", 0 },
        { "test", 0 },
        { "chore", 0 },
        { "other", 0 }
    };

    var typePattern = new Regex(@"^(\w+)(\(.+\))?!?:");

    foreach (var commit in commits)
    {
        var match = typePattern.Match(commit);
        if (match.Success)
        {
            var type = match.Groups[1].Value.ToLower();
            if (types.ContainsKey(type))
                types[type]++;
            else
                types["other"]++;
        }
        else
        {
            types["other"]++;
        }
    }

    // Results table
    var table = new Table()
        .Border(TableBorder.Rounded)
        .AddColumn("Type")
        .AddColumn(new TableColumn("Count").RightAligned())
        .AddColumn("Bar");

    var maxCount = types.Values.Max();

    foreach (var kvp in types.OrderByDescending(x => x.Value))
    {
        if (kvp.Value > 0)
        {
            var barLength = (int)((double)kvp.Value / maxCount * 20);
            var bar = new string('█', barLength);
            var color = GetTypeColor(kvp.Key);

            table.AddRow(
                $"[{color}]{kvp.Key}[/]",
                kvp.Value.ToString(),
                $"[{color}]{bar}[/]"
            );
        }
    }

    AnsiConsole.Write(table);
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[dim]Analyzed {commits.Count} commits[/]");
}

// Execute Git command
static async Task<List<string>> GetCommitsAsync(int count)
{
    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"log --oneline -n {count} --format=%s",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }
    };

    process.Start();
    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();

    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
}

// Color per type
static string GetTypeColor(string type) => type switch
{
    "feat" => "green",
    "fix" => "red",
    "docs" => "blue",
    "refactor" => "yellow",
    "test" => "cyan",
    "chore" => "grey",
    _ => "white"
};
```

```bash
# Analyze the last 50 commits (default)
dotnet commit-analyzer.cs

# Analyze the last 100 commits
dotnet commit-analyzer.cs --count 100
```

The output looks like this.

```txt
───────────────── Commit Analysis ─────────────────

╭──────────┬───────┬──────────────────────╮
│ Type     │ Count │ Bar                  │
├──────────┼───────┼──────────────────────┤
│ feat     │    15 │ ████████████████████ │
│ fix      │     8 │ ██████████           │
│ docs     │     6 │ ████████             │
│ chore    │     5 │ ██████               │
│ refactor │     4 │ █████                │
│ test     │     3 │ ████                 │
│ other    │     2 │ ██                   │
╰──────────┴───────┴──────────────────────╯

Analyzed 50 commits
```

## Key Patterns Summary

Through the four exercises, you should start to see the common structure of .NET 10 File-based Apps. All the release note automation scripts follow this pattern as well.

```csharp
#!/usr/bin/env dotnet              // 1. Shebang

#:package <package>@<version>      // 2. Package reference

using System;                       // 3. using statements

// Option/argument definitions      // 4. CLI definitions
var option = new Option<string>("--name");
var rootCommand = new RootCommand { option };

// Handler                          // 5. Execution logic
rootCommand.SetAction((parseResult, ct) => {
    var value = parseResult.GetValue(option);
    // Perform work
    return 0;
});

return rootCommand.Parse(args).Invoke();  // 6. Execute
```

Having commonly used packages organized is also convenient when creating new scripts.

| Package | Purpose |
|--------|------|
| `System.CommandLine@2.0.1` | CLI argument parsing |
| `Spectre.Console@0.54.0` | Console UI |
| `System.Text.Json` | JSON processing (included by default) |

## FAQ

### Q1: Are there restrictions on packages that can be referenced via the `#:package` directive in File-based Apps?
**A**: Any package published to NuGet can be referenced. However, **packages with native dependencies (e.g., SQLite) or** packages requiring additional build configuration may not work properly in the File-based App environment. Pure .NET packages like `System.CommandLine`, `Spectre.Console`, and `System.Text.Json` can be used without issues.

### Q2: How do `SetAction` and `SetHandler` differ in `System.CommandLine`?
**A**: `SetHandler` is the API from an earlier version, and **`SetAction` was introduced in `System.CommandLine` 2.0.1** as the new handler registration method. `SetAction` receives `ParseResult` directly, allowing more flexible argument handling, and all scripts in this tutorial use the `SetAction` pattern.

### Q3: Can scripts be written using only basic `Console.WriteLine` without `Spectre.Console`?
**A**: Yes. `Spectre.Console` is an optional package for **easily adding visual elements** like tables, colors, and spinners. The same functionality can be implemented with basic `Console.WriteLine`, and basic console may actually be more suitable when piping output or redirecting to log files.

### Q4: What is the difference between a File-based App `.cs` file and a regular C# project `.cs` file?
**A**: File-based App `.cs` files can include **Shebang lines (`#!/usr/bin/env dotnet`) and `#:package` directives,** and can be executed directly with `dotnet <filename>.cs` without a `.csproj` file. Regular project `.cs` files must be executed with `dotnet run` alongside a `.csproj`. File-based Apps are suited for script-like tasks, while regular projects are suited for libraries or large-scale applications.

Now when reading the release note automation script code, you should be able to understand what role each part plays. The next section covers problems that may arise during the exercises and their solutions.

- [Troubleshooting Guide](03-troubleshooting.md)
