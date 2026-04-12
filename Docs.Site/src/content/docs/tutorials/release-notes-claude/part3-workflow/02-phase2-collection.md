---
title: "Data Collection"
---

Now that the environment is ready, it is time to collect the raw data that forms the basis of the release notes. In Phase 2, two C# scripts are executed sequentially to extract per-component changes and the complete Public API. The data created at this stage serves as the foundation for all subsequent analysis and document writing, so accurate and complete collection is important.

All work is performed from the script directory.

```bash
cd .release-notes/scripts
```

## Step 1: Component Change Analysis

The first script, `AnalyzeAllComponents.cs`, explores Git history to analyze which files changed and which commits occurred for each component (project). The Base/Target range determined in Phase 1 is passed as arguments.

For a first deployment, analysis starts from the initial commit.

```bash
# Find initial commit SHA
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)

# Analyze from initial commit to current
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

For a subsequent release, the previous release branch is used as the baseline.

```bash
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
```

This script generates two types of results.

**Individual component analysis files** (`.analysis-output/*.md`) are generated one per component. Each file contains overall change statistics such as the number of added/modified/deleted files, the complete commit history for that component, contributor information, and commit lists classified by feature/bug fix/Breaking Changes. In Phase 3, these files serve as the primary input when analyzing commits and extracting features.

**Analysis summary** (`.analysis-output/analysis-summary.md`) is a high-level overview of all component changes. It shows the number of changed files per component and the list of generated analysis files at a glance.

## Step 2: API Change Extraction

The second script, `ExtractApiChanges.cs`, builds the projects and extracts Public APIs from the DLLs. If Step 1 tells you "what changed", Step 2 tells you "exactly what the current API looks like."

```bash
dotnet ExtractApiChanges.cs
```

This script generates three key results.

**The Uber API file** (`all-api-changes.txt`) is the **Single Source of Truth** of this workflow. Generated in the `.analysis-output/api-changes-build-current/` directory, it contains all Public API definitions of the current build with exact parameter names and types. When writing code examples in Phase 4, APIs not in this file are not documented. This is the most important mechanism for preventing non-existent APIs from being included in the release notes.

**The API change Diff** (`api-changes-diff.txt`) is the Git diff of the `.api` folder, used for automatic Breaking Changes detection. It can objectively identify deleted or signature-changed APIs, catching Breaking Changes that might be missed from commit messages alone.

**Individual API files** (`Src/*/.api/*.cs`) define each assembly's Public API in C# source code format. They are tracked by Git to manage API change history.

## Output Structure

The complete file structure generated after script execution.

```txt
.release-notes/scripts/
└── .analysis-output/
    ├── analysis-summary.md          # Overall summary
    ├── Functorium.md                # Src/Functorium analysis
    ├── Functorium.Testing.md        # Src/Functorium.Testing analysis
    ├── Docs.md                      # Docs analysis
    └── api-changes-build-current/
        ├── all-api-changes.txt      # Uber file (all APIs)
        ├── api-changes-summary.md   # API summary
        └── api-changes-diff.txt     # API differences
```

## Component Analysis File Structure

Let's look at the format of each component file.

````markdown
# Analysis for Src/Functorium

Generated: 2025-12-19 10:30:00
Comparing: origin/release/1.0 -> HEAD

## Change Summary
[git diff --stat output]

## All Commits
[Commit SHA and message list]

## Top Contributors
[Commits per contributor]

## Categorized Commits

### Feature Commits
[feat, feature, add pattern commits]

### Bug Fixes
[fix, bug pattern commits]

### Breaking Changes
[breaking, BREAKING, !: pattern commits]
````

## Commit Classification Patterns

`AnalyzeAllComponents.cs` automatically classifies commits using Conventional Commits patterns. More sophisticated analysis occurs in Phase 3, but the initial classification at this stage provides the foundational data.

### Feature Commits

Search keywords: `feat`, `feature`, `add`

```txt
Examples:
- feat: Add user authentication
- add: Logging feature
- feature(api): New endpoint
```

### Bug Fixes

Search keywords: `fix`, `bug`

```txt
Examples:
- fix: Handle null reference exception
- bug: Fix memory leak
```

### Breaking Changes

Search conditions (OR):
1. Contains `breaking` or `BREAKING` string
2. `!` pattern after type (e.g., `feat!:`, `fix!:`)

```txt
Examples:
- feat!: Change API response format
- feat!: BREAKING CHANGE: API format change
- fix: breaking: Compatibility change
```

### Other Conventional Commits Types

| Type | Description | Included in Release Notes |
|------|-------------|:------------------------:|
| `docs` | Documentation change | Usually omitted |
| `refactor` | Refactoring | Usually omitted |
| `perf` | Performance improvement | Included |
| `test` | Add/modify tests | Omitted |
| `build` | Build system change | Usually omitted |
| `chore` | Other changes | Omitted |
| `ci` | CI configuration change | Omitted |

## Data Collection Verification

After script execution is finished, verify that the results were properly generated.

### Component Analysis Verification

```bash
# Check component file count
ls -1 .analysis-output/*.md | wc -l

# Verify main components exist
ls .analysis-output/Functorium*.md

# Check analysis summary
cat .analysis-output/analysis-summary.md
```

### API Change Verification

```bash
# Verify Uber file existence and size
wc -l .analysis-output/api-changes-build-current/all-api-changes.txt

# Check key APIs (example)
grep -c "ErrorCodeFactory" .analysis-output/api-changes-build-current/all-api-changes.txt

# Check API files
ls Src/*/.api/*.cs
```

## Console Output Format

### Data Collection Success

```txt
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Phase 2: Data Collection Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Generated component analysis files:
  analysis-summary.md
  Functorium.md (31 files, 19 commits)
  Functorium.Testing.md (18 files, 13 commits)
  Docs.md (38 files, 37 commits)

Generated API files:
  all-api-changes.txt (Uber file)
  api-changes-summary.md
  api-changes-diff.txt
  Src/Functorium/.api/Functorium.cs
  Src/Functorium.Testing/.api/Functorium.Testing.cs

Location: .release-notes/scripts/.analysis-output/
```

## Error Handling

### AnalyzeAllComponents.cs Failure

```txt
Script execution failed: AnalyzeAllComponents.cs

Error: <error message>

Troubleshooting:
  1. Delete .analysis-output folder and retry
     rmdir /s /q .analysis-output  (Windows)
     rm -rf .analysis-output       (Linux/Mac)

  2. Clear NuGet cache
     dotnet nuget locals all --clear

  3. Terminate dotnet processes (Windows)
     taskkill /F /IM dotnet.exe
```

### ExtractApiChanges.cs Failure

```txt
API extraction failed: ExtractApiChanges.cs

Error: <error message>

Possible causes:
  1. Build error: Project does not build
  2. Missing DLL: No build output
  3. No API: No public types

Resolution:
  1. Verify project build
     dotnet build -c Release

  2. Fix build errors and retry
```

## Important Notes

Data collection is performed **only once** at the start of the workflow. Running scripts again during document writing in Phase 4 can overwrite analysis results and break consistency. Since the Uber file is the single source of truth for all API verification, all documented APIs must exist in this file. Commit analysis results are provided on a feature basis and are directly used for structuring sections of the release notes.

## FAQ

### Q1: Does the execution order of `AnalyzeAllComponents.cs` and `ExtractApiChanges.cs` matter?
**A**: Yes. `AnalyzeAllComponents.cs` is run first to collect per-component commit history, then `ExtractApiChanges.cs` is run to extract the current build's Public API. While the two scripts generate data for different purposes, **both results must be analyzed together in Phase 3** to write complete release notes.

### Q2: What problems occur if data collection is re-run during the workflow?
**A**: Files in the `.analysis-output/` folder are overwritten, **breaking data consistency.** The commit list already analyzed in Phase 3 and the Uber file referenced in Phase 4 may differ, so data collection should be performed **only once** at the start of the workflow.

### Q3: What happens if an API not in the Uber file is included in the release notes?
**A**: It is detected as an "API accuracy error" in Phase 5 validation. Since the Uber file is extracted from compiled DLLs, APIs not in it either do not exist or have internal access level. Documenting them would cause users to encounter compilation errors when trying to use non-existent APIs.

Once data collection is complete, proceed to [Phase 3: Commit Analysis](03-phase3-analysis.md) to transform the raw data into meaningful features.
