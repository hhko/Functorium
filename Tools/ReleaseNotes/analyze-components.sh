#!/bin/bash

# Automated analysis of all components based on configuration
# Usage: ./analyze-components.sh <base_branch> <target_branch>

set -e

BASE_BRANCH=${1:-origin/release/1.0}
TARGET_BRANCH=${2:-origin/main}

SCRIPTS_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_FILE="$SCRIPTS_DIR/config/component-priority.json"
ANALYSIS_DIR="$SCRIPTS_DIR/analysis-output"

echo "🔍 Starting automated component analysis"
echo "📋 Using config: $CONFIG_FILE"
echo "📊 Output directory: $ANALYSIS_DIR"
echo "⏱️  This may take several minutes for large repositories..."

# Start total timing
SCRIPT_START_TIME=$(date +%s)

# Ensure analysis directory exists
mkdir -p "$ANALYSIS_DIR"

# Check if jq is available for JSON processing
if ! command -v jq &> /dev/null; then
    echo "⚠️  jq not found, using manual JSON parsing"
    # Fallback to manual JSON parsing
    if [ -f "$CONFIG_FILE" ]; then
        echo "📊 Reading components from configuration (manual parsing)..."
        RAW_COMPONENTS=($(grep -oP '"\K[^"]+(?=")' "$CONFIG_FILE" | grep -v "analysis_priorities"))
    else
        echo "⚠️  Config file not found, using default fallback"
        RAW_COMPONENTS=("Src/Functorium" "Src/Functorium.Testing" "Docs")
    fi
else
    echo "📊 Processing components from configuration..."
    # Extract component paths from JSON config - flat array structure
    RAW_COMPONENTS=($(jq -r '.analysis_priorities[]' "$CONFIG_FILE" 2>/dev/null || echo ""))

    # Expand glob patterns to actual directories
    COMPONENTS=()
    for pattern in "${RAW_COMPONENTS[@]}"; do
        if [[ "$pattern" == *"*"* ]]; then
            # This is a glob pattern, expand it from the git root
            echo "🔍 Expanding glob pattern: $pattern"
            # Change to git root directory for proper glob expansion
            GIT_ROOT=$(git rev-parse --show-toplevel)
            cd "$GIT_ROOT"
            for expanded_path in $pattern; do
                if [ -d "$expanded_path" ]; then
                    COMPONENTS+=("$expanded_path")
                    echo "   ✅ Found: $expanded_path"
                fi
            done
            # Return to scripts directory
            cd "$SCRIPTS_DIR"
        else
            # Regular path, add as-is if it exists
            GIT_ROOT=$(git rev-parse --show-toplevel)
            if [ -d "$GIT_ROOT/$pattern" ]; then
                COMPONENTS+=("$pattern")
                echo "   ✅ Found: $pattern"
            fi
        fi
    done

    # If config reading failed or no components found, use fallback
    if [ ${#COMPONENTS[@]} -eq 0 ]; then
        echo "⚠️  Could not read config or no valid components found, using fallback list"
        COMPONENTS=("Src/Functorium" "Src/Functorium.Testing" "Docs")
    fi
fi

# If still no components (for non-jq path), process RAW_COMPONENTS
if ! command -v jq &> /dev/null && [ -n "$RAW_COMPONENTS" ]; then
    COMPONENTS=()
    for pattern in "${RAW_COMPONENTS[@]}"; do
        GIT_ROOT=$(git rev-parse --show-toplevel)
        if [ -d "$GIT_ROOT/$pattern" ]; then
            COMPONENTS+=("$pattern")
            echo "   ✅ Found: $pattern"
        fi
    done
fi

# Function to analyze a single component
analyze_component() {
    local component_start=$(date +%s)
    local component_path="$1"
    local output_file="$2"

    echo "  📁 Analyzing: $component_path"

    # Change to git root for proper path resolution
    local original_dir=$(pwd)
    cd "$(git rev-parse --show-toplevel)"

    # Check if there are any changes in this component first
    local change_count=$(git diff --name-status $BASE_BRANCH..$TARGET_BRANCH -- "$component_path/" 2>/dev/null | wc -l | tr -d ' ')
    local commit_count=$(git log --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- "$component_path/" 2>/dev/null | wc -l | tr -d ' ')

    # Return to original directory
    cd "$original_dir"

    if [ "$change_count" -eq 0 ] && [ "$commit_count" -eq 0 ]; then
        echo "    ⏭️  No changes found, skipping file creation"
        local component_end=$(date +%s)
        echo "    ⏱️  Completed in $((component_end - component_start))s"
        return 1  # Return non-zero to indicate no file was created
    fi

    echo "    ✅ Found $change_count file changes and $commit_count commits, creating analysis file"

    # Use existing analyze-folder.sh if available
    if [ -f "$SCRIPTS_DIR/analyze-folder.sh" ]; then
        export BASE_BRANCH
        export TARGET_BRANCH
        "$SCRIPTS_DIR/analyze-folder.sh" "$component_path" > "$output_file"
    else
        # Fallback manual analysis - ensure we're in git root for commands
        echo "# Analysis for $component_path" > "$output_file"
        echo "" >> "$output_file"
        echo "Generated: $(date)" >> "$output_file"
        echo "Comparing: $BASE_BRANCH → $TARGET_BRANCH" >> "$output_file"
        echo "" >> "$output_file"
        echo "## Change Summary" >> "$output_file"
        echo "" >> "$output_file"

        # Change to git root for git commands
        cd "$(git rev-parse --show-toplevel)"
        git diff --stat $BASE_BRANCH..$TARGET_BRANCH -- "$component_path/" >> "$output_file" 2>/dev/null || echo "No changes found" >> "$output_file"

        echo "" >> "$output_file"
        echo "## All Commits (Chronological)" >> "$output_file"
        echo "" >> "$output_file"
        git log --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- "$component_path/" >> "$output_file" 2>/dev/null || echo "No commits found" >> "$output_file"

        echo "" >> "$output_file"
        echo "## Top Contributors" >> "$output_file"
        echo "" >> "$output_file"
        git log --format="%an" --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- "$component_path/" 2>/dev/null | sort | uniq -c | sort -nr | head -5 >> "$output_file" || echo "No contributors" >> "$output_file"

        echo "" >> "$output_file"
        echo "## Categorized Commits" >> "$output_file"
        echo "" >> "$output_file"
        echo "### Feature Commits" >> "$output_file"
        echo "" >> "$output_file"
        git log --grep="feat\|feature\|add" --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- "$component_path/" 2>/dev/null | head -10 >> "$output_file" || echo "None found" >> "$output_file"

        echo "" >> "$output_file"
        echo "### Bug Fixes" >> "$output_file"
        echo "" >> "$output_file"
        git log --grep="fix\|bug" --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- "$component_path/" 2>/dev/null | head -10 >> "$output_file" || echo "None found" >> "$output_file"

        echo "" >> "$output_file"
        echo "### Breaking Changes" >> "$output_file"
        echo "" >> "$output_file"
        git log --grep="breaking\|BREAKING" --oneline --no-merges --cherry-pick --right-only $BASE_BRANCH...$TARGET_BRANCH -- "$component_path/" 2>/dev/null | head -10 >> "$output_file" || echo "None found" >> "$output_file"

        # Add playground/test examples if available
        if [[ "$component_path" == "playground/"* ]] || [[ "$component_path" == "tests/"* ]]; then
            echo "" >> "$output_file"
            echo "## Notable Changes" >> "$output_file"
            echo "" >> "$output_file"
            git diff --name-status $BASE_BRANCH..$TARGET_BRANCH -- "$component_path/" | grep "^A" | head -10 >> "$output_file" 2>/dev/null || echo "No new files added" >> "$output_file"
        fi

        # Return to original directory
        cd "$original_dir"
    fi

    local component_end=$(date +%s)
    echo "    ⏱️  Completed in $((component_end - component_start))s"
    return 0  # Return zero to indicate file was created successfully
}

# Function to generate safe filename from component path
generate_filename() {
    local component="$1"
    local git_root=$(git rev-parse --show-toplevel 2>/dev/null || echo "")

    # If component is an absolute path, try to make it relative to git root
    if [[ "$component" = /* ]] || [[ "$component" =~ ^[A-Za-z]:/ ]]; then
        if [ -n "$git_root" ]; then
            # Remove git root prefix if present
            component="${component#$git_root/}"
            component="${component#$git_root}"
        fi
    fi

    # Generate safe filename: replace slashes and colons, remove src- prefix and trailing dash
    echo "$component" | sed 's|[/:\\]|-|g' | sed 's|^src-||' | sed 's|-$||' | sed 's|^[A-Za-z]-||'
}

# Analyze all components
echo "🎯 Analyzing components..."
ANALYSIS_START=$(date +%s)

component_count=0
files_created=0
total_components=${#COMPONENTS[@]}
echo "📊 Processing $total_components components..."

for component in "${COMPONENTS[@]}"; do
    ((component_count++))
    echo "[$component_count/$total_components] Processing: $component"
    component_name=$(generate_filename "$component")
    output_file="$ANALYSIS_DIR/$component_name.md"

    if analyze_component "$component" "$output_file"; then
        ((files_created++))
    fi
done

ANALYSIS_END=$(date +%s)
echo "✅ Component analysis completed in $((ANALYSIS_END - ANALYSIS_START))s"
echo "📊 Created $files_created analysis files out of $total_components components"

# Generate summary report
echo "📊 Generating summary report..."
SUMMARY_START=$(date +%s)
summary_file="$ANALYSIS_DIR/summary.md"

cat > "$summary_file" << EOF
# Release Notes Analysis Summary

Generated: $(date)
Comparison: $BASE_BRANCH → $TARGET_BRANCH

## Components Analyzed

EOF

for component in "${COMPONENTS[@]}"; do
    component_name=$(generate_filename "$component")
    if [ -f "$ANALYSIS_DIR/$component_name.md" ]; then
        # Extract the actual component path from the analysis file instead of using the original pattern
        actual_component_path=$(grep "📁 ANALYZING:" "$ANALYSIS_DIR/$component_name.md" 2>/dev/null | sed 's/📁 ANALYZING: //' | tr -d '[:space:]' || echo "$component")
        if [ -n "$actual_component_path" ]; then
            # Ensure we're in the git repository root for the file count
            cd "$(git rev-parse --show-toplevel)"
            file_count=$(git diff --name-status $BASE_BRANCH..$TARGET_BRANCH -- "$actual_component_path" 2>/dev/null | wc -l | tr -d ' ')
            cd "$SCRIPTS_DIR"
            echo "- [$component]($component_name.md) - $file_count files changed" >> "$summary_file"
        else
            echo "- [$component]($component_name.md)" >> "$summary_file"
        fi
    fi
done

cat >> "$summary_file" << EOF

## Statistics

- Total components checked: $total_components
- Components with changes: $files_created
- Analysis files generated: $files_created

## Next Steps

1. Review each component analysis file
2. Extract key features and changes
3. Write user-facing release notes using tools/ReleaseNotes/docs/template.md
4. Validate code examples and API references

EOF

SUMMARY_END=$(date +%s)
echo "✅ Summary generation completed in $((SUMMARY_END - SUMMARY_START))s"

# Calculate total time
SCRIPT_END_TIME=$(date +%s)
TOTAL_TIME=$((SCRIPT_END_TIME - SCRIPT_START_TIME))

echo ""
echo "✅ Component analysis complete!"
echo "⏱️  Total execution time: ${TOTAL_TIME}s"
echo ""
echo "📊 Timing Summary:"
echo "   Component Analysis: $((ANALYSIS_END - ANALYSIS_START))s"
echo "   Summary Generation: $((SUMMARY_END - SUMMARY_START))s"
echo ""
echo "📄 Summary: $summary_file"
echo "📁 Detailed analysis files in: $ANALYSIS_DIR/"
echo ""
echo "📋 Analysis files generated:"
ls -1 "$ANALYSIS_DIR"/*.md 2>/dev/null | sed 's/^/   /' || echo "   (none)"
