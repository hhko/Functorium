---
title: "Troubleshooting"
---

This appendix compiles common problems and solutions encountered during source generator development. For detailed information about debugging setup, refer to [Part 1-03. Debugging Setup](../Part1-Fundamentals/03-Debugging-Setup/).

---

## Build/Generation Issues

| Symptom | Cause | Solution |
|---------|-------|----------|
| Generated code is not visible | Previous results remain in build cache | Delete `bin/obj` folders and rebuild |
| Code changes are not reflected | IDE is caching the previous generator DLL | Fully close Visual Studio → delete `bin/obj` → restart |
| Compile error: duplicate type definition | `OutputItemType` missing from project reference | Verify `OutputItemType="Analyzer"` |
| Generator does not run | Missing `[Generator]` attribute or `IIncrementalGenerator` not implemented | Verify `[Generator(LanguageNames.CSharp)]` on generator class |

```powershell
# Batch delete bin/obj
Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force
```

---

## Debugging Issues

| Symptom | Cause | Solution |
|---------|-------|----------|
| Breakpoints not working (hollow circle) | Build cache and source mismatch | Clear cache → `dotnet clean` → rebuild |
| `Debugger.Launch()` popup doesn't appear | Release build or `#if DEBUG` condition not met | Verify Debug build configuration, check `AttachDebugger: true` setting |
| Cannot step into generator from tests | Test project has `ReferenceOutputAssembly="false"` | Change to `ReferenceOutputAssembly="true"` |
| IDE not selected after `Debugger.Launch()` | Multiple Visual Studio instances running | Close all but one instance |

---

## Test Issues

| Symptom | Cause | Solution |
|---------|-------|----------|
| Verify snapshot mismatch | Generated code has changed (intentionally or not) | Review changes and run `Build-VerifyAccept.ps1` |
| Required assembly missing in `CSharpCompilation` | Insufficient reference types in `RequiredTypes` array | Add needed types to `SourceGeneratorTestRunner`'s `RequiredTypes` |
| `NullReferenceException` in tests | Generator did not produce code | Verify `[GenerateObservablePort]` and `IObservablePort` implementation in input source |
| Diagnostic test failure | Using `Generate` instead of `GenerateWithDiagnostics` | Use `GenerateWithDiagnostics` method for diagnostic verification |

```powershell
# Approve all pending snapshots
./Build-VerifyAccept.ps1
```

---

## Performance Issues

| Symptom | Cause | Solution |
|---------|-------|----------|
| Slow build | Incremental caching not working | Verify data model is `record struct` (value equality required) |
| IDE unresponsive | `Debugger.Launch()` left in `true` state | Restore to `AttachDebugger: false` |
| Build delay in large projects | Generator traversing all Syntax Nodes | Narrow filtering scope with `ForAttributeWithMetadataName` |

---

## Debugging Tips

### Checking Generated Code

You can directly view generated code in Visual Studio Solution Explorer:

```
Solution Explorer
→ Dependencies
→ Analyzers
→ Functorium.SourceGenerators
→ Functorium.SourceGenerators.ObservablePortGenerator
   → GenerateObservablePortAttribute.g.cs
   → Repositories.UserRepositoryObservable.g.cs
```

### Useful Watch Expressions

Expressions useful for understanding source generator internal state during debugging:

```csharp
// Full class name
classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
// → "global::MyApp.Adapters.UserRepository"

// All interfaces
classSymbol.AllInterfaces.Select(i => i.Name).ToArray()
// → ["IUserRepository", "IObservablePort"]

// Method signature
method.ToDisplayString()
// → "GetUserAsync(int)"

// Return type
method.ReturnType.ToDisplayString()
// → "LanguageExt.FinT<LanguageExt.IO, User>"
```

### Build Log Analysis

```bash
# Generate detailed log
dotnet build MyProject.csproj -v:diag > build.log

# Search for source generator related logs
grep -i "sourcegenerator" build.log
```

---

## Further Reading

→ [Part 1-03. Debugging Setup](../Part1-Fundamentals/03-Debugging-Setup/)
