#!/usr/bin/env bash
# audit-license-years.sh - Audit .cs files for mismatched copyright years
#
# Usage:
#   ./scripts/audit-license-years.sh
#   ./scripts/audit-license-years.sh --csv
#   ./scripts/audit-license-years.sh --summary
#
# This script compares the copyright year in .cs file headers against the
# git creation year to identify files with mismatched years.
#
# Options:
#   --csv      Output results in CSV format (file,current_year,git_year,match)
#   --summary  Only show summary statistics
#   --help     Show this help message

set -euo pipefail

# Configuration
REPO_START_YEAR=2023
CURRENT_YEAR=2026

# Parse arguments
OUTPUT_MODE="default"
for arg in "$@"; do
    case "$arg" in
        --csv)
            OUTPUT_MODE="csv"
            ;;
        --summary)
            OUTPUT_MODE="summary"
            ;;
        --help|-h)
            head -20 "$0" | tail -18
            exit 0
            ;;
        *)
            echo "Unknown option: $arg" >&2
            exit 1
            ;;
    esac
done

# Get script directory and repo root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$REPO_ROOT"

# Counters
total_files=0
matched_files=0
mismatched_files=0
missing_header_files=0
no_git_history_files=0

# Arrays for mismatches
declare -a mismatch_list=()

# Extract year from copyright header
get_header_year() {
    local file="$1"
    local first_line
    first_line=$(head -1 "$file" 2>/dev/null || echo "")

    # Match pattern: // MIT License - Copyright (c) YYYY ...
    if [[ "$first_line" =~ Copyright\ \(c\)\ ([0-9]{4}) ]]; then
        echo "${BASH_REMATCH[1]}"
    else
        echo ""
    fi
}

# Get git creation year for a file
get_git_creation_year() {
    local file="$1"
    local year

    # Use --follow to track across renames, --diff-filter=A for additions only
    year=$(git log --follow --diff-filter=A --format=%ad --date=format:%Y -- "$file" 2>/dev/null | tail -1)

    if [[ -z "$year" ]]; then
        echo ""
    else
        echo "$year"
    fi
}

# Print CSV header if in CSV mode
if [[ "$OUTPUT_MODE" == "csv" ]]; then
    echo "file,current_year,git_year,status"
fi

# Find all .cs files
while IFS= read -r -d '' file; do
    ((total_files++)) || true

    # Get relative path for cleaner output
    rel_path="${file#$REPO_ROOT/}"

    # Get header year
    header_year=$(get_header_year "$file")

    if [[ -z "$header_year" ]]; then
        ((missing_header_files++)) || true
        if [[ "$OUTPUT_MODE" == "csv" ]]; then
            echo "$rel_path,MISSING,N/A,missing_header"
        elif [[ "$OUTPUT_MODE" == "default" ]]; then
            echo "MISSING HEADER: $rel_path"
        fi
        continue
    fi

    # Get git creation year
    git_year=$(get_git_creation_year "$file")

    if [[ -z "$git_year" ]]; then
        ((no_git_history_files++)) || true
        # File has no git history (untracked), should use current year
        if [[ "$header_year" == "$CURRENT_YEAR" ]]; then
            ((matched_files++)) || true
            if [[ "$OUTPUT_MODE" == "csv" ]]; then
                echo "$rel_path,$header_year,UNTRACKED,ok"
            fi
        else
            ((mismatched_files++)) || true
            mismatch_list+=("$rel_path: has $header_year, expected $CURRENT_YEAR (untracked)")
            if [[ "$OUTPUT_MODE" == "csv" ]]; then
                echo "$rel_path,$header_year,UNTRACKED,mismatch"
            elif [[ "$OUTPUT_MODE" == "default" ]]; then
                echo "MISMATCH: $rel_path - has $header_year, expected $CURRENT_YEAR (untracked)"
            fi
        fi
        continue
    fi

    # Handle files created before repo start year
    if [[ "$git_year" -lt "$REPO_START_YEAR" ]]; then
        git_year="$REPO_START_YEAR"
    fi

    # Compare years
    if [[ "$header_year" == "$git_year" ]]; then
        ((matched_files++)) || true
        if [[ "$OUTPUT_MODE" == "csv" ]]; then
            echo "$rel_path,$header_year,$git_year,ok"
        fi
    else
        ((mismatched_files++)) || true
        mismatch_list+=("$rel_path: has $header_year, expected $git_year")
        if [[ "$OUTPUT_MODE" == "csv" ]]; then
            echo "$rel_path,$header_year,$git_year,mismatch"
        elif [[ "$OUTPUT_MODE" == "default" ]]; then
            echo "MISMATCH: $rel_path - has $header_year, expected $git_year"
        fi
    fi
done < <(find "$REPO_ROOT" -name "*.cs" -type f -print0 | sort -z)

# Print summary
if [[ "$OUTPUT_MODE" != "csv" ]]; then
    echo ""
    echo "=== License Year Audit Summary ==="
    echo "Total .cs files:        $total_files"
    echo "Matched years:          $matched_files"
    echo "Mismatched years:       $mismatched_files"
    echo "Missing headers:        $missing_header_files"
    echo "No git history:         $no_git_history_files"
    echo ""

    if [[ $mismatched_files -gt 0 ]]; then
        echo "Files needing update: $mismatched_files"
        exit 1
    else
        echo "All files have correct copyright years!"
        exit 0
    fi
fi
