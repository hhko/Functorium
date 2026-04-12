---
title: "component-priority.json"
---

A project has various folders such as source code, tests, and documentation—which folders should be targeted for analysis? Analyzing all folders introduces unnecessary noise and can bury changes in the core library. component-priority.json is the configuration file that answers this question. It **defines the analysis target components and their priorities,** determining what content appears in the release notes and in what order.

This file is located at `.release-notes/scripts/config/component-priority.json`.

## File Structure

The file content is simple. It lists the folder paths to analyze in a single array called `analysis_priorities`.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs",
    ".release-notes/scripts"
  ]
}
```

## Property Description

### analysis_priorities

`analysis_priorities` is an array of folder paths to analyze. Paths are relative to the Git repository root, and forward slashes (`/`) are used even on Windows. The casing must match the actual folder names.

## Examples

The most basic configuration analyzes only the core libraries.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing"
  ]
}
```

If you want to include documentation changes in the release notes, add the Docs folder.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Docs"
  ]
}
```

For larger projects, you can list multiple projects.

```json
{
  "analysis_priorities": [
    "Src/Core",
    "Src/Api",
    "Src/Web",
    "Src/Infrastructure",
    "Tests/UnitTests",
    "Tests/IntegrationTests"
  ]
}
```

---

## Priority Behavior

The order of the array is not just a listing. **The order is the priority.** Components listed earlier in the array are analyzed first and appear first in the output files.

```json
{
  "analysis_priorities": [
    "Src/Functorium",        // Priority 1 - analyzed and output first
    "Src/Functorium.Testing", // Priority 2
    "Docs"                    // Priority 3 - last
  ]
}
```

This order is directly reflected in the listing order of analysis output files.

```txt
.analysis-output/
├── Functorium.md          # Priority 1 component
├── Functorium.Testing.md  # Priority 2 component
├── Docs.md                # Priority 3 component
└── analysis-summary.md    # Summary (listed in priority order)
```

The "New Features" section of the release notes also follows the same order. The core library appears first, and supplementary content is placed later.

```markdown
## New Features

### Functorium Library     (Priority 1)
...

### Functorium.Testing Library  (Priority 2)
...

### Docs                      (Priority 3)
...
```

## Handling Missing Folders

If the configuration file is missing or empty, default values are applied.

```csharp
// Default values in AnalyzeAllComponents.cs
if (components.Count == 0)
{
    components = new List<string>
    {
        "Src/Functorium",
        "Src/Functorium.Testing",
        "Docs"
    };
}
```

Even if a non-existent folder is included in the array, it causes no issues. It is automatically skipped.

---

## Adding a New Project

Adding a new project to the analysis targets is a three-step process.

First, verify that the project folder you want to add actually exists.

```bash
# Check project folders
ls Src/

# Example output:
# Functorium/
# Functorium.Testing/
# Functorium.Web/       <- New project to add
```

Once verified, add the path to the configuration file.

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Src/Functorium.Testing",
    "Src/Functorium.Web"
  ]
}
```

Then run the analysis, and a result file for the new component will be generated.

```bash
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

```txt
.analysis-output/
├── Functorium.md
├── Functorium.Testing.md
├── Functorium.Web.md      <- Newly created
└── analysis-summary.md
```

## Advanced Configuration Examples

You can configure for various project structures. For a monorepo structure, list packages and apps separately.

```json
{
  "analysis_priorities": [
    "packages/core",
    "packages/ui",
    "packages/api",
    "apps/web",
    "apps/mobile",
    "docs"
  ]
}
```

For a microservices structure, list by service.

```json
{
  "analysis_priorities": [
    "services/auth",
    "services/user",
    "services/order",
    "services/payment",
    "shared/common",
    "shared/contracts"
  ]
}
```

If you want to focus analysis on specific components, simply keep only the necessary folders. For example, to exclude tests:

```json
{
  "analysis_priorities": [
    "Src/Functorium",
    "Docs"
  ]
}
```

## Output File Naming Rules

Output file names are automatically generated from folder paths. The last part of the path becomes the file name, and slashes are converted to hyphens.

| Folder Path | Output File Name |
|-----------|------------|
| `Src/Functorium` | `Functorium.md` |
| `Src/Functorium.Testing` | `Functorium.Testing.md` |
| `Docs` | `Docs.md` |
| `packages/core` | `packages-core.md` |

---

component-priority.json controls analysis targets and their order through a single `analysis_priorities` array. The file is located at `.release-notes/scripts/config/component-priority.json`, and the array order determines the analysis and output priority. If no configuration exists, Functorium, Functorium.Testing, and Docs are applied as defaults. It is just a simple JSON file, but it plays an important role in determining what content appears in the release notes and in what order.

## FAQ

### Q1: Does the order of the `analysis_priorities` array directly affect the feature listing order in the release notes?
**A**: Yes. Components listed earlier in the array **appear first in both the analysis output files and the "New Features" section of the release notes.** Placing the core library first allows users to see the most important changes first.

### Q2: Does including a non-existent folder in the array cause an error?
**A**: No. `AnalyzeAllComponents.cs` **automatically skips non-existent folders.** Since the Git diff and commits for that component will be empty, no analysis file is generated, but the analysis of the remaining components proceeds normally.

### Q3: Can Windows backslashes (`\`) be used in configuration file paths?
**A**: No. Paths must use **forward slashes (`/`).** Git commands interpret paths based on forward slashes, and Git correctly handles forward-slash paths even on Windows. Using backslashes may cause Git diff or log commands to fail to find the specified path.

### Q4: Should the Tests folder be included in `analysis_priorities`?
**A**: Generally, it should not be included. Release notes are documents that convey **changes to the libraries provided to users,** so test code changes are mostly unnecessary noise. However, test libraries like `Functorium.Testing` that are provided to users as test tools are appropriate to include.

## Next Step

- [Output File Formats](09-output-formats.md)
