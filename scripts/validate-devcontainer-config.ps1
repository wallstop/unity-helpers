<#
.SYNOPSIS
    Validates devcontainer.json formatter assignments are consistent with
    pre-commit hook file type handling.
.DESCRIPTION
    Checks that every language/file type formatted or linted in the pre-commit
    hook has a corresponding explicit "[language]" formatter entry in
    devcontainer.json. Reports missing entries as warnings, then exits with error code 1 if any are found.

    This prevents drift between the hook's formatting pipeline and the
    editor's formatter assignments.
.EXAMPLE
    pwsh -NoProfile -File scripts/validate-devcontainer-config.ps1
#>
[CmdletBinding()]
param(
    [switch]$VerboseOutput
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$devcontainerPath = Join-Path $repoRoot '.devcontainer' 'devcontainer.json'
$preCommitPath = Join-Path $repoRoot '.githooks' 'pre-commit'

# ── Validate files exist ────────────────────────────────────────────────────

if (-not (Test-Path $devcontainerPath)) {
    Write-Error "devcontainer.json not found at: $devcontainerPath"
    exit 1
}

if (-not (Test-Path $preCommitPath)) {
    Write-Error "pre-commit hook not found at: $preCommitPath"
    exit 1
}

# ── Read devcontainer.json ──────────────────────────────────────────────────

$devcontainerContent = Get-Content $devcontainerPath -Raw

# Extract all "[language]" entries from devcontainer.json
# Matches patterns like: "[javascript]", "[csharp]", etc.
$languageEntries = [System.Collections.Generic.HashSet[string]]::new(
    [System.StringComparer]::OrdinalIgnoreCase
)
$languagePattern = '"\[(\w+)\]"'
$matches = [regex]::Matches($devcontainerContent, $languagePattern)
foreach ($match in $matches) {
    [void]$languageEntries.Add($match.Groups[1].Value)
}

if ($VerboseOutput) {
    Write-Host "Found devcontainer formatter entries for: $($languageEntries -join ', ')"
}

# ── Define expected language/formatter mappings ─────────────────────────────
# MAINTENANCE: When adding a new language/file type to the pre-commit hook,
# add a corresponding entry here so the validator catches missing devcontainer
# formatter assignments. Keep this list in sync with the hook's file type arrays.

$expectedLanguages = @(
    @{ Language = 'csharp'; FilePattern = '*.cs'; Formatter = 'csharpier.csharpier-vscode' },
    @{ Language = 'json'; FilePattern = '*.json'; Formatter = 'esbenp.prettier-vscode' },
    @{ Language = 'jsonc'; FilePattern = '*.jsonc'; Formatter = 'esbenp.prettier-vscode' },
    @{ Language = 'yaml'; FilePattern = '*.yml/*.yaml'; Formatter = 'esbenp.prettier-vscode' },
    @{ Language = 'markdown'; FilePattern = '*.md'; Formatter = 'esbenp.prettier-vscode' },
    @{ Language = 'javascript'; FilePattern = '*.js'; Formatter = 'esbenp.prettier-vscode' },
    @{ Language = 'xml'; FilePattern = '*.xml'; Formatter = '(formatOnSave: false)' },
    @{ Language = 'shellscript'; FilePattern = '*.sh'; Formatter = '(formatOnSave: false)' },
    @{ Language = 'powershell'; FilePattern = '*.ps1'; Formatter = '(formatOnSave: false)' },
    @{ Language = 'hlsl'; FilePattern = '*.hlsl'; Formatter = '(formatOnSave: false)' },
    @{ Language = 'shaderlab'; FilePattern = '*.shader'; Formatter = '(formatOnSave: false)' }
)

# ── Check for missing entries ───────────────────────────────────────────────

$missing = @()
foreach ($entry in $expectedLanguages) {
    if (-not $languageEntries.Contains($entry.Language)) {
        $missing += $entry
    }
}

if ($missing.Count -gt 0) {
    Write-Host ''
    Write-Warning "The following languages are handled by pre-commit but have no explicit devcontainer.json formatter entry:"
    foreach ($m in $missing) {
        Write-Warning "  [$($m.Language)] ($($m.FilePattern)) - expected formatter: $($m.Formatter)"
    }
    Write-Host ''
    Write-Error "devcontainer.json is missing $($missing.Count) explicit formatter assignment(s). See warnings above."
    exit 1
}

if ($VerboseOutput) {
    Write-Host "All expected formatter assignments are present in devcontainer.json." -ForegroundColor Green
}
exit 0
