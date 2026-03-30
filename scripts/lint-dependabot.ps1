#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Validate .github/dependabot.yml against the Dependabot v2 schema.

.DESCRIPTION
    Performs structural checks on the Dependabot configuration file to catch
    common mistakes such as invalid top-level keys, missing schedule blocks,
    and misplaced keys that belong inside groups.

    Error codes emitted:
      DEP001 - Missing 'version: 2' at the top of the file
      DEP002 - 'multi-ecosystem-groups:' top-level key present (invalid)
      DEP003 - 'multi-ecosystem-group:' key inside an updates entry (invalid)
      DEP004 - 'patterns:' key at updates-entry level instead of inside groups
      DEP005 - An updates entry is missing a 'schedule:' block
      DEP006 - A 'groups:' entry is missing 'patterns:' inside it

.PARAMETER Paths
    Specific paths to validate. Defaults to '.github/dependabot.yml'.

.PARAMETER VerboseOutput
    Show verbose output including which file is being checked.

.EXAMPLE
    ./scripts/lint-dependabot.ps1
    Validate the default .github/dependabot.yml.

.EXAMPLE
    ./scripts/lint-dependabot.ps1 -VerboseOutput
    Validate with verbose output.

.EXAMPLE
    ./scripts/lint-dependabot.ps1 -- .github/dependabot.yml
    Validate an explicit path.
#>
param(
    [switch]$VerboseOutput,
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$Paths
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Resolve the file to validate
$fileToCheck = $null
if ($Paths -and $Paths.Count -gt 0) {
    foreach ($p in $Paths) {
        # Accept any explicitly provided path that exists and is a YAML file
        if ($p -match '\.ya?ml$' -and (Test-Path $p)) {
            $fileToCheck = $p
            break
        }
        # Also accept paths that look like dependabot.yml regardless of extension check
        if ($p -match 'dependabot' -and (Test-Path $p)) {
            $fileToCheck = $p
            break
        }
    }
    # If still not found, just use the first non-flag argument
    if (-not $fileToCheck) {
        foreach ($p in $Paths) {
            if ($p -ne '--' -and (Test-Path $p)) {
                $fileToCheck = $p
                break
            }
        }
    }
}

if (-not $fileToCheck) {
    # Default location relative to repo root (two levels up from scripts/)
    $repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
    $fileToCheck = Join-Path $repoRoot '.github' 'dependabot.yml'
}

if (-not (Test-Path $fileToCheck)) {
    Write-Warning "dependabot.yml not found at: $fileToCheck — skipping schema check."
    exit 0
}

if ($VerboseOutput) {
    Write-Host "Checking Dependabot config: $fileToCheck" -ForegroundColor Cyan
}

$lines = Get-Content $fileToCheck

$errors = @()

# ── DEP001: version: 2 must be present ──────────────────────────────────────
$hasVersion2 = $false
foreach ($line in $lines) {
    if ($line -match '^\s*version\s*:\s*2\s*$') {
        $hasVersion2 = $true
        break
    }
}
if (-not $hasVersion2) {
    $errors += 'DEP001: Missing "version: 2" at the top of the file'
}

# ── DEP002: multi-ecosystem-groups: top-level key (invalid) ─────────────────
foreach ($line in $lines) {
    if ($line -match '^multi-ecosystem-groups\s*:') {
        $errors += 'DEP002: "multi-ecosystem-groups:" is not a valid Dependabot v2 top-level key'
        break
    }
}

# ── Stateful per-entry analysis (DEP003, DEP004, DEP005, DEP006) ─────────────
# We iterate line-by-line tracking:
#   $inUpdates        - true once we pass 'updates:'
#   $inEntry          - true inside an '  - package-ecosystem:' block
#   $entryHasSchedule - true if the current entry has '    schedule:'
#   $inGroupsBlock    - true while inside a '    groups:' block (4-space)
#   $inGroupsItem     - true while inside a named group under groups (6-space)
#   $groupsItemHasPatterns - true if current named group has 'patterns:'

$inUpdates = $false
$inEntry = $false
$entryHasSchedule = $false
$inGroupsBlock = $false
$inGroupsItem = $false
$groupsItemHasPatterns = $false
$currentEcosystem = ''
$entryLineNumber = 0
$lineNum = 0

function Finish-Entry {
    param($ecosystem, $lineNum, [ref]$errList, $hasSchedule)
    if (-not $hasSchedule) {
        $errList.Value += "DEP005: updates entry '$ecosystem' (near line $lineNum) is missing a 'schedule:' block"
    }
}

function Finish-GroupsItem {
    param($lineNum, [ref]$errList, $hasPatterns)
    if (-not $hasPatterns) {
        $errList.Value += "DEP006: A 'groups:' entry (near line $lineNum) is missing 'patterns:' inside it"
    }
}

foreach ($line in $lines) {
    $lineNum++

    # Detect start of 'updates:' section (top-level, no indent)
    if ($line -match '^updates\s*:') {
        $inUpdates = $true
        continue
    }

    if (-not $inUpdates) { continue }

    # Detect start of a new entry: '  - package-ecosystem:' (2-space + dash)
    if ($line -match '^  - package-ecosystem\s*:') {
        # Close previous entry
        if ($inEntry) {
            if ($inGroupsItem) {
                Finish-GroupsItem -lineNum $lineNum -errList ([ref]$errors) -hasPatterns $groupsItemHasPatterns
            }
            Finish-Entry -ecosystem $currentEcosystem -lineNum $entryLineNumber -errList ([ref]$errors) -hasSchedule $entryHasSchedule
        }
        $inEntry = $true
        $entryHasSchedule = $false
        $inGroupsBlock = $false
        $inGroupsItem = $false
        $groupsItemHasPatterns = $false
        $entryLineNumber = $lineNum
        if ($line -match 'package-ecosystem\s*:\s*["\x27]?(\S+?)["\x27]?\s*$') {
            $currentEcosystem = $Matches[1]
        } else {
            $currentEcosystem = 'unknown'
        }
        continue
    }

    if (-not $inEntry) { continue }

    # ── DEP003: multi-ecosystem-group: inside an entry ───────────────────────
    if ($line -match '^    multi-ecosystem-group\s*:') {
        $errors += "DEP003: ""multi-ecosystem-group:"" is not a valid Dependabot v2 key (found in entry '$currentEcosystem', line $lineNum)"
    }

    # ── Detect schedule: at entry level (4-space indent) ─────────────────────
    if ($line -match '^    schedule\s*:') {
        $entryHasSchedule = $true
    }

    # ── Detect groups: block start (4-space indent) ───────────────────────────
    if ($line -match '^    groups\s*:') {
        $inGroupsBlock = $true
        $inGroupsItem = $false
        $groupsItemHasPatterns = $false
        continue
    }

    if ($inGroupsBlock) {
        # A named group item: 6-space indent + non-whitespace key (e.g. '      all-dependencies:')
        if ($line -match '^      [^\s]') {
            if ($inGroupsItem) {
                Finish-GroupsItem -lineNum $lineNum -errList ([ref]$errors) -hasPatterns $groupsItemHasPatterns
            }
            $inGroupsItem = $true
            $groupsItemHasPatterns = $false
            continue
        }

        # patterns: inside a named group (8-space indent)
        if ($line -match '^        patterns\s*:') {
            $groupsItemHasPatterns = $true
            continue
        }

        # If we hit a 4-space key that is not groups:, we've left the groups block
        if ($line -match '^    [^\s]' -and $line -notmatch '^    -') {
            if ($inGroupsItem) {
                Finish-GroupsItem -lineNum $lineNum -errList ([ref]$errors) -hasPatterns $groupsItemHasPatterns
                $inGroupsItem = $false
            }
            $inGroupsBlock = $false
        }
    }

    # ── DEP004: patterns: at entry level (not inside groups) ─────────────────
    # 4-space indent patterns: or 2-space indent patterns:
    if (-not $inGroupsBlock) {
        if ($line -match '^    patterns\s*:' -or $line -match '^  patterns\s*:') {
            $errors += "DEP004: ""patterns:"" found at entry level in '$currentEcosystem' (line $lineNum); it must be inside a 'groups:' block"
        }
    }
}

# Close the last entry
if ($inEntry) {
    if ($inGroupsItem) {
        Finish-GroupsItem -lineNum $lineNum -errList ([ref]$errors) -hasPatterns $groupsItemHasPatterns
    }
    Finish-Entry -ecosystem $currentEcosystem -lineNum $entryLineNumber -errList ([ref]$errors) -hasSchedule $entryHasSchedule
}

# ── Output results ────────────────────────────────────────────────────────────
if ($errors.Count -eq 0) {
    if ($VerboseOutput) {
        Write-Host "dependabot.yml passed all schema checks." -ForegroundColor Green
    }
    exit 0
}

foreach ($err in $errors) {
    Write-Host $err -ForegroundColor Red
}

exit 1
