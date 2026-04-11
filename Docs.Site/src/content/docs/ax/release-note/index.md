---
title: "release-note"
description: "Automated release note generation plugin"
---

release-note is a Claude Code plugin that automates release note generation for the Functorium project. It executes a 5-phase workflow covering C# script-based data collection, Conventional Commits analysis, Breaking Changes detection, release note writing, and validation.

## Installation

```bash
# Load standalone
claude --plugin-dir ./.claude/plugins/release-note

# Load simultaneously with the functorium-develop plugin
claude --plugin-dir ./.claude/plugins/release-note --plugin-dir ./.claude/plugins/functorium-develop
```

> `--plugin-dir` loads plugins on a per-session basis. They appear in `/skills` in the format `release-note:generate`.

## 5-Phase Workflow

```
Environment Validation → Data Collection → Commit Analysis → Release Note Writing → Validation
```

| Phase | Name | Goal | Key Deliverables |
|:-----:|------|------|-----------------|
| 1 | Environment Validation | Verify prerequisites, determine Base Branch | Git/SDK status, Base/Target determination |
| 2 | Data Collection | Analyze component/API changes with C# scripts | `*.md` analysis files, `all-api-changes.txt`, `api-changes-diff.txt` |
| 3 | Commit Analysis | Analyze collected data, extract features | `phase3-commit-analysis.md`, `phase3-feature-groups.md` |
| 4 | Release Note Writing | Generate release notes based on TEMPLATE.md | `RELEASE-{VERSION}.md` |
| 5 | Validation | Quality and accuracy verification | `phase5-validation-report.md`, `phase5-api-validation.md` |

### Core Principles

| Principle | Description |
|-----------|-------------|
| Accuracy first | Never document APIs not found in the Uber file (`all-api-changes.txt`) |
| Value delivery required | All major features include a "Why this matters:" section |
| Breaking Changes auto-detection | Git Diff analysis takes priority over commit message patterns |
| Traceability | All features tracked by commit SHA |

## generate Skill

The only skill that automatically generates release notes. It takes a version as parameter and executes the entire 5-Phase workflow.

**Trigger Examples:**

```text
/generate v1.2.0
Generate the release notes
Create a release note
Write release notes
New version release
```

**Version Formats:**

| Format | Example | Description |
|--------|---------|-------------|
| Regular release | `v1.2.0` | Standard deployment |
| First deployment | `v1.0.0` | Analyzes from initial commit |
| Pre-release | `v1.0.0-beta.1` | Pre-deployment |

## Agents

The release-note plugin provides 1 **release-engineer** agent. It handles C# script execution, commit analysis, Breaking Changes detection, release note writing, and validation.

See the [Expert Agents](./agents/) page for details.

## Plugin Structure

```
.claude/plugins/release-note/
├── .claude-plugin/
│   └── plugin.json              # Plugin metadata (v1.0.0)
├── skills/
│   └── generate/
│       └── SKILL.md             # generate skill definition (5-Phase workflow)
└── agents/
    └── release-engineer.md      # release-engineer agent definition
```

## .release-notes/ Directory Structure

Scripts, templates, and validation tools needed for release note generation are located in the `.release-notes/` directory at the project root.

```
.release-notes/
├── TEMPLATE.md                  # Release note copy template
├── RELEASE-{VERSION}.md         # Generated release notes
├── validate-release-notes.ps1   # GitHub Release size limit (125,000 chars) validation
├── README.md                    # Process overview
└── scripts/
    ├── AnalyzeAllComponents.cs  # Component analysis C# script
    ├── AnalyzeFolder.cs         # Folder analysis C# script
    ├── ApiGenerator.cs          # API change generation C# script
    ├── ExtractApiChanges.cs     # API change extraction C# script
    ├── Directory.Build.props    # Build settings
    ├── Directory.Packages.props # Package settings
    ├── config/
    │   └── component-priority.json  # Component priority settings
    └── docs/
        ├── README.md            # 5-Phase workflow full overview
        ├── phase1-setup.md      # Phase 1 details
        ├── phase2-collection.md # Phase 2 details
        ├── phase3-analysis.md   # Phase 3 details
        ├── phase4-writing.md    # Phase 4 details
        └── phase5-validation.md # Phase 5 details
```

## Troubleshooting

| Problem | Solution |
|---------|----------|
| No Base Branch | Auto-detected as first deployment, analyzes from initial commit |
| .NET SDK version error | .NET 10.x installation required |
| File lock issue | `taskkill /F /IM dotnet.exe` (Windows) |
| API validation failure | Verify correct API name in Uber file |
| runfile cache error | Run `./Build-CleanRunFileCache.ps1` |

### Full Reset (Windows)

```powershell
Stop-Process -Name "dotnet" -Force -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .release-notes\scripts\.analysis-output -ErrorAction SilentlyContinue
dotnet nuget locals all --clear
```
