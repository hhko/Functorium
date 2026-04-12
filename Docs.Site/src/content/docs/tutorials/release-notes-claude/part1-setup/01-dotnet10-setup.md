---
title: ".NET 10 Environment Setup"
---

The release note automation system consists of several C# scripts that analyze Git logs and generate documents. These scripts use .NET 10's **file-based app** feature, which allows execution from a single `.cs` file without a `.csproj` project file. This is ideal for quickly creating and modifying automation tools because you can run scripts immediately without any project setup.

In this section, we will install the .NET 10 SDK and verify that file-based apps work properly.

## What Is a File-Based App?

This feature, supported from .NET 10 onwards, allows you to run an application with just a single `.cs` file. Previously, you had to create a project with `dotnet new console` and then run it with `dotnet run`, but file-based apps skip that process.

```bash
# Traditional approach (requires a project)
dotnet new console -n MyApp
cd MyApp
dotnet run

# File-based app (single file)
dotnet MyScript.cs
```

The reason the automation scripts in this tutorial are written as file-based apps is clear. Scripts for release note generation need fast prototyping and modification, do not need a deployment build, and it is sufficient for them to work independently without inter-project references. On the other hand, for large-scale applications composed of multiple files, or when a DLL/EXE build is needed, or when integration with test projects is required, the traditional project approach is still more appropriate.

## .NET 10 SDK Installation

### Windows

1. Download the SDK from the [.NET download page](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Run the installer
3. After installation, verify in the terminal:

```powershell
dotnet --version
# Output: 10.0.100
```

### macOS

**Using Homebrew:**
```bash
brew install dotnet-sdk
```

**Manual installation:**
1. Download the macOS version from the [.NET download page](https://dotnet.microsoft.com/download/dotnet/10.0)
2. Run the `.pkg` file
3. Verify:
```bash
dotnet --version
```

### Linux (Ubuntu/Debian)

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-10.0

# Verify
dotnet --version
```

## Verify Installation

Run the following commands in the terminal to verify the installation was successful.

```bash
# Version check
dotnet --version
# Output: 10.0.100 (or higher)

# SDK information
dotnet --info
```

**Expected output:**
```
.NET SDK:
 Version:           10.0.100
 Commit:            ...
 Workload version:  ...

Runtime Environment:
 OS Name:     Windows
 OS Version:  10.0.22631
 OS Platform: Windows
 RID:         win-x64
 Base Path:   C:\Program Files\dotnet\sdk\10.0.100\
```

## Run Your First File-Based App

Let's test whether file-based apps work properly.

### 1. Create a Test File

Create a `hello.cs` file:

```csharp
// hello.cs
Console.WriteLine("Hello, .NET 10 file-based app!");
Console.WriteLine($"Current time: {DateTime.Now}");
```

### 2. Run

```bash
dotnet hello.cs
```

**Expected output:**
```
Hello, .NET 10 file-based app!
Current time: 2025-12-20 10:30:45 AM
```

### 3. Using NuGet Packages

File-based apps can also use NuGet packages. Add a `#r` directive at the top of the file:

```csharp
// nuget-test.cs
#r "nuget: Spectre.Console, 0.54.0"

using Spectre.Console;

AnsiConsole.MarkupLine("[green]Hello[/] from [blue]Spectre.Console[/]!");
```

**Run:**
```bash
dotnet nuget-test.cs
```

The first run may take some time as it downloads the package.

## Environment Variable Configuration

### DOTNET_ROOT (Optional)

If some tools cannot find the .NET SDK path, set the environment variable.

**Windows (PowerShell):**
```powershell
$env:DOTNET_ROOT = "C:\Program Files\dotnet"
[System.Environment]::SetEnvironmentVariable("DOTNET_ROOT", "C:\Program Files\dotnet", "User")
```

**macOS/Linux:**
```bash
export DOTNET_ROOT=/usr/share/dotnet
echo 'export DOTNET_ROOT=/usr/share/dotnet' >> ~/.bashrc
```

### PATH Verification

The `dotnet` command must be in the PATH to be executable from anywhere.

**Windows:**
```powershell
$env:Path -split ';' | Where-Object { $_ -like '*dotnet*' }
```

**macOS/Linux:**
```bash
echo $PATH | tr ':' '\n' | grep dotnet
```

## Features of File-Based Apps

### Supported Features

| Feature | Supported | Example |
|---------|----------|---------|
| Top-level statements | Yes | `Console.WriteLine("Hello");` |
| NuGet packages | Yes | `#r "nuget: PackageName, Version"` |
| Command-line arguments | Yes | Use `args` array |
| Multiple classes | Yes | Multiple class definitions in a file |
| async/await | Yes | `await Task.Delay(1000);` |
| Project references | No | Single file only |

### Limitations

File-based apps are single-file only, so they cannot directly reference other `.cs` files, and DLL/EXE builds are not possible. They are unsuitable for large-scale projects, but ideal for independently operating tools like the automation scripts in this tutorial.

## Using Directory.Build.props

To share common settings across multiple file-based apps, use a `Directory.Build.props` file.

**Directory.Build.props:**
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

Settings are applied to all file-based apps run in the folder (or parent folders) where this file exists.

## Using Directory.Packages.props

To centrally manage NuGet package versions, use `Directory.Packages.props`.

**Directory.Packages.props:**
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="System.CommandLine" Version="2.0.1" />
    <PackageVersion Include="Spectre.Console" Version="0.54.0" />
    <PackageVersion Include="PublicApiGenerator" Version="11.5.4" />
  </ItemGroup>
</Project>
```

With this setup, scripts can reference packages without specifying versions:
```csharp
#r "nuget: Spectre.Console"  // Version can be omitted
```

## FAQ

### Q1: Can File-based Apps be run with SDK versions prior to .NET 10?
**A**: No. File-based Apps (`dotnet MyScript.cs`) are a feature first introduced in .NET 10, so .NET 10.x or higher SDK is required. In earlier versions, you must create a project with `dotnet new console`.

### Q2: What are the advantages of using `Directory.Build.props` and `Directory.Packages.props` together?
**A**: `Directory.Build.props` applies common build settings like `TargetFramework` and `Nullable`, while `Directory.Packages.props` centrally manages NuGet package versions. Using both files together maintains **consistent build environments and package versions** across multiple file-based apps, preventing version conflicts between scripts.

### Q3: How do the `#r` directive and the `#:package` directive differ for referencing NuGet packages in file-based apps?
**A**: `#r "nuget: PackageName, Version"` is the format used in early previews, and `#:package PackageName@Version` is the new format at the time of .NET 10's official release. Both formats work, but `#:package` is the officially recommended format. The actual scripts in this tutorial use the `#:package` format.

### Q4: What is covered in the next section?
**A**: We introduce Claude Code, the AI tool that runs .NET 10 scripts. We will cover installation, basic usage, the concept of custom commands, and the structure of the `/release-note` command.

Now that the .NET 10 environment is ready, let's next look at Claude Code, the AI tool that runs these scripts.
