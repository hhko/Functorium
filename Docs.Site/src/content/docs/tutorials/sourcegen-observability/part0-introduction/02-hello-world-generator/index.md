---
title: "Hello World Generator"
---

A minimal IIncrementalGenerator implementation example.

## Project Structure

- `HelloWorldGenerator.Generator/` — Source generator (netstandard2.0)
- `HelloWorldGenerator.Usage/` — Console app that uses the generated code

## Run

```bash
dotnet run --project HelloWorldGenerator.Usage
```

---

## FAQ

### Q1: Why must the Hello World generator target `netstandard2.0`?
**A**: Source generators run inside the Roslyn compiler, which can only load `netstandard2.0` assemblies. If you target `net8.0` or `net10.0`, the compiler will not recognize the generator assembly.

### Q2: Why must the generator project and the usage project be separated?
**A**: Since source generators function as extensions of the compiler, including the generator code in the compilation target project would create a circular dependency. They must be in a separate project and referenced with `OutputItemType="Analyzer"` for the compiler to correctly recognize the generator.

### Q3: Where can the generated code be viewed?
**A**: In Visual Studio's Solution Explorer, expand Dependencies > Analyzers > generator project name to directly open `.g.cs` files. Alternatively, add `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` to the project file to output files to disk.
