---
title: "Troubleshooting Guide"
---

When running release note automation, you may encounter unexpected errors. Most problems occur in one of three areas: environment setup, script execution, or Phase progression, and understanding the cause allows for quick resolution.

This section explains **why** each problem occurs—the root cause—and provides the corresponding solution.

## Environment-Related Issues

Environment issues are revealed first in Phase 1. They occur when the required tools or paths are not in place before the scripts run.

### .NET SDK Not Found

```txt
Error: .NET 10 SDK is required
Cannot execute 'dotnet --version' command.
```

The release note scripts use .NET 10's File-based App feature. Since this feature was first introduced in .NET 10, earlier versions of the SDK cannot directly execute `.cs` files.

**Solution:**
1. Install .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
2. Check the PATH environment variable
3. Restart the terminal and verify:
   ```bash
   dotnet --version
   ```

### Not a Git Repository

```txt
Error: Not a Git repository
```

Release note generation starts with analyzing Git commit history. If you run the command in a location without a `.git` directory, commits cannot be read, causing this error.

**Solution:**
```bash
# Navigate to the Git repository root
cd /path/to/your/project

# Verify Git status
git status
```

### Scripts Directory Not Found

```txt
Error: Cannot find release note scripts
'.release-notes/scripts' directory does not exist.
```

The C# scripts to be executed in Phase 2 must be in the `.release-notes/scripts/` folder. If this folder is missing, it may have been excluded when cloning the repository initially, or you may be on a different branch.

**Solution:**
```bash
# Check from the project root
ls -la .release-notes/scripts/

# If the directory is missing, fetch from the repository
git checkout origin/main -- .release-notes/
```

## Script Execution Issues

Cases where the environment is fine but the script itself fails to execute. NuGet package issues, file locks, and build errors are the main causes.

### NuGet Package Restore Failure

```txt
error: Unable to resolve package 'System.CommandLine@2.0.1'
```

File-based Apps automatically restore NuGet packages at execution time. This error appears when package download fails due to network issues or cache corruption.

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Retry
dotnet AnalyzeAllComponents.cs --base HEAD~10 --target HEAD
```

### File Lock Error

```txt
The process cannot access the file because it is being used by another process.
```

A previous script execution may have terminated abnormally, leaving a dotnet process holding files in the `.analysis-output/` folder. This occurs particularly often on Windows.

**Solution:**
```bash
# On Windows, terminate dotnet processes
taskkill /F /IM dotnet.exe

# Delete the .analysis-output folder
rm -rf .release-notes/scripts/.analysis-output/

# Retry
```

### Build Failure

```txt
error CS1002: ; expected
Build FAILED.
```

The API extraction script (`ExtractApiChanges.cs`) builds the project to analyze the DLL. If there are compilation errors in the project code, the DLL cannot be generated and API extraction also fails.

**Solution:**
```bash
# First verify the project builds
dotnet build -c Release

# Fix build errors and retry
```

## Phase-Specific Issues

Issues that occur during specific Phases of the 5-Phase workflow. Which Phase it stopped at is the first clue for identifying the cause.

### Phase 1: Base Branch Not Found

```txt
Base branch origin/release/1.0 does not exist
```

For subsequent releases, the previous release branch is used as the Base. For first deployments, this branch does not yet exist, so this error may appear. The command automatically sets the initial commit as the Base, but for manual execution, you need to specify it directly.

**Solution (first deployment):**
```bash
# Analyze from the initial commit
cd .release-notes/scripts
FIRST_COMMIT=$(git rev-list --max-parents=0 HEAD)
dotnet AnalyzeAllComponents.cs --base $FIRST_COMMIT --target HEAD
```

**Solution (use a different branch):**
```bash
dotnet AnalyzeAllComponents.cs --base origin/main --target HEAD
```

### Phase 2: API Extraction Failure

```txt
API extraction failed: ExtractApiChanges.cs
DLL not found
```

`ExtractApiChanges.cs` extracts Public APIs from the built DLL. If the Release build has not been performed yet or the build output path is different, it cannot find the DLL.

**Solution:**
```bash
# Build the project
dotnet build -c Release

# Verify build output
ls Src/Functorium/bin/Release/net10.0/

# Retry API extraction
dotnet ExtractApiChanges.cs
```

### Phase 3: Analysis Files Not Found

```txt
Cannot find analysis files
.analysis-output/*.md files are missing
```

Phase 3 uses the output of Phase 2 (component analysis files) as input. If Phase 2 failed or the output folder is empty, Phase 3 cannot proceed.

**Solution:**
```bash
# Re-run Phase 2
cd .release-notes/scripts
dotnet AnalyzeAllComponents.cs --base origin/release/1.0 --target HEAD
dotnet ExtractApiChanges.cs

# Verify files
ls .analysis-output/
```

### Phase 4: Uber File Not Found

```txt
Cannot find Uber file
all-api-changes.txt is missing
```

The Uber file contains all the Public APIs of the project and is used when Phase 4 writes the API section of the release notes and when Phase 5 validates accuracy. Since `ExtractApiChanges.cs` generates this file, that script must be run first.

**Solution:**
```bash
# Run ExtractApiChanges.cs
cd .release-notes/scripts
dotnet ExtractApiChanges.cs

# Verify
cat .analysis-output/api-changes-build-current/all-api-changes.txt
```

### Phase 5: Validation Failure

```txt
Phase 5: Validation Failed
API Accuracy (2 errors)
```

This occurs when API signatures described in the release notes differ from the contents of the actual Uber file. Claude may have described API names or parameters slightly differently while writing the document.

**Solution:**
1. Identify the problematic APIs from the error message
2. Search for the correct signature in the Uber file:
   ```bash
   grep "MethodName" .analysis-output/api-changes-build-current/all-api-changes.txt
   ```
3. Fix the release notes
4. Re-run validation

## Claude Code-Related Issues

Issues that occur with Claude Code itself, which runs the workflow.

### Command Not Recognized

```txt
Command not found: /release-note
```

Claude Code custom commands are defined in the `.claude/commands/` folder. If this folder or the `release-note.md` file is missing, the command will not be recognized.

**Solution:**
```bash
# Check the .claude/commands/ folder
ls .claude/commands/

# Verify the release-note.md file exists
cat .claude/commands/release-note.md
```

### Version Parameter Missing

```txt
Error: Version parameter is required
```

The `/release-note` command requires a version string as a mandatory argument.

**Solution:**
```bash
# Correct usage
> /release-note v1.0.0
```

### Context Exceeded

In large projects, there may be so many commits and files to analyze that Claude's context window is exceeded. Symptoms include responses stopping midway or incomplete results.

**Solution:**
- Split the conversation into parts
- Request Phase by Phase separately
- Start a new conversation

## Output File Issues

### Character Encoding Issues

If characters appear garbled in the release notes, it's a file encoding issue. The generated file may have been saved in an encoding other than UTF-8.

**Solution:**
- Save the file in UTF-8 encoding
- Check editor encoding settings

### Markdown Rendering Errors

If code blocks or tables are not displayed correctly, it's a Markdown syntax error. Common causes include mismatched backtick counts and missing table alignment characters.

**Solution:**
```bash
# Run Markdown lint
npx markdownlint-cli@0.45.0 .release-notes/RELEASE-v1.0.0.md

# Fix errors
```

## General Troubleshooting Checklist

When individual problems above don't solve the issue, follow this checklist in order. The approach is to verify the most basic environment first, clean the cache, and then try running scripts individually. Most problems reveal their cause within these five steps.

1. **Verify Environment**
   ```bash
   dotnet --version    # .NET 10.x?
   git status          # Git repository?
   pwd                 # Correct directory?
   ```

2. **Clear Cache**
   ```bash
   dotnet nuget locals all --clear
   rm -rf .release-notes/scripts/.analysis-output/
   ```

3. **Build the Project**
   ```bash
   dotnet build -c Release
   ```

4. **Run Scripts Individually**
   ```bash
   cd .release-notes/scripts
   dotnet AnalyzeAllComponents.cs --help
   dotnet ExtractApiChanges.cs --help
   ```

5. **Check Logs**
   - Read the entire error message
   - Check the stack trace
   - Review generated file contents

## Getting Help

If the checklist doesn't resolve the issue, please refer to the following documentation or file an issue.

- `.release-notes/scripts/docs/README.md` - Script documentation
- `.claude/commands/release-note.md` - Command definition
- GitHub Issues: https://github.com/hhko/Functorium/issues

## FAQ

### Q1: Why does the "file lock error" occur particularly often on Windows?
**A**: Windows uses an exclusive lock policy where **another process cannot delete or overwrite a file** if a process has it open. If a previous script execution terminated abnormally, the dotnet process may remain holding `.analysis-output/` files, requiring `taskkill /F /IM dotnet.exe` to terminate the process.

### Q2: When Phase 5 validation shows an API accuracy error, should the Uber file or the release notes be corrected?
**A**: **The release notes should be corrected.** The Uber file is extracted from the actually built DLL and serves as the Single Source of Truth. If the API signature described in the release notes differs from the Uber file, the release notes are incorrect. Use `grep` to find the correct signature in the Uber file and then fix the release notes.

### Q3: What cache does `dotnet nuget locals all --clear` clean?
**A**: It deletes **all local caches used by NuGet,** including HTTP cache, global packages folder, and temporary folders. File-based Apps automatically restore NuGet packages at execution time, so if the cache is corrupted, package restoration can fail. After clearing, packages will be re-downloaded on the next execution.

### Q4: How should you respond when a context exceeded issue occurs?
**A**: In large projects with many commits and files, Claude's context window can be exceeded. **Splitting requests by Phase** is the most effective approach. For example, first run only Phase 2 data collection, then in a new conversation, proceed with Phases 3-5 based on the collected data to distribute the context burden.

This concludes the main text of the hands-on tutorial. The next section provides a quick reference guide consolidating the commands, workflow, and output files needed for release note generation.

- [Quick Reference](04-quick-reference.md)
