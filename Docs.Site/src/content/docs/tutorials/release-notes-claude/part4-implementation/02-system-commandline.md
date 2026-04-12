---
title: "System.CommandLine Package"
---

When an automation script directly parses `args[0]`, `args[1]`, it quickly hits limitations. Every time the argument order changes, defaults are needed, or `--help` must be shown, the code becomes more complex. System.CommandLine is Microsoft's **command-line argument parsing library** that solves these problems. Define the CLI declaratively, and parsing, validation, and help generation are handled automatically.

## Package Installation

In File-based Apps, install with the `#:package` directive.

```csharp
#:package System.CommandLine@2.0.1
```

## Core Elements of a CLI

A System.CommandLine CLI consists of four building blocks.

| Element | Description | Example |
|---------|-------------|---------|
| RootCommand | Top-level command | `dotnet MyApp.cs` |
| Option | Named argument | `--base`, `-b` |
| Argument | Positional argument | `<file>` |
| Command | Subcommand | `add`, `remove` |

Let's see how these elements correspond in an actual command.

```bash
dotnet MyApp.cs add --name "Item" --priority 1 file.txt
#               ^^^  ^^^^^^^^^^^^  ^^^^^^^^^^^  ^^^^^^^^
#               |    Option        Option       Argument
#               Command
```

## Defining Options

Options are named arguments. Let's start from the most basic form.

```csharp
using System.CommandLine;

// string type Option
var nameOption = new Option<string>("--name")
{
    Description = "The name to use"
};

// int type Option (with default value)
var countOption = new Option<int>("--count")
{
    Description = "Number of items"
};
countOption.DefaultValueFactory = (_) => 10;
```

Adding short aliases allows both long and short names.

```csharp
var verboseOption = new Option<bool>(new[] { "--verbose", "-v" })
{
    Description = "Enable verbose output"
};
```

To make an Option required, set `IsRequired`.

```csharp
var requiredOption = new Option<string>("--required")
{
    Description = "This option is required",
    IsRequired = true
};
```

## Defining Arguments

Arguments are positional, identified by position rather than name.

```csharp
// Single Argument
var fileArgument = new Argument<string>("file")
{
    Description = "The file to process"
};

// Multiple Arguments
var filesArgument = new Argument<string[]>("files")
{
    Description = "Files to process",
    Arity = ArgumentArity.ZeroOrMore
};
```

## Assembling a CLI with RootCommand

Once Options and Arguments are defined, register them with a RootCommand and set up a handler. Let's build one step by step following the typical pattern of release note scripts.

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1

using System.CommandLine;

// Define Options
var baseOption = new Option<string>("--base")
{
    Description = "Base branch for comparison"
};
baseOption.DefaultValueFactory = (_) => "origin/release/1.0";

var targetOption = new Option<string>("--target")
{
    Description = "Target branch for comparison"
};
targetOption.DefaultValueFactory = (_) => "HEAD";

// Create RootCommand
var rootCommand = new RootCommand("My CLI application")
{
    baseOption,
    targetOption
};

// Set up handler
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    Console.WriteLine($"Base: {baseBranch}");
    Console.WriteLine($"Target: {targetBranch}");

    return 0;
});

// Execute
return await rootCommand.Parse(args).InvokeAsync();
```

Run:
```bash
dotnet MyApp.cs --base origin/main --target HEAD
# Output:
# Base: origin/main
# Target: HEAD
```

If async operations are needed in the handler, add the `async` keyword.

```csharp
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    await AnalyzeAsync(baseBranch, targetBranch);
    return 0;
});
```

## Adding Subcommands

To provide multiple actions in a single CLI, use subcommands.

```csharp
// add subcommand
var addCommand = new Command("add", "Add a new item")
{
    nameOption
};

addCommand.SetAction((parseResult, cancellationToken) =>
{
    var name = parseResult.GetValue(nameOption)!;
    Console.WriteLine($"Adding: {name}");
    return 0;
});

// remove subcommand
var removeCommand = new Command("remove", "Remove an item")
{
    nameOption
};

removeCommand.SetAction((parseResult, cancellationToken) =>
{
    var name = parseResult.GetValue(nameOption)!;
    Console.WriteLine($"Removing: {name}");
    return 0;
});

// Add to RootCommand
var rootCommand = new RootCommand("Item manager")
{
    addCommand,
    removeCommand
};
```

Run:
```bash
dotnet MyApp.cs add --name "Item1"
dotnet MyApp.cs remove --name "Item1"
```

## Practical Example: CLI Configuration of AnalyzeAllComponents.cs

Let's see how System.CommandLine is used in the actual release note automation code. Two Options, `--base` and `--target`, receive the comparison target branches, and an async handler performs the analysis.

```csharp
#!/usr/bin/env dotnet

#:package System.CommandLine@2.0.1
#:package Spectre.Console@0.54.0

using System;
using System.CommandLine;
using System.Threading.Tasks;

// Define Options
var baseOption = new Option<string>("--base")
{
    Description = "Base branch for comparison"
};
baseOption.DefaultValueFactory = (_) => "origin/release/1.0";

var targetOption = new Option<string>("--target")
{
    Description = "Target branch for comparison"
};
targetOption.DefaultValueFactory = (_) => "origin/main";

// Configure RootCommand
var rootCommand = new RootCommand("Automated analysis of all components")
{
    baseOption,
    targetOption
};

// Async handler
rootCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var baseBranch = parseResult.GetValue(baseOption)!;
    var targetBranch = parseResult.GetValue(targetOption)!;

    await AnalyzeAllComponentsAsync(baseBranch, targetBranch);
    return 0;
});

// Execute
return await rootCommand.Parse(args).InvokeAsync();

// Main logic
static async Task AnalyzeAllComponentsAsync(string baseBranch, string targetBranch)
{
    Console.WriteLine($"Analyzing from {baseBranch} to {targetBranch}...");
    // Actual analysis logic
}
```

## Automatically Provided Features

Using System.CommandLine automatically provides three features without additional code.

Passing `--help` outputs help with Option lists and default values.

```bash
$ dotnet MyApp.cs --help

Description:
  Automated analysis of all components

Usage:
  MyApp [options]

Options:
  --base <base>      Base branch for comparison [default: origin/release/1.0]
  --target <target>  Target branch for comparison [default: origin/main]
  --help             Show help and usage information
  --version          Show version information
```

Version information can be checked with `--version`, and passing unknown arguments produces an error message with correct usage guidance.

```bash
$ dotnet MyApp.cs --unknown
Unrecognized command or argument '--unknown'.
```

## Pattern Summary

Here is a summary of patterns used repeatedly across release note scripts.

```csharp
// 1. Basic Option
var option = new Option<string>("--name");

// 2. Default value
option.DefaultValueFactory = (_) => "default";

// 3. Required Option
option.IsRequired = true;

// 4. Short alias
var option = new Option<string>(new[] { "--name", "-n" });
```

```csharp
// 1. Create and add Options
var rootCommand = new RootCommand("Description")
{
    option1,
    option2
};

// 2. Set up handler
rootCommand.SetAction((parseResult, cancellationToken) =>
{
    var value = parseResult.GetValue(option1);
    return 0;
});

// 3. Execute
return await rootCommand.Parse(args).InvokeAsync();
```

## FAQ

### Q1: Why use System.CommandLine instead of directly parsing `args[0]`?
**A**: Directly parsing `args[]` makes code dramatically more complex even with just 2-3 arguments for order management, default value handling, and error message generation. System.CommandLine declaratively defines Options and Arguments and **automatically handles parsing, validation, and `--help` generation**, allowing script code to focus solely on actual business logic.

### Q2: What does the `0` returned from the `SetAction` handler mean?
**A**: It is the process exit code. `0` means normal termination, and non-zero values indicate errors. This exit code is used by CI/CD pipelines or shell scripts to determine command success, so error situations should return `1` or other values.

### Q3: What is the difference between `DefaultValueFactory` and setting a default value directly in the constructor?
**A**: `DefaultValueFactory` **lazily generates the default value via a lambda.** That is, the factory is called only when the user does not specify that Option. This is especially useful when the default value must be read from an environment variable or configuration file rather than being a simple constant.

With System.CommandLine handling argument parsing and validation, script code can **focus solely on actual logic**. The next section examines Spectre.Console, which enriches the console output of these scripts.
