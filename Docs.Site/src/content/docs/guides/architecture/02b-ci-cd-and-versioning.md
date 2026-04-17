---
title: "CI/CD Workflows and Version Management"
---

This document explains CI/CD workflow configuration using GitHub Actions, NuGet package deployment methods, Git tag-based automatic version management using MinVer, and next version suggestion commands.

## Introduction

"How do you configure automatic NuGet package deployment when a tag is pushed?"
"What tools should you use to avoid manually managing build numbers and package versions?"
"What procedure does version progression follow from Pre-release to stable release?"

Manual version management and deployment are error-prone and lack reproducibility. Combining Git tag-based automatic version management with GitHub Actions CI/CD pipelines allows automating the entire process from code quality verification to package deployment.

### What You Will Learn

This document covers the following topics:

1. **Build/Publish workflow configuration** - CI on Push/PR, Release automation on tag push
2. **NuGet package configuration** - `Directory.Build.props` common metadata and SourceLink/symbol packages
3. **MinVer-based automatic version management** - Automatic SemVer version calculation from Git tags
4. **Version progression scenarios** - Alpha to Beta to RC to Stable release flow
5. **Conventional Commits-based version suggestion** - Automatic next version determination by commit type

### Prerequisites

A basic understanding of the following concepts is needed to understand this document:

- [Solution Configuration Guide](./02-solution-configuration) - `Directory.Build.props`, `Directory.Packages.props` structure
- Basic concepts of GitHub Actions workflows (trigger, jobs, steps)
- Semantic Versioning (SemVer 2.0) rules

> **The core of CI/CD and version management is** automating the entire process from version calculation to package deployment with a single Git tag, fundamentally eliminating manual management errors.

## Summary

### CI/CD Workflows

#### Key Commands

```bash
# CI auto-execution (Push to main or PR)
git push origin main

# Release deployment (tag push)
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0

# Local package creation
./Build-Local.ps1
dotnet pack -c Release -o .nupkg
```

#### Key Procedures

**1. Normal development (CI only):**
1. After code changes, `git push origin main` or create a PR
2. Build workflow auto-executes (build -> test -> coverage)

**2. Release deployment:**
1. `git tag -a v1.0.0 -m "Release 1.0.0"` Create tag
2. `git push origin v1.0.0` Push tag
3. Publish workflow auto-executes (build -> test -> package deployment -> GitHub Release)

#### Key Concepts

| Concept | Description |
|------|------|
| Build workflow | Build, test, coverage collection on PR/Push |
| Publish workflow | Build + package deployment + GitHub Release on tag push (v*.*.*) |
| NuGet metadata | Common settings in `Directory.Build.props`, project-specific settings in csproj |
| SourceLink + symbol packages | Step-into library source support during debugging |
| Deterministic Build | Ensures reproducible builds in CI environments |

### Version Management

#### Key Commands

```bash
# Check version
dotnet build -p:MinVerVerbosity=normal

# Create tag and release
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0

# Suggest next version
/suggest-next-version
/suggest-next-version alpha
```

#### Key Procedures

**1. From Pre-release to Stable:**
1. Alpha tag: `git tag v1.0.0-alpha.0`
2. Beta tag: `git tag v1.0.0-beta.0` (optional)
3. RC tag: `git tag v1.0.0-rc.0` (optional)
4. Stable release: `git tag v1.0.0`

**2. Next Patch release:**
1. Continue development after stable release (automatically displays `X.Y.Z+1-alpha.0.N`)
2. When ready, `git tag vX.Y.Z+1`

#### Key Concepts

| Concept | Description |
|------|------|
| MinVer | Git tag-based automatic version calculation MSBuild tool |
| Height | Number of commits since latest tag (auto-increment) |
| MinVerAutoIncrement | Auto-increment unit after RTM tag (patch/minor/major) |
| AssemblyVersion strategy | `Major.Minor.0.0` (Patch excluded -- prevents recompilation) |
| Conventional Commits | Version increment determined by commit type (feat/fix/feat!) |

---

## Overview

### Purpose

Automates stable builds and deployments through Git tag-based automatic version management and CI/CD pipelines.

### Workflow Configuration

The following table summarizes the two workflows and their respective trigger conditions.

| Workflow | Trigger | Main Tasks |
|-----------|--------|----------|
| CI (build.yml) | PR, Push to main | Build, test, coverage |
| Release (publish.yml) | Tag push (v*.*.*) | Build, test, package deployment |

### Generated Packages

| Package | Description |
|--------|------|
| `Functorium` | Functional domain framework for .NET |
| `Functorium.Testing` | Functorium test utilities |

### File Locations

```
ProjectRoot/
├── .github/
│   └── workflows/
│       ├── build.yml        # Build workflow (CI)
│       └── publish.yml      # Publish workflow (Release)
├── Directory.Build.props    # Common NuGet settings
├── Directory.Packages.props # Central package version management
├── Functorium.png              # Package icon (128x128 PNG)
├── .nupkg/                  # Generated package output directory
└── Src/
    ├── Functorium/
    │   └── Functorium.csproj
    └── Functorium.Testing/
        └── Functorium.Testing.csproj
```

---

## Workflow Configuration

### Build Workflow (build.yml)

**Trigger:**
- Pull Request to main
- Push to main branch
- Excludes documentation/script files (*.md, Docs/**, .claude/**, *.ps1)
- Manual execution (workflow_dispatch)

**Tasks:**
1. Code checkout
2. .NET 10 setup
3. NuGet package cache
4. Dependency restore
5. Vulnerable package inspection
6. Release mode build
7. Test execution and coverage collection
8. Upload test results
9. Generate coverage report with ReportGenerator
10. Display coverage summary (GITHUB_STEP_SUMMARY)
11. Upload coverage report

### Publish Workflow (publish.yml)

**Trigger:**
- Tag push: v*.*.* (e.g., v1.0.0, v1.2.3)

**Tasks:**
1. Perform all Build workflow tasks
2. Generate NuGet packages
3. Deploy to NuGet.org
4. Create GitHub Release

---

## Build Workflow

### Workflow File

`.github/workflows/build.yml`:

```yaml
name: Build

on:
  push:
    branches: [ main ]
    paths-ignore:
      - '**.md'
      - 'Docs/**'
      - '.claude/**'
      - '**.ps1'
  pull_request:
    branches: [ main ]
    paths-ignore:
      - '**.md'
      - 'Docs/**'
      - '.claude/**'
      - '**.ps1'
  workflow_dispatch:

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  CONFIGURATION: Release

jobs:
  build:
    name: Build and Test
    runs-on: ${{ matrix.os }}

    env:
      SOLUTION_FILE: ${{ github.workspace }}/Functorium.slnx
      COVERAGE_REPORT_DIR: ${{ github.workspace }}/coverage

    strategy:
      matrix:
        os: [ubuntu-24.04]
        dotnet: ['10.0.x']

    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet }}

    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-

    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}

    - name: Check for vulnerable packages
      run: |
        dotnet list ${{ env.SOLUTION_FILE }} package --vulnerable --include-transitive 2>&1 | tee ${{ github.workspace }}/vulnerability-report.txt
        if grep -q "has the following vulnerable packages" ${{ github.workspace }}/vulnerability-report.txt; then
          echo "::warning::Vulnerable packages detected. Review vulnerability-report.txt for details."
        fi

    - name: Build
      run: |
        dotnet build ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-restore \
          -p:MinVerVerbosity=normal

    - name: Test with coverage (MTP)
      run: |
        dotnet test \
          --solution ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-build \
          -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx

    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
        path: ${{ github.workspace }}/**/TestResults/**/*.trx
        retention-days: 30

    - name: Generate coverage report
      if: success()
      uses: danielpalme/ReportGenerator-GitHub-Action@v5.4.4
      with:
        reports: '${{ github.workspace }}/**/TestResults/**/coverage.cobertura.xml'
        targetdir: '${{ env.COVERAGE_REPORT_DIR }}'
        reporttypes: 'Html;Cobertura;MarkdownSummaryGithub'
        assemblyfilters: '-*.Tests.*'
        filefilters: '-*.g.cs'
        verbosity: 'Warning'

    - name: Display coverage summary
      if: success()
      run: |
        cat ${{ env.COVERAGE_REPORT_DIR }}/SummaryGithub.md >> $GITHUB_STEP_SUMMARY

    - name: Upload coverage report
      if: success()
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
        path: '${{ env.COVERAGE_REPORT_DIR }}'
        retention-days: 30
```

### Execution Methods

```bash
# Push to main - Build workflow auto-executes
git push origin main

# Pull Request - Build workflow auto-executes
gh pr create --base main --head feature/new-feature

# Manual execution - GitHub Actions tab > Build > Run workflow
```

---

## Publish Workflow

### Workflow File

`.github/workflows/publish.yml`:

```yaml
name: Publish

on:
  push:
    tags:
      - 'v*.*.*'

env:
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true
  CONFIGURATION: Release

permissions:
  contents: write

jobs:
  release:
    name: Build and Publish Release
    runs-on: ${{ matrix.os }}

    env:
      SOLUTION_FILE: ${{ github.workspace }}/Functorium.slnx
      COVERAGE_REPORT_DIR: ${{ github.workspace }}/coverage

    strategy:
      matrix:
        os: [ubuntu-24.04]
        dotnet: ['10.0.x']

    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ matrix.dotnet }}

    - name: Cache NuGet packages
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj', '**/Directory.Packages.props') }}
        restore-keys: |
          ${{ runner.os }}-nuget-

    - name: Restore dependencies
      run: dotnet restore ${{ env.SOLUTION_FILE }}

    - name: Check for vulnerable packages
      run: |
        dotnet list ${{ env.SOLUTION_FILE }} package --vulnerable --include-transitive 2>&1 | tee ${{ github.workspace }}/vulnerability-report.txt
        if grep -q "has the following vulnerable packages" ${{ github.workspace }}/vulnerability-report.txt; then
          echo "::warning::Vulnerable packages detected. Review vulnerability-report.txt for details."
        fi

    - name: Build
      run: |
        dotnet build ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-restore \
          -p:MinVerVerbosity=normal

    - name: Test with coverage (MTP)
      run: |
        dotnet test \
          --solution ${{ env.SOLUTION_FILE }} \
          --configuration ${{ env.CONFIGURATION }} \
          --no-build \
          -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --report-trx

    - name: Upload test results
      uses: actions/upload-artifact@v4
      if: always()
      with:
        name: test-results-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
        path: ${{ github.workspace }}/**/TestResults/**/*.trx
        retention-days: 30

    - name: Generate coverage report
      if: success()
      uses: danielpalme/ReportGenerator-GitHub-Action@v5.4.4
      with:
        reports: '${{ github.workspace }}/**/TestResults/**/coverage.cobertura.xml'
        targetdir: '${{ env.COVERAGE_REPORT_DIR }}'
        reporttypes: 'Html;Cobertura;MarkdownSummaryGithub'
        assemblyfilters: '-*.Tests.*'
        filefilters: '-*.g.cs'
        verbosity: 'Warning'

    - name: Display coverage summary
      if: success()
      run: |
        cat ${{ env.COVERAGE_REPORT_DIR }}/SummaryGithub.md >> $GITHUB_STEP_SUMMARY

    - name: Upload coverage report
      if: success()
      uses: actions/upload-artifact@v4
      with:
        name: coverage-report-${{ matrix.os }}-dotnet${{ matrix.dotnet }}
        path: '${{ env.COVERAGE_REPORT_DIR }}'
        retention-days: 30

    # - name: Pack NuGet packages
    #   run: |
    #     # Pack only Src projects (exclude Tests)
    #     for project in $(find ./Src -name "*.csproj" ! -name "*Tests*"); do
    #       echo "Packing: $project"
    #       dotnet pack "$project" \
    #         --configuration ${{ env.CONFIGURATION }} \
    #         --no-build \
    #         --output ./packages
    #     done

    # - name: List packages
    #   run: |
    #     echo "=== NuGet Packages ==="
    #     ls -la ./packages/*.nupkg 2>/dev/null || echo "No .nupkg files"
    #     echo ""
    #     echo "=== Symbol Packages ==="
    #     ls -la ./packages/*.snupkg 2>/dev/null || echo "No .snupkg files"

    # - name: Publish to NuGet.org
    #   if: startsWith(github.ref, 'refs/tags/v')
    #   run: |
    #     # Push NuGet packages (.nupkg)
    #     # Symbol packages (.snupkg) are automatically pushed when pushing to NuGet.org
    #     dotnet nuget push ./packages/*.nupkg \
    #       --api-key ${{ secrets.NUGET_API_KEY }} \
    #       --source https://api.nuget.org/v3/index.json \
    #       --skip-duplicate
    #   continue-on-error: true

    # - name: Publish to GitHub Packages
    #   if: startsWith(github.ref, 'refs/tags/v')
    #   run: |
    #     dotnet nuget push ./packages/*.nupkg \
    #       --api-key ${{ secrets.GITHUB_TOKEN }} \
    #       --source https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json \
    #       --skip-duplicate
    #   continue-on-error: true

    # Custom Release Notes Integration
    # Checks for .release-notes/RELEASE-{version}.md file generated by /release-note command
    # Falls back to auto-generated notes if file not found
    # Generate custom notes with: /release-note [version] [topic]

    - name: Extract version from tag
      id: version
      if: startsWith(github.ref, 'refs/tags/v')
      run: |
        # Extract version (e.g., refs/tags/v1.0.0 → v1.0.0)
        VERSION=${GITHUB_REF#refs/tags/}
        echo "version=$VERSION" >> $GITHUB_OUTPUT
        echo "Extracted version: $VERSION"

    - name: Check for custom release notes
      id: release_notes
      if: startsWith(github.ref, 'refs/tags/v')
      run: |
        VERSION=${{ steps.version.outputs.version }}
        RELEASE_NOTE_FILE=".release-notes/RELEASE-${VERSION}.md"

        if [ -f "$RELEASE_NOTE_FILE" ]; then
          echo "exists=true" >> $GITHUB_OUTPUT
          echo "file=$RELEASE_NOTE_FILE" >> $GITHUB_OUTPUT
          echo "✓ Found custom release notes: $RELEASE_NOTE_FILE"
        else
          echo "exists=false" >> $GITHUB_OUTPUT
          echo "⚠ No custom release notes found, will use auto-generated notes"
          echo "::notice::Release note file not found: $RELEASE_NOTE_FILE"
        fi

    # Delete existing release assets before creating/updating release
    # This prevents stale files from previous releases when re-releasing with same tag
    - name: Delete existing release assets
      if: startsWith(github.ref, 'refs/tags/v')
      run: |
        VERSION=${{ steps.version.outputs.version }}

        # Check if release exists
        if gh release view "$VERSION" &>/dev/null; then
          echo "Found existing release: $VERSION"

          # Get and delete all assets
          ASSETS=$(gh release view "$VERSION" --json assets -q '.assets[].name')
          if [ -n "$ASSETS" ]; then
            echo "Deleting existing assets..."
            echo "$ASSETS" | while read -r asset; do
              if [ -n "$asset" ]; then
                echo "  Deleting: $asset"
                gh release delete-asset "$VERSION" "$asset" --yes
              fi
            done
            echo "✓ All existing assets deleted"
          else
            echo "No existing assets to delete"
          fi
        else
          echo "No existing release found for $VERSION"
        fi
      env:
        GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}

    - name: Create GitHub Release
      uses: softprops/action-gh-release@v2
      if: startsWith(github.ref, 'refs/tags/v')
      with:
        # files: |
        #   ./packages/*.nupkg
        #   ./packages/*.snupkg
        body_path: ${{ steps.release_notes.outputs.exists == 'true' && steps.release_notes.outputs.file || '' }}
        generate_release_notes: ${{ steps.release_notes.outputs.exists != 'true' }}
        draft: false
        prerelease: ${{ contains(github.ref, '-') }}
        fail_on_unmatched_files: false
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

### Release Process

```bash
# 1. Create version tag
git tag -a v1.0.0 -m "Release 1.0.0"

# 2. Push tag (Release workflow auto-executes)
git push origin v1.0.0

# 3. Verify on GitHub
# - Actions tab: workflow execution status
# - Releases tab: created release
```

---

While workflows automate builds and deployments, NuGet package settings determine the metadata and debugging support for deployed packages.

## NuGet Package Settings

### Directory.Build.props Common Settings

NuGet metadata settings applied to all projects.

```xml
<!-- NuGet Package Common Settings -->
<PropertyGroup>
  <Authors>고형호</Authors>
  <Company>Functorium</Company>
  <Copyright>Copyright (c) Functorium Contributors. All rights reserved.</Copyright>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <PackageProjectUrl>https://github.com/hhko/Functorium</PackageProjectUrl>
  <RepositoryUrl>https://github.com/hhko/Functorium.git</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <PackageReadmeFile>README.md</PackageReadmeFile>
  <PackageIcon>Functorium.png</PackageIcon>
  <PackageTags>functorium;functional;dotnet;csharp</PackageTags>
</PropertyGroup>
```

### Symbol Package Settings

Symbol package generation settings for debugging support.

```xml
<!-- Symbol Package for Debugging -->
<PropertyGroup>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

- PDB included in `.snupkg` files
- Auto-uploaded to NuGet.org symbol server
- Step-into debugging available in Visual Studio

### SourceLink Settings

Links to GitHub source to support accessing original code during debugging.

```xml
<!-- Source Link for Debugging -->
<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.GitHub" PrivateAssets="All" />
</ItemGroup>
```

### Deterministic Build Settings

Settings for reproducible builds in CI environments.

```xml
<!-- Deterministic Build -->
<PropertyGroup>
  <ContinuousIntegrationBuild Condition="'$(GITHUB_ACTIONS)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

### Difference Between Symbol Package and SourceLink Roles

The following table compares what information symbol packages and SourceLink each provide during debugging.

| Feature | Role | Provided Information |
|------|------|----------|
| **Symbol packages (.snupkg)** | "Where" it is executing | Method names, line numbers, variable names |
| **SourceLink** | "What" is executing | Actual source code contents |

Both are needed to Step-into library source during debugging.

---

## Per-Project Package Settings

### Functorium.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>Functorium</PackageId>
    <Description>Functorium - A functional domain framework for .NET</Description>
    <PackageTags>$(PackageTags);functional-programming;domain-driven-design;ddd</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Functorium.png" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

### Functorium.Testing.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>Functorium.Testing</PackageId>
    <Description>Functorium.Testing - Testing utilities for a functional domain framework for .NET</Description>
    <PackageTags>$(PackageTags);testing;functional-programming;domain-driven-design;ddd</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Functorium.png" Pack="true" PackagePath="\" />
  </ItemGroup>

  <!-- Not a test project (produces NuGet package) -->
  <PropertyGroup>
    <IsTestProject>false</IsTestProject>
  </PropertyGroup>
</Project>
```

> **Note**: `IsTestProject` must be set to `false` to enable NuGet package generation.

### Adding a New Package Project

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <PackageId>Functorium.NewPackage</PackageId>
    <Description>Functorium.NewPackage - Description here</Description>
    <PackageTags>$(PackageTags);additional;tags</PackageTags>
  </PropertyGroup>

  <ItemGroup>
    <None Include="..\..\README.md" Pack="true" PackagePath="\" />
    <None Include="..\..\Functorium.png" Pack="true" PackagePath="\" />
  </ItemGroup>
</Project>
```

Required settings:
1. `PackageId`: Unique package name
2. `Description`: Package description
3. `PackageTags`: Common tags + additional tags
4. Package Files: Include README.md, Functorium.png

---

## Package Creation

### Using Build-Local.ps1 (Recommended)

```bash
# Full build and package creation
./Build-Local.ps1

# Skip package creation
./Build-Local.ps1 -SkipPack
```

### Using dotnet CLI Directly

```bash
# Full solution package creation
dotnet pack -c Release -o .nupkg

# Specific project only
dotnet pack Src/Functorium/Functorium.csproj -c Release -o .nupkg

# Specify version
dotnet pack -c Release -o .nupkg -p:Version=1.0.0
```

### Output Files

| File | Description |
|------|------|
| `*.nupkg` | NuGet package (for deployment) |
| `*.snupkg` | Symbol package (for debugging) |

---

## Package Verification

### Checking Package Contents

```bash
# Check package contents
unzip -l .nupkg/Functorium.1.0.0.nupkg

# dotnet CLI verification
dotnet nuget verify .nupkg/Functorium.1.0.0.nupkg
```

### Local Testing

```bash
# 1. Add local NuGet source
dotnet nuget add source .nupkg/ --name local

# 2. Test package installation
dotnet add package Functorium --source local

# 3. Verify build
dotnet build
```

---

## Initial Setup

### GitHub Secrets

| Secret | Purpose | Required | How to Obtain |
|--------|------|----------|----------|
| `NUGET_API_KEY` | NuGet.org deployment | Required for Release | NuGet.org > Account > API Keys |
| `CODECOV_TOKEN` | Codecov upload | Optional (currently disabled) | Codecov.io |

`GITHUB_TOKEN` is automatically provided (GitHub Packages, Release creation).

### How to Add GitHub Secrets

1. GitHub repository > **Settings** > **Secrets and variables** > **Actions**
2. Click **New repository secret**
3. Name: `NUGET_API_KEY`, Secret: [NuGet API Key value]
4. Click **Add secret**

### Creating NuGet.org API Key

1. NuGet.org login > Account > **API Keys**
2. Click **Create**
3. Settings: Key Name, Package Owner, Glob Pattern: `Functorium.*`, Scopes: **Push**
4. Click **Create** and copy the API Key

### Workflow Permission Settings

Settings > Actions > General:
- Workflow permissions: **Read and write permissions**
- Check **Allow GitHub Actions to create and approve pull requests**

---

With CI/CD pipelines and package settings in place, let us now look at MinVer-based version management that automatically determines package versions.

## Version Management Overview

### What is MinVer?

MinVer is an MSBuild tool that automatically calculates .NET project versions based on Git tags.

### Key Features

- **Tag-based**: Version management with Git tags only
- **Zero configuration**: Ready to use with just default settings
- **Fast speed**: Runs only minimal Git commands
- **SemVer 2.0**: Follows semantic versioning rules

### Comparison with Traditional Methods

The following table compares manual version management and MinVer automatic version management.

| Traditional Method | MinVer Method |
|----------|------------|
| Manually edit version files | Automatically calculated from Git tags |
| Risk of version mismatch | Always matches tags |
| Additional work at release | Just push the tag |

---

## Version Structure

### Full Structure

```
{Major}.{Minor}.{Patch}-{Identifier}.{Phase}.{Height}+{Commit}
    |      |      |         |         |        |        |
    |      |      |         |         |        |        +-- Commit hash (short)
    |      |      |         |         |        +----------- Height (auto-increment: commit count)
    |      |      |         |         +-------------------- Phase (manual change)
    |      |      |         +------------------------------ Identifier (manual change)
    |      |      +---------------------------------------- Patch (auto-increment*: after RTM tag)
    |      +----------------------------------------------- Minor
    +------------------------------------------------------ Major
```

### Element Descriptions

| Element | Description | Change Method |
|------|------|----------|
| **Major** | Incremented for breaking changes | Manual (tag) |
| **Minor** | Incremented for new features | Manual (tag) |
| **Patch** | Incremented for bug fixes | Manual (tag) / Automatic display* |
| **Identifier** | Pre-release stage (alpha, beta, rc) | Manual (tag) |
| **Phase** | Stage number within same Identifier (starts from 0) | Manual (tag) |
| **Height** | Number of commits since latest tag | Auto-increment |
| **Commit** | Short hash of current commit (build metadata) | Automatic |

\* After an RTM tag, automatically displays +1 according to the `MinVerAutoIncrement` setting

### Tag Format

```bash
vX.X.0-alpha.0   # First pre-release
vX.X.0           # First stable release
vX.X.1           # Next patch release
vX.Y.0           # Next minor release
vY.0.0           # Next major release
```

### Automatic vs Manual Increment

| Element | Increment Method | Trigger |
|------|----------|--------|
| **Height** | Auto-increment | On every commit |
| **Phase** | Manual change | Only when creating Git tags |
| **Identifier** | Manual change | Only when creating Git tags |
| **Patch/Minor/Major** | Automatic display | After RTM tag (actual change is tag only) |

---

## Installation and Configuration

### When Using Central Package Management

**Directory.Packages.props:**

```xml
<Project>
  <ItemGroup Label="Versioning">
    <PackageVersion Include="MinVer" Version="6.0.0" />
  </ItemGroup>
</Project>
```

**Directory.Build.props:**

```xml
<Project>
  <ItemGroup>
    <PackageReference Include="MinVer">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

### Recommended Settings (Current Project)

```xml
<PropertyGroup>
  <MinVerTagPrefix>v</MinVerTagPrefix>
  <MinVerVerbosity>minimal</MinVerVerbosity>
  <MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
  <MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
  <MinVerAutoIncrement>patch</MinVerAutoIncrement>
  <MinVerWorkingDirectory>$(MSBuildThisFileDirectory)</MinVerWorkingDirectory>
</PropertyGroup>

<!-- AssemblyVersion is set in MSBuild Target (executed after MinVer calculation) -->
<Target Name="SetAssemblyVersion" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

### Check Version Command

```bash
# Default build
dotnet build

# Detailed version information
dotnet build -p:MinVerVerbosity=normal

# Diagnostic information
dotnet build -p:MinVerVerbosity=diagnostic
```

---

## Key Configuration Options

### MinVerTagPrefix

```xml
<MinVerTagPrefix>v</MinVerTagPrefix>
```

| Setting Value | Recognized Tags | Ignored Tags |
|--------|---------|----------|
| `v` | v1.0.0, v2.0.0 | 1.0.0, ver1.0.0 |
| `ver` | ver1.0.0 | v1.0.0, 1.0.0 |
| (empty string) | 1.0.0 | v1.0.0 |

Recommended: `v` (GitHub standard)

### MinVerVerbosity

| Value | Output Content | When to Use |
|----|---------|----------|
| `minimal` | Warnings/errors only | Normal builds |
| `normal` | Version calculation process | Version verification |
| `diagnostic` | Detailed debug information | Troubleshooting |

### MinVerDefaultPreReleaseIdentifiers

Prerelease suffix used when there are no tags:

```xml
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
```

**Pre-release stages:**

| Stage | Meaning | When to Use |
|------|------|-----------|
| `alpha` | Alpha version | Early development, incomplete features, unstable (default) |
| `beta` | Beta version | Feature complete, under testing, fixing bugs |
| `rc` | Release Candidate | Release candidate, final testing |

Version comparison order: `alpha < beta < rc < (stable)`

### MinVerAutoIncrement

Auto-increment unit after an RTM tag:

| Value | Behavior | Example |
|----|------|------|
| `patch` (default) | Patch version +1 display | v1.0.0 → 1.0.1-alpha.0.1 |
| `minor` | Minor version +1 display | v1.0.0 → 1.1.0-alpha.0.1 |
| `major` | Major version +1 display | v1.0.0 → 2.0.0-alpha.0.1 |

> **Important:** This only increases the "display." Actual versions are changed only by Git tags.

### MinVerMinimumMajorMinor

Minimum version setting when there are no tags:

```xml
<MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>
```

`0.0.0` version prevention effect: even without tags, `1.0.0-alpha.0.N` uses

---

## Version Calculation Method

### When There Are No Tags

```bash
# Git history: 18 commits, no tags
# Calculated result: 0.0.0-alpha.0.18
```

### When There Are Tags (Height = 0)

```bash
# v1.0.0 tag on HEAD
# Calculated result: 1.0.0
```

### After Tag Commits (Height > 0)

```bash
# 5 commits after v1.0.0 tag
# Calculated result: 1.0.1-alpha.0.5
```

### Prerelease Tags

```bash
# v1.0.0-rc.1 tag
# Calculated result: 1.0.0-rc.1

# 1 commit after
# Calculated result: 1.0.0-rc.1.1
```

### When There Are Multiple Tags

Uses the nearest tag among ancestors of the current commit:

```bash
# 3 commits after v1.1.0 tag
# Calculated result: 1.1.1-alpha.0.3
```

---

## Version Progression Scenarios

### From Pre-release to Stable

```bash
# Alpha stage
git tag v25.13.0-alpha.0     # → 25.13.0-alpha.0
# 3 commits                    # → 25.13.0-alpha.0.1 ~ .3

# Beta stage
git tag v25.13.0-beta.0      # → 25.13.0-beta.0
# 2 commits                    # → 25.13.0-beta.0.1 ~ .2

# Release Candidate
git tag v25.13.0-rc.0        # → 25.13.0-rc.0
# 1 commit                    # → 25.13.0-rc.0.1

# Stable release
git tag v25.13.0             # → 25.13.0 (stable)
```

### Next Patch Version

```bash
# Continue development after v25.13.0 release
# (MinVerAutoIncrement=patch → Patch +1 automatic display)
# 2 commits                    # → 25.13.1-alpha.0.1 ~ .2

# Next Patch release
git tag v25.13.1             # → 25.13.1 (stable)
```

### Key Points

1. Height auto-increments on every commit
2. Phase can only be changed via Git tags
3. alpha -> beta -> rc progression is optional (stages can be skipped)

---

## Practical Use of Height

### Ensuring Build Uniqueness

Every build has a unique version number even without tags:

```bash
v1.0.0-alpha.0.3  # 3rd commit
v1.0.0-alpha.0.4  # 4th commit
v1.0.0-alpha.0.5  # 5th commit
```

Each version is clearly distinguishable in NuGet package managers, enabling CI/CD automatic deployment without version conflicts.

### Traceability

```bash
# Determine commit position from version
1.0.0-alpha.0.47
# → Immediately identify as "47 commits after alpha.0 tag"
```

### SemVer 2.0 Ordering Rules

```bash
1.0.0-alpha.0.5   <
1.0.0-alpha.0.6   <
1.0.0-alpha.1     <  (automatic "promotion" when tag is created)
1.0.0-alpha.1.1   <
1.0.0-alpha.2
```

### Philosophy

MinVer's philosophy is **"tags only for meaningful milestones, everything else is automatic."**

---

## Assembly Version Strategy

.NET assemblies have 3 version properties:

| Property | Purpose | Format | Example Value |
|------|------|------|---------|
| **AssemblyVersion** | Binary compatibility | Major.Minor.0.0 | 1.0.0.0 |
| **FileVersion** | File property display | Major.Minor.Patch.0 | 1.0.1.0 |
| **InformationalVersion** | Product version (user-facing) | Full SemVer | 1.0.1-alpha.0.5+abc123 |

### Why AssemblyVersion Does Not Include Patch

AssemblyVersion determines binary compatibility. If Patch is included, all referencing assemblies must be recompiled for every bug fix.

```xml
<!-- Recommended (current project settings) -->
<Target Name="SetAssemblyVersion" AfterTargets="MinVer">
  <PropertyGroup>
    <AssemblyVersion>$(MinVerMajor).$(MinVerMinor).0.0</AssemblyVersion>
  </PropertyGroup>
</Target>
```

**Examples:**

```bash
v1.0.0: AssemblyVersion=1.0.0.0, FileVersion=1.0.0.0
v1.0.1: AssemblyVersion=1.0.0.0, FileVersion=1.0.1.0  # No recompilation needed
v1.0.2: AssemblyVersion=1.0.0.0, FileVersion=1.0.2.0  # No recompilation needed
v1.1.0: AssemblyVersion=1.1.0.0, FileVersion=1.1.0.0  # Minor change - recompilation needed
```

### MSBuild Properties

| Property | Example Value | Description |
|------|---------|------|
| `$(MinVerVersion)` | 1.0.0 | Full SemVer version |
| `$(MinVerMajor)` | 1 | Major version |
| `$(MinVerMinor)` | 0 | Minor version |
| `$(MinVerPatch)` | 0 | Patch version |
| `$(MinVerPreRelease)` | alpha.0.5 | Prerelease part |
| `$(MinVerBuildMetadata)` | abc123 | Build metadata |

---

Now that we understand the version calculation method, let us finally look at the command that automatically suggests the next version from commit history.

## Suggest Next Version Command

### Overview

`/suggest-next-version` command analyzes Conventional Commits history and suggests the next release version tag according to Semantic Versioning.

### Usage

```bash
/suggest-next-version          # Suggest stable version
/suggest-next-version alpha    # Suggest Alpha version
/suggest-next-version beta     # Suggest Beta version
/suggest-next-version rc       # Suggest RC version
```

### Version Increment Rules (Conventional Commits)

| Commit Type | Version Increment | Example |
|-----------|-----------|------|
| `feat!`, `BREAKING CHANGE` | Major | v1.0.0 → v2.0.0 |
| `feat` | Minor | v1.0.0 → v1.1.0 |
| `fix`, `perf` | Patch | v1.0.0 → v1.0.1 |
| `docs`, `style`, `refactor`, `test`, `build`, `ci`, `chore` | None | No version increment needed |

Priority: `Major > Minor > Patch`

### Execution Procedure

1. **Check current version**: `git describe --tags --abbrev=0`
2. **Analyze commit history**: Classify commits since last tag
3. **Determine version increment**: Based on highest level of change
4. **Output results**: Display suggested version and git commands

### Output Example

```
Tag Suggestion Results

Current version: v1.2.3
Suggested version: v1.3.0

Reason for version increment:
  - 3 feat commits found (Minor increment)
  - 5 fix commits found

Tag creation commands:
  git tag v1.3.0
  git push origin v1.3.0
```

### Pre-release Support

| Type | Description | Version Example |
|------|------|----------|
| `alpha` | Alpha version (early development stage) | v1.3.0-alpha.0 |
| `beta` | Beta version (feature complete, testing stage) | v1.3.0-beta.0 |
| `rc` | Release Candidate | v1.3.0-rc.0 |

> **Note**: The `/suggest-next-version` command only makes suggestions. Actual tag creation requires the user to execute the commands directly.

---

## Troubleshooting

### When Workflows Do Not Execute

**Cause**: Workflow file syntax error or insufficient permissions

**Resolution:**
1. Validate YAML file (VS Code YAML extension or yamllint.com)
2. Check permissions in Settings > Actions > General

### When Version Shows as 0.0.0

```bash
# Check project Version property
dotnet build -v detailed | grep Version
```

### NuGet Deployment Failure

| Cause | Solution |
|------|------|
| No API Key | Check `NUGET_API_KEY` in GitHub Secrets |
| Duplicate package name | Change project name or request permissions from package owner |
| Insufficient API Key permissions | Check Push permissions and Glob Pattern on NuGet.org |

### When Version Shows as 0.0.0 in CI

When Git history is missing due to shallow clone:

```yaml
# GitHub Actions
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0  # Fetch full history
```

### README.md Not Included in Package

Add Pack settings to csproj:

```xml
<ItemGroup>
  <None Include="..\..\README.md" Pack="true" PackagePath="\" />
</ItemGroup>
```

### When Version Shows as 0.0.0-alpha.0.N

| Cause | Solution |
|------|------|
| No Git tag | `git tag -a v0.1.0 -m "Initial version"` |
| Tag prefix mismatch | Check `<MinVerTagPrefix>` setting |
| Minimum version not set | `<MinVerMinimumMajorMinor>1.0</MinVerMinimumMajorMinor>` |

### When Version Does Not Change Even After Creating a Tag

```bash
# Check tag format
git tag v1.0.0       # O - correct format
git tag 1.0.0        # X - no prefix
git tag v1.0         # X - missing Patch version

# Check tags on current branch
git tag --merged
```

### When MinVer Does Not Execute

```bash
# Rebuild after clearing cache
dotnet clean
dotnet nuget locals all --clear
dotnet restore
dotnet build
```

### Korean Path Issue

If the Git repository is in a path containing Korean characters, MinVer may fail to recognize the path. Move the project to an English-character path.

---

## FAQ

### Q1. Do I need to push a tag every time?

**A:** Push tags only when deploying releases.

```bash
# Normal development - CI only (build, test)
git commit -m "feat: new feature"
git push origin main

# Release - Release workflow execution (build, test, deploy)
git tag -a v1.0.0 -m "Release 1.0.0"
git push origin v1.0.0
```

### Q2. Can Preview versions also be deployed?

**A:** Yes, use prerelease tags. The workflow automatically detects prerelease:

```bash
git tag -a v1.0.0-rc.1 -m "Release Candidate 1"
git push origin v1.0.0-rc.1
```

```yaml
prerelease: ${{ contains(github.ref, '-') }}
```

### Q3. How to deploy only specific projects?

**A:** Specify the project in `dotnet pack`:

```yaml
- name: Pack NuGet packages
  run: |
    dotnet pack Src/Functorium/Functorium.csproj -c Release --no-build --output ./packages
    dotnet pack Src/Functorium.Testing/Functorium.Testing.csproj -c Release --no-build --output ./packages
```

### Q4. What if manual approval is needed before deployment?

**A:** Use GitHub Environment protection rules:

1. Settings > Environments > **New environment** (`production`)
2. Add **Required reviewers**
3. Add `environment: production` to publish.yml

### Q5. How to test across multiple .NET versions?

**A:** Matrix 빌드를 uses합니다:

```yaml
strategy:
  matrix:
    dotnet-version: ['8.0.x', '9.0.x', '10.0.x']
```

### Q6. 배포된 패키지를 어떻게 uses하나요?

**A:**

```bash
# Install from NuGet.org
dotnet add package Functorium --version 1.0.0

# Install pre-release
dotnet add package Functorium --prerelease
```

### Q7. How to determine PackageId?

**A:** 네임스페이스와 일치시키고, 소문자와 점(.)을 uses합니다:

```xml
<!-- Recommended -->
<PackageId>Functorium</PackageId>
<PackageId>Functorium.Testing</PackageId>
<PackageId>Functorium.Extensions.Http</PackageId>
```

### Q8. What if Directory.Build.props and csproj settings conflict?

**A:** csproj settings take priority:

```xml
<!-- Directory.Build.props -->
<PackageTags>functorium;functional</PackageTags>

<!-- Functorium.csproj - add tags -->
<PackageTags>$(PackageTags);ddd</PackageTags>
<!-- Result: functorium;functional;ddd -->
```

### Q9. What are the differences between MinVer and GitVersion?

**A:**

| Item | MinVer | GitVersion |
|------|--------|-----------|
| Complexity | Simple (tags only) | Complex (branch strategy) |
| Configuration | Minimal settings | Detailed configuration file needed |
| Branch strategy | Not supported | GitFlow, GitHub Flow, etc. |
| Speed | Fast | Relatively slow |

### Q10. How to change the Pre-release stage?

**A:** Change `MinVerDefaultPreReleaseIdentifiers`:

```xml
<MinVerDefaultPreReleaseIdentifiers>alpha.0</MinVerDefaultPreReleaseIdentifiers>
<MinVerDefaultPreReleaseIdentifiers>beta.0</MinVerDefaultPreReleaseIdentifiers>
<MinVerDefaultPreReleaseIdentifiers>rc.0</MinVerDefaultPreReleaseIdentifiers>
```

### Q11. How to build with a specific version without tags?

**A:** Override with `MinVerVersion`:

```bash
dotnet build -p:MinVerVersion=1.2.3
dotnet pack -p:MinVerVersion=1.2.3
```

### Q12. How to manage Hotfix releases?

**A:** Create a branch from the previous release tag and create a new tag:

```bash
git checkout v1.0.0
git checkout -b hotfix/1.0.1
git commit -m "fix: critical bug"
git tag -a v1.0.1 -m "Hotfix 1.0.1"
git push origin v1.0.1
git checkout main
git merge hotfix/1.0.1
```

### Q13. How are NuGet package versions set?

**A:** MinVer가 Automatic으로 `<Version>` 속성을 설정합니다:

```bash
dotnet pack
# Functorium.1.0.0.nupkg
```

### Q14. How to distinguish local build and CI build versions?

**A:** Build metadata를 uses합니다:

```yaml
# CI (GitHub Actions)
- run: dotnet build -p:MinVerBuildMetadata=ci.${{ github.run_number }}
  # 결과: 1.0.0+ci.123
```

### Q15. How to detect Breaking Changes?

**A:** Detected in two ways:

1. Exclamation mark after type: `feat!`, `fix!`
2. Footer includes `BREAKING CHANGE:`

```
feat!: Change API response format

BREAKING CHANGE: Response changed from array to object
```

---

## References

| Document | Description |
|------|------|
| [GitHub Actions Documentation](https://docs.github.com/actions) | GitHub Actions official documentation |
| [NuGet Deployment Guide](https://learn.microsoft.com/nuget/nuget-org/publish-a-package) | NuGet.org deployment official guide |
| [SourceLink Documentation](https://github.com/dotnet/sourcelink) | SourceLink debugging configuration |
| [MinVer GitHub](https://github.com/adamralph/minver) | MinVer official repository |
| [Semantic Versioning 2.0.0](https://semver.org/) | SemVer official documentation |
| [Conventional Commits 1.0.0](https://www.conventionalcommits.org/) | Conventional Commits official documentation |
