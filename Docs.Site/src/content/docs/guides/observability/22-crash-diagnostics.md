---
title: "Crash Dump Handler Guide"
---

Crashes in production environments can sometimes be difficult to diagnose with logs and metrics alone. This guide explains how to generate and analyze crash dumps for .NET applications using `Functorium.Abstractions.Diagnostics.CrashDumpHandler`.

## Introduction

Have you ever experienced a situation where a process terminated unexpectedly in production, but there was no trace in the logs? Exceptions like `StackOverflowException` or `AccessViolationException` that cannot be caught by `try-catch` terminate the process before the Observability pipeline can operate.

### What You Will Learn

1. **Role and initialization of CrashDumpHandler** - CSE (Corrupted State Exception) handling principles
2. **Deployment configuration per production environment** - Docker, Kubernetes, Windows services
3. **Dump file analysis methods** - Using dotnet-dump, Visual Studio, WinDbg
4. **Relationship with Observability** - Role separation between logs/metrics/tracing and crash dumps

### Prerequisites

- Basic understanding of .NET runtime exception handling
- Basic Docker/Kubernetes usage (for production deployment)
- [08-observability.md](../../spec/08-observability) -- Observability 3-Pillar specification

> **Core principle:** Crash dumps are a last resort for post-mortem analysis of CSE (Corrupted State Exception) that cannot be diagnosed with Observability 3-Pillar (Logging, Metrics, Tracing). Call `CrashDumpHandler.Initialize()` on the first line of `Program.cs` to capture crashes at all points in time.

## Summary

### Key Commands

```csharp
// Initialize on the first line of Program.cs
CrashDumpHandler.Initialize();

// Specify custom path
CrashDumpHandler.Initialize("/var/log/myapp/dumps");

// Query dump path
Console.WriteLine(CrashDumpHandler.DumpDirectory);
```

```bash
# Install and analyze with dotnet-dump
dotnet tool install -g dotnet-dump
dotnet-dump analyze crash.dmp

# Key analysis commands
> clrstack          # Stack trace
> pe                # Exception information
> dumpheap -stat    # Heap statistics
```

### Key Procedures

**1. Setup:**
1. Add `CrashDumpHandler.Initialize()` on the first line of `Program.cs`
2. Customize dump path if needed (environment variable or direct specification)

**2. Production Deployment:**
1. Set dump directory permissions and volume mount
2. Docker: Add `cap_add: SYS_PTRACE`
3. Kubernetes: Add `securityContext.capabilities.add: ["SYS_PTRACE"]`

**3. Dump Analysis:**
1. Collect `.dmp` files
2. Analyze with `dotnet-dump analyze` or Visual Studio
3. Identify root cause with `clrstack`, `pe`, `dumpheap`, etc.

### Key Concepts

| Concept | Description |
|------|------|
| CSE (Corrupted State Exception) | Exceptions that cannot be caught by `try-catch` (AccessViolation, StackOverflow, etc.) |
| Crash dump | Memory snapshot at the point of process termination |
| `MiniDumpWriteDump` | API used for dump generation on Windows |
| `createdump` | .NET dump generation tool on Linux/macOS |
| Source Link | Technology enabling source code-level debugging without PDB files |

---

## CrashDumpHandler Overview

`CrashDumpHandler` handles **Corrupted State Exceptions (CSE)** such as `AccessViolationException`. CSEs cannot be caught by `try-catch`, and the handler creates dumps just before process termination via the `AppDomain.UnhandledException` event.

| Exception Type | Catchable by try-catch | CrashDumpHandler Handles |
|-----------|:--------------:|:---------------------:|
| General Exception | O | O |
| AccessViolationException | X | O |
| StackOverflowException | X | O |
| ExecutionEngineException | X | O |

### Source Location

```
Src/Functorium/Abstractions/Diagnostics/CrashDumpHandler.cs
```

## Program.cs Configuration

`CrashDumpHandler.Initialize()` **must be called on the very first line** of `Program.cs`.

```csharp
using Functorium.Abstractions.Diagnostics;

// Initialize first (before any other code)
CrashDumpHandler.Initialize();

var builder = WebApplication.CreateBuilder(args);
// ... rest of the code
```

### Custom Path Specification

```csharp
// Explicit path specification (Linux/macOS)
CrashDumpHandler.Initialize("/var/log/myapp/dumps");

// Explicit path specification (Windows)
CrashDumpHandler.Initialize(@"C:\Logs\MyApp\Dumps");

// Using environment variable
var dumpDir = Environment.GetEnvironmentVariable("CRASH_DUMP_DIR");
CrashDumpHandler.Initialize(dumpDir);
```

### Default Dump Path

When the `dumpDirectory` parameter is omitted, `{LocalApplicationData}/{EntryAssemblyName}/CrashDumps` is used.

| Platform | Default Path Example |
|--------|---------------|
| Windows | `%LOCALAPPDATA%\MyApp\CrashDumps\` |
| Linux | `~/.local/share/MyApp/CrashDumps/` |
| macOS | `~/Library/Application Support/MyApp/CrashDumps/` |

### DumpDirectory Property

After initialization, the path can be queried via `CrashDumpHandler.DumpDirectory`.

```csharp
CrashDumpHandler.Initialize();
Console.WriteLine(CrashDumpHandler.DumpDirectory);
```

## Generated Files

### Mini Dump File (`.dmp`)

```
crash_AccessViolationException_20240115_143052.dmp
```

- Filename format: `crash_{ExceptionType}_{DateTime}.dmp`
- Windows: `MiniDumpWriteDump` (Full Memory)
- Linux/macOS: Uses `createdump` tool

### Exception Information File (`.txt`)

```
crash_info_20240115_143052.txt
```

Saves process information, exception details, stack traces, and Inner Exceptions as text.

With CrashDumpHandler setup complete in the local development environment, let us now learn about deployment configuration for safely collecting dumps in actual production environments (Docker, Kubernetes, Windows services).

## Production Deployment

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0

RUN mkdir -p /app/dumps && chmod 777 /app/dumps
VOLUME ["/app/dumps"]
ENV CRASH_DUMP_DIR=/app/dumps

WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

```yaml
# docker-compose.yml
services:
  api:
    environment:
      - CRASH_DUMP_DIR=/app/dumps
    volumes:
      - crash-dumps:/app/dumps
    cap_add:
      - SYS_PTRACE  # Required for createdump
```

### Kubernetes

```yaml
spec:
  containers:
  - name: myapp
    env:
    - name: CRASH_DUMP_DIR
      value: /dumps
    volumeMounts:
    - name: dump-volume
      mountPath: /dumps
    securityContext:
      capabilities:
        add: ["SYS_PTRACE"]
  volumes:
  - name: dump-volume
    persistentVolumeClaim:
      claimName: dump-pvc
```

### Windows Service (WER)

```powershell
# Configure Windows Error Reporting automatic dump
$werKey = "HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\MyApp.exe"
New-Item -Path $werKey -Force
Set-ItemProperty -Path $werKey -Name "DumpFolder" -Value "C:\Dumps\MyApp"
Set-ItemProperty -Path $werKey -Name "DumpType" -Value 2  # Full dump
Set-ItemProperty -Path $werKey -Name "DumpCount" -Value 10
```

Once dump files are collected, you can use the following tools to analyze the crash cause.

## Dump Analysis Tools

| Tool | Platform | Purpose |
|------|--------|------|
| Visual Studio | Windows | GUI-based analysis, native .NET support |
| WinDbg | Windows | Advanced debugging, script support |
| dotnet-dump | Cross-platform | CLI-based, suitable for container environments |
| lldb | Linux/macOS | Native debugging |

### dotnet-dump Key Commands

```bash
# Installation
dotnet tool install -g dotnet-dump

# Start analysis
dotnet-dump analyze crash.dmp

# Key commands
> clrstack          # Current thread stack trace
> clrstack -all     # All threads stack trace
> pe                # View exception information
> dumpheap -stat    # Heap statistics
> dumpobj <addr>    # Dump specific object
> gcroot <addr>     # Find GC roots
> threads           # Thread list
> syncblk           # Synchronization blocks (deadlock analysis)
```

### Visual Studio Analysis

1. Open `.dmp` file from `File > Open > File`
2. Click "Debug with Managed Only"
3. Check exception location in Call Stack window
4. Check variable values in Locals/Watch window

### Symbol (PDB) Management

| Analysis Level | PDB Required |
|-----------|:--------:|
| Basic stack trace (method names only) | X |
| Method name + line number | O |
| Debugging with source code | O + source |
| Variable/parameter values | O |
| Heap/memory analysis | X |

Source Link usage is recommended:

```xml
<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <DebugType>embedded</DebugType>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" Version="8.0.0" PrivateAssets="All"/>
</ItemGroup>
```

## Troubleshooting

### Dump File Not Generated

| Cause | Solution |
|------|------|
| Insufficient permissions | `chmod 755 /var/log/myapp/dumps` |
| Insufficient disk space | Clean old dumps: `find ... -mtime +7 -delete` |
| Crash before handler initialization | Call `Initialize()` on the first line of `Program.cs` |

### Cannot Open Dump File

| Cause | Solution |
|------|------|
| Bitness mismatch | Use 64-bit debugger for 64-bit dumps |
| Missing symbol files | `dotnet publish -c Release -p:DebugType=full` |

### Dump Generation Failure in Container

Docker: `cap_add: SYS_PTRACE` + `security_opt: seccomp:unconfined`
Kubernetes: `securityContext.capabilities.add: ["SYS_PTRACE"]`

## Relationship with Observability

Observability (Logging, Metrics, Tracing) is a tool for observing the behavior of a **running** process. Crash dumps are a post-mortem analysis tool for after a process **terminates abnormally**, which is fundamentally different in nature.

Recommended order for problem diagnosis:

1. **Logging** -- Check error codes and context with structured logs
2. **Metrics** -- Check trend changes in error rates, response times, etc.
3. **Tracing** -- Track bottlenecks/failure points in distributed request flows
4. **Crash dumps** -- Analyze process crashes that cannot be resolved with the above tools (last resort)

> For Observability specifications, see [08-observability.md](../../spec/08-observability).

## FAQ

### Q1. Why must `CrashDumpHandler.Initialize()` be on the first line of Program.cs?

CSE (Corrupted State Exception) can occur during application initialization. If a crash occurs during DI container configuration or middleware setup, dumps cannot be generated if the handler has not been registered. Initializing on the first line ensures crashes at all points in time can be captured.

### Q2. How large are crash dump files?

Since they are Full Memory dumps, the size is proportional to the process's memory usage. Typically they can range from hundreds of MB to several GB. In production environments, it is recommended to set up disk space monitoring and automatic cleanup of old dumps.

### Q3. Are Observability tools (logs, metrics, tracing) not sufficient?

Observability tools observe running processes. In cases where processes terminate abnormally, such as `AccessViolationException` or `StackOverflowException`, logs or traces may not be recorded. Crash dumps are a last resort for post-mortem analysis of the process state at the point of termination in such situations.

### Q4. Why is `SYS_PTRACE` permission required in Docker containers?

On Linux, the `createdump` tool reads another process's memory to generate dumps. This requires `ptrace` system call permission, which Docker blocks by default. `cap_add: SYS_PTRACE` must be added to enable dump generation.

### Q5. Can dumps be analyzed without PDB files?

Basic stack traces (method names) and heap/memory analysis are possible without PDB files. However, line numbers, source code-level debugging, and variable/parameter value inspection require PDB files. Source Link configuration is recommended, and `DebugType=embedded` can be used to include PDB in the assembly.

---

## References

- Original detailed document: `Tests.Hosts/01-SingleHost/CRASH-DUMP.md`
- [Microsoft: Collect and analyze memory dumps](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dumps)
- [dotnet-dump official documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump)
