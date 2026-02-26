#!/usr/bin/env bash
# MIT License - Copyright (c) 2026 wallstop
# Full license text: https://github.com/wallstop/unity-helpers/blob/main/LICENSE

# Common validation fix patterns for CI failures.
# Referenced by: .llm/skills/validation-troubleshooting.md

# --- CRLF-Aware Newline Appending ---
# When appending a final newline to a file, detect existing line endings
# to avoid creating mixed CRLF/LF, which fails eol:check.
crlf_aware_append_newline() {
    local file="$1"
    if grep -q $'\r' "$file" 2>/dev/null; then
        printf '\r\n' >> "$file"  # CRLF
    else
        printf '\n' >> "$file"    # LF
    fi
}

# --- Fix Prettier Formatting ---
# Fix a single file or all files
prettier_fix_file() {
    npx prettier --write -- "$1"
}
prettier_fix_all() {
    npx prettier --write -- .
}

# --- Fix Line Endings ---
fix_line_endings() {
    npm run eol:fix
}

# --- Fix Git Hook Permissions ---
# Hook files must be executable or git silently skips them.
fix_hook_permissions() {
    chmod +x .githooks/*
    git update-index --chmod=+x .githooks/pre-commit .githooks/pre-push
    npm run validate:hook-perms
}

# --- Check For Pre-Existing Warnings ---
# Determine if a validation failure exists on main (pre-existing, not blocking).
check_preexisting() {
    local command="$1"
    git stash && git checkout main
    npm run "$command"
    git checkout - && git stash pop
}

# --- Quick Recovery (run all fixes) ---
quick_recovery() {
    npx prettier --write -- .           # Fix all formatting
    npm run eol:fix                     # Fix line endings
    dotnet tool run csharpier format .  # Format C#
    npm run validate:prepush            # Full validation
}
