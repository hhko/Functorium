---
title: "File-Based Apps"
---

To write release note automation scripts, do you need to create a project file, add it to a solution, and set up build configurations? With .NET 10's File-based App, you can skip all that hassle and **run directly from a single C# file.** This section examines why File-based Apps are well-suited for automation scripts and how they are actually used.

## What Is a File-Based App?

File-based App is an execution method introduced in .NET 10. Previously, you had to create multiple files including a `.csproj` project file, `Program.cs`, and necessary class files, but with File-based App, **everything is handled in a single `MyApp.cs` file.** You can run a program with just a C# file, without a project file.

## Why File-Based Apps Are Suitable for Automation Scripts

The biggest advantage is a **fast start.** The traditional approach required creating a project with `dotnet new console`, navigating to the directory, and then running `dotnet run`. With File-based App, a single line `dotnet MyApp.cs` is all you need.

```bash
# Traditional approach: Requires project creation
dotnet new console -n MyApp
cd MyApp
dotnet run

# File-based App: Run immediately
dotnet MyApp.cs
```

This is particularly suitable for **tool-type programs** like release note automation, build scripts, code generators, and analysis tools. Since it is a single file, change history tracking is also simple, and copying a single file allows immediate use in another environment.

However, since it cannot be split into multiple files and writing unit tests is difficult, it is unsuitable for large-scale applications. The traditional project approach is also better when complex build configurations are needed.

## Basic Syntax

The simplest form looks like this.

```csharp
#!/usr/bin/env dotnet

// hello.cs
Console.WriteLine("Hello, World!");
```

```bash
# Run
dotnet hello.cs
```

The first line `#!/usr/bin/env dotnet` is the Shebang line. On Unix systems, after `chmod +x hello.cs`, it enables direct execution with `./hello.cs`.

## Package Reference: `#:package` Directive

In File-based Apps, since there is no `.csproj`, a separate method for referencing NuGet packages is needed. The `#:package` directive was introduced for this purpose. Declaring packages and versions directly in the file allows the runtime to automatically restore and reference them.

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System.CommandLine;
using Spectre.Console;

// Now System.CommandLine and Spectre.Console are available
AnsiConsole.WriteLine("Hello!");
```

There are rules about the placement of this directive. It **must be at the top of the file**, after the Shebang and comments, but before using statements.

```csharp
#!/usr/bin/env dotnet        // 1. Shebang (optional)

// Comments                   // 2. Comments (optional)

#:package Spectre.Console@0.54.0  // 3. Package directives

using System;                // 4. using statements
using Spectre.Console;

// Code starts                // 5. Actual code
```

## Execution Methods

The basic execution is `dotnet MyScript.cs`. To pass arguments, append them after the filename.

```bash
# Basic execution
dotnet MyScript.cs

# Passing arguments
dotnet MyScript.cs --base origin/release/1.0 --target HEAD

# Running from working directory
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

## Release Note Automation Scripts

The Functorium project implements release note automation with three File-based Apps.

```txt
.release-notes/scripts/
├── AnalyzeAllComponents.cs    # Component analysis
├── ExtractApiChanges.cs       # API change extraction
└── ApiGenerator.cs            # Public API generation
```

These scripts all commonly use two packages: `System.CommandLine@2.0.1` for CLI argument parsing and `Spectre.Console@0.54.0` for rich console UI.

## Practical Example: Simple Analysis Script

Let's look at what a File-based App actually looks like with a simple file analysis script. This program takes a directory and shows file counts by extension in a table.

```csharp
#!/usr/bin/env dotnet

// SimpleAnalyzer.cs - Simple file analysis script
// Usage: dotnet SimpleAnalyzer.cs <directory>

#:package Spectre.Console@0.54.0

using System;
using System.IO;
using System.Linq;
using Spectre.Console;

// Check arguments
if (args.Length == 0)
{
    AnsiConsole.MarkupLine("[red]Error:[/] Directory path required");
    AnsiConsole.MarkupLine("[dim]Usage: dotnet SimpleAnalyzer.cs <directory>[/]");
    return 1;
}

var directory = args[0];

// Check directory
if (!Directory.Exists(directory))
{
    AnsiConsole.MarkupLine($"[red]Error:[/] Directory not found: {directory}");
    return 1;
}

// Header
AnsiConsole.Write(new Rule("[bold blue]File Analysis[/]").RuleStyle("blue"));
AnsiConsole.WriteLine();

// File analysis
var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
var groupedFiles = files
    .GroupBy(f => Path.GetExtension(f).ToLower())
    .OrderByDescending(g => g.Count())
    .Take(10);

// Result table
var table = new Table()
    .Border(TableBorder.Rounded)
    .AddColumn("Extension")
    .AddColumn("Count");

foreach (var group in groupedFiles)
{
    var ext = string.IsNullOrEmpty(group.Key) ? "(no ext)" : group.Key;
    table.AddRow(ext, group.Count().ToString());
}

AnsiConsole.Write(table);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine($"[dim]Total: {files.Length} files[/]");

return 0;
```

Run:
```bash
dotnet SimpleAnalyzer.cs ./src
```

Package references, argument handling, and console UI are all contained in a single file. This is the essence of File-based App. You can utilize all C# features without the overhead of a project structure.

## FAQ

### Q1: How do File-based Apps differ from traditional `.csproj` projects?
**A**: File-based Apps run from a single `.cs` file without a project file (`.csproj`). NuGet package references are declared directly in the file using the `#:package` directive, and execution is immediate with `dotnet MyApp.cs`. However, splitting into multiple files or writing unit tests is difficult, making them suitable for automation scripts and simple tools.

### Q2: What is the difference between the `#:package` directive and the `#r` directive?
**A**: `#r` is the syntax used in C# Interactive (`.csx`), and `#:package` is the directive exclusive to .NET 10 File-based Apps. `#:package` explicitly specifies the NuGet package name and version (`PackageName@Version`), and the runtime automatically restores packages. It must be placed at the top of the file, before `using` statements.

### Q3: Can multiple classes be used in a File-based App?
**A**: Yes, multiple classes, records, and static methods can all be defined **within a single `.cs` file.** However, splitting into multiple files is not possible, so readability may decrease as the code grows longer. In that case, you should consider the traditional `.csproj` project approach.

The following sections will examine the two core packages used by these scripts: System.CommandLine and Spectre.Console.
